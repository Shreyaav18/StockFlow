using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using StockFlow.Web.Data;
using StockFlow.Web.DTOs.Item;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Services
{
    public class ItemService : IItemService
    {
        private readonly AppDbContext _db;
        private readonly IAuditLogService _auditLogService;
        private readonly IMemoryCache _cache;
        private const string ALL_ITEMS_CACHE_KEY = "items:all";
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(10);

        public ItemService(AppDbContext db, IAuditLogService auditLogService, IMemoryCache cache)
        {
            _db = db;
            _auditLogService = auditLogService;
            _cache = cache;
        }

        public async Task<ItemViewModel> GetByIdAsync(int itemId, CancellationToken ct = default)
        {
            try
            {
                var cacheKey = $"items:{itemId}";

                if (_cache.TryGetValue(cacheKey, out ItemViewModel? cached) && cached != null)
                    return cached;

                var item = await _db.Items
                    .AsNoTracking()
                    .Include(i => i.Creator)
                    .FirstOrDefaultAsync(i => i.ItemId == itemId, ct)
                    ?? throw new NotFoundException(ErrorMessages.Item.NotFound);

                var viewModel = MapToViewModel(item);
                _cache.Set(cacheKey, viewModel, CACHE_DURATION);
                return viewModel;
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching item {ItemId}", itemId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<IEnumerable<ItemViewModel>> GetAllAsync(CancellationToken ct = default)
        {
            try
            {
                if (_cache.TryGetValue(ALL_ITEMS_CACHE_KEY, out IEnumerable<ItemViewModel>? cached) && cached != null)
                    return cached;

                var items = await _db.Items
                    .AsNoTracking()
                    .Include(i => i.Creator)
                    .Where(i => i.IsActive)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync(ct);

                var viewModels = items.Select(MapToViewModel).ToList();
                _cache.Set(ALL_ITEMS_CACHE_KEY, viewModels, CACHE_DURATION);
                return viewModels;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching all items");
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<ItemViewModel> CreateAsync(CreateItemDto dto, int createdBy, CancellationToken ct = default)
        {
            try
            {
                if (await SKUExistsAsync(dto.SKU, null, ct))
                    throw new ConflictException(ErrorMessages.Item.DuplicateSKU);

                var item = new Models.Item
                {
                    ItemName = dto.ItemName.Trim(),
                    SKU = dto.SKU.ToUpper().Trim(),
                    Unit = dto.Unit.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                _db.Items.Add(item);
                await _db.SaveChangesAsync(ct);
                InvalidateItemCache(item.ItemId);

                await _auditLogService.LogAsync(new DTOs.Report.AuditLogDto
                {
                    EntityName = "Item",
                    EntityId = item.ItemId,
                    Action = "Create",
                    PerformedBy = createdBy,
                    Details = $"SKU: {item.SKU}"
                }, ct);

                Log.Information("Item {ItemId} created by user {UserId}", item.ItemId, createdBy);
                return await GetByIdAsync(item.ItemId, ct);
            }
            catch (AppException) { throw; }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error creating item with SKU {SKU}", dto.SKU);
                throw new DatabaseException();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error creating item with SKU {SKU}", dto.SKU);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<ItemViewModel> UpdateAsync(int itemId, UpdateItemDto dto, int updatedBy, CancellationToken ct = default)
        {
            try
            {
                var item = await _db.Items.FindAsync(new object[] { itemId }, ct)
                    ?? throw new NotFoundException(ErrorMessages.Item.NotFound);

                item.ItemName = dto.ItemName.Trim();
                item.Unit = dto.Unit.Trim();
                item.IsActive = dto.IsActive;

                await _db.SaveChangesAsync(ct);
                InvalidateItemCache(itemId);

                await _auditLogService.LogAsync(new DTOs.Report.AuditLogDto
                {
                    EntityName = "Item",
                    EntityId = itemId,
                    Action = "Update",
                    PerformedBy = updatedBy
                }, ct);

                Log.Information("Item {ItemId} updated by user {UserId}", itemId, updatedBy);
                return await GetByIdAsync(itemId, ct);
            }
            catch (AppException) { throw; }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error updating item {ItemId}", itemId);
                throw new DatabaseException();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error updating item {ItemId}", itemId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task DeleteAsync(int itemId, int deletedBy, CancellationToken ct = default)
        {
            try
            {
                var item = await _db.Items.FindAsync(new object[] { itemId }, ct)
                    ?? throw new NotFoundException(ErrorMessages.Item.NotFound);

                var isLinked = await _db.Shipments.AnyAsync(s => s.ItemId == itemId, ct);
                if (isLinked)
                    throw new ConflictException(ErrorMessages.Item.CannotDelete);

                item.IsActive = false;
                await _db.SaveChangesAsync(ct);
                InvalidateItemCache(itemId);

                await _auditLogService.LogAsync(new DTOs.Report.AuditLogDto
                {
                    EntityName = "Item",
                    EntityId = itemId,
                    Action = "Delete",
                    PerformedBy = deletedBy
                }, ct);

                Log.Information("Item {ItemId} soft-deleted by user {UserId}", itemId, deletedBy);
            }
            catch (AppException) { throw; }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error deleting item {ItemId}", itemId);
                throw new DatabaseException();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error deleting item {ItemId}", itemId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<bool> SKUExistsAsync(string sku, int? excludeItemId = null, CancellationToken ct = default)
        {
            try
            {
                return await _db.Items.AnyAsync(i =>
                    i.SKU == sku.ToUpper().Trim() &&
                    (excludeItemId == null || i.ItemId != excludeItemId), ct);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking SKU existence for {SKU}", sku);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        private void InvalidateItemCache(int itemId)
        {
            _cache.Remove($"items:{itemId}");
            _cache.Remove(ALL_ITEMS_CACHE_KEY);
        }

        private static ItemViewModel MapToViewModel(Models.Item item) => new()
        {
            ItemId = item.ItemId,
            ItemName = item.ItemName,
            SKU = item.SKU,
            Unit = item.Unit,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            CreatedByName = item.Creator?.FullName ?? "System"
        };
    }
}