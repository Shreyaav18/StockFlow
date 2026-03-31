using Microsoft.EntityFrameworkCore;
using Serilog;
using StockFlow.Web.Data;
using StockFlow.Web.DTOs.Item;
using StockFlow.Web.DTOs.Process;
using StockFlow.Web.DTOs.Search;
using StockFlow.Web.DTOs.Shipment;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Services
{
    public class SearchService : ISearchService
    {
        private readonly AppDbContext _db;

        public SearchService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<SearchResultViewModel> SearchAsync(SearchDto dto, CancellationToken ct = default)
        {
            try
            {
                if (dto.Query.Length < 2)
                    throw new ValidationException(ErrorMessages.Search.QueryTooShort);

                var items = dto.SearchItems
                    ? await SearchItemsAsync(dto.Query, ct)
                    : Enumerable.Empty<ItemViewModel>();

                var shipments = dto.SearchShipments
                    ? await SearchShipmentsAsync(dto.Query, ct)
                    : Enumerable.Empty<ShipmentViewModel>();

                var processedItems = dto.SearchProcessedItems
                    ? await SearchProcessedItemsAsync(dto.Query, ct)
                    : Enumerable.Empty<ProcessedItemViewModel>();

                return new SearchResultViewModel
                {
                    Query = dto.Query,
                    TotalResults = items.Count() + shipments.Count() + processedItems.Count(),
                    SearchItems = dto.SearchItems,
                    SearchShipments = dto.SearchShipments,
                    SearchProcessedItems = dto.SearchProcessedItems,
                    Items = items,
                    Shipments = shipments,
                    ProcessedItems = processedItems
                };
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error performing search for query {Query}", dto.Query);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<IEnumerable<ItemViewModel>> SearchItemsAsync(string query, CancellationToken ct = default)
        {
            try
            {
                var q = query.ToLower().Trim();
                var items = await _db.Items
                    .AsNoTracking()
                    .Include(i => i.Creator)
                    .Where(i => i.IsActive && (
                        i.ItemName.ToLower().Contains(q) ||
                        i.SKU.ToLower().Contains(q)))
                    .OrderBy(i => i.ItemName)
                    .ToListAsync(ct);

                return items.Select(i => new ItemViewModel
                {
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    SKU = i.SKU,
                    Unit = i.Unit,
                    IsActive = i.IsActive,
                    CreatedAt = i.CreatedAt,
                    CreatedByName = i.Creator?.FullName ?? "System"
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching items for query {Query}", query);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<IEnumerable<ShipmentViewModel>> SearchShipmentsAsync(string query, CancellationToken ct = default)
        {
            try
            {
                var q = query.ToLower().Trim();
                var shipments = await _db.Shipments
                    .AsNoTracking()
                    .Include(s => s.Item)
                    .Include(s => s.ReceivedByUser)
                    .Where(s =>
                        s.Item!.ItemName.ToLower().Contains(q) ||
                        s.Item.SKU.ToLower().Contains(q) ||
                        s.Status.ToLower().Contains(q))
                    .OrderByDescending(s => s.ReceivedAt)
                    .ToListAsync(ct);

                return shipments.Select(s => new ShipmentViewModel
                {
                    ShipmentId = s.ShipmentId,
                    ItemId = s.ItemId,
                    ItemName = s.Item?.ItemName ?? string.Empty,
                    SKU = s.Item?.SKU ?? string.Empty,
                    TotalWeight = s.TotalWeight,
                    Unit = s.Item?.Unit ?? string.Empty,
                    Status = s.Status,
                    ReceivedAt = s.ReceivedAt,
                    ReceivedByName = s.ReceivedByUser?.FullName ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching shipments for query {Query}", query);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<IEnumerable<ProcessedItemViewModel>> SearchProcessedItemsAsync(string query, CancellationToken ct = default)
        {
            try
            {
                var q = query.ToLower().Trim();
                var items = await _db.ProcessedItems
                    .AsNoTracking()
                    .Include(p => p.Item)
                    .Include(p => p.ProcessedByUser)
                    .Where(p =>
                        p.Item!.ItemName.ToLower().Contains(q) ||
                        p.Item.SKU.ToLower().Contains(q) ||
                        p.Status.ToLower().Contains(q))
                    .OrderByDescending(p => p.ProcessedAt)
                    .ToListAsync(ct);

                return items.Select(p => new ProcessedItemViewModel
                {
                    ProcessedItemId = p.ProcessedItemId,
                    ParentId = p.ParentId,
                    ShipmentId = p.ShipmentId,
                    ItemId = p.ItemId,
                    ItemName = p.Item?.ItemName ?? string.Empty,
                    SKU = p.Item?.SKU ?? string.Empty,
                    InputWeight = p.InputWeight,
                    OutputWeight = p.OutputWeight,
                    Unit = p.Item?.Unit ?? string.Empty,
                    Status = p.Status,
                    ProcessedAt = p.ProcessedAt,
                    ProcessedByName = p.ProcessedByUser?.FullName ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching processed items for query {Query}", query);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }
    }
}