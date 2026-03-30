using Microsoft.EntityFrameworkCore;
using Serilog;
using StockFlow.Web.Data;
using StockFlow.Web.DTOs.Process;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Services
{
    public class ProcessService : IProcessService
    {
        private readonly AppDbContext _db;
        private readonly IWeightValidatorService _weightValidator;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;
        private const int MAX_TREE_DEPTH = 10;

        public ProcessService(
            AppDbContext db,
            IWeightValidatorService weightValidator,
            INotificationService notificationService,
            IAuditLogService auditLogService)
        {
            _db = db;
            _weightValidator = weightValidator;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
        }

        public async Task<ProcessedItemViewModel> GetByIdAsync(int processedItemId, CancellationToken ct = default)
        {
            try
            {
                var item = await _db.ProcessedItems
                    .AsNoTracking()
                    .Include(p => p.Item)
                    .Include(p => p.ProcessedByUser)
                    .FirstOrDefaultAsync(p => p.ProcessedItemId == processedItemId, ct)
                    ?? throw new NotFoundException(ErrorMessages.Process.NotFound);

                return MapToViewModel(item);
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching processed item {ProcessedItemId}", processedItemId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<IEnumerable<ProcessedItemViewModel>> GetByShipmentAsync(int shipmentId, CancellationToken ct = default)
        {
            try
            {
                var items = await _db.ProcessedItems
                    .AsNoTracking()
                    .Include(p => p.Item)
                    .Include(p => p.ProcessedByUser)
                    .Where(p => p.ShipmentId == shipmentId)
                    .OrderBy(p => p.ProcessedAt)
                    .ToListAsync(ct);

                return items.Select(MapToViewModel);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching processed items for shipment {ShipmentId}", shipmentId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<ProcessedItemViewModel> ProcessAsync(CreateProcessDto dto, int processedBy, CancellationToken ct = default)
        {
            try
            {
                if (!dto.Children.Any())
                    throw new ValidationException(ErrorMessages.Process.NoChildren);

                var shipment = await _db.Shipments.FindAsync(new object[] { dto.ShipmentId }, ct)
                    ?? throw new NotFoundException(ErrorMessages.Shipment.NotFound);

                if (shipment.Status == "Processed")
                    throw new ConflictException(ErrorMessages.Shipment.AlreadyProcessed);

                double inputWeight;

                if (dto.ParentId.HasValue)
                {
                    var depth = await GetDepthAsync(dto.ParentId.Value, ct);
                    if (depth >= MAX_TREE_DEPTH)
                        throw new ValidationException(ErrorMessages.Process.InvalidDepth);

                    await _weightValidator.ValidateChildWeightsAsync(
                        dto.ParentId.Value,
                        dto.Children.Select(c => c.OutputWeight),
                        ct);

                    inputWeight = await _db.ProcessedItems
                        .AsNoTracking()
                        .Where(p => p.ProcessedItemId == dto.ParentId.Value)
                        .Select(p => p.OutputWeight)
                        .FirstOrDefaultAsync(ct);
                }
                else
                {
                    var childTotal = dto.Children.Sum(c => c.OutputWeight);
                    if (childTotal > shipment.TotalWeight)
                        throw new WeightValidationException(shipment.TotalWeight, childTotal);

                    inputWeight = shipment.TotalWeight;
                }

                var savedItems = new List<Models.ProcessedItem>();

                foreach (var child in dto.Children)
                {
                    await _weightValidator.ValidateSingleWeightAsync(child.OutputWeight, "Output weight", ct);

                    var processedItem = new Models.ProcessedItem
                    {
                        ParentId = dto.ParentId,
                        ItemId = child.ItemId,
                        ShipmentId = dto.ShipmentId,
                        InputWeight = inputWeight,
                        OutputWeight = child.OutputWeight,
                        Status = "Pending",
                        ProcessedAt = DateTime.UtcNow,
                        ProcessedBy = processedBy
                    };

                    _db.ProcessedItems.Add(processedItem);
                    savedItems.Add(processedItem);
                }

                await _db.SaveChangesAsync(ct);

                shipment.Status = "InProgress";
                await _db.SaveChangesAsync(ct);

                foreach (var saved in savedItems)
                {
                    await _auditLogService.LogAsync(new DTOs.Report.AuditLogDto
                    {
                        EntityName = "ProcessedItem",
                        EntityId = saved.ProcessedItemId,
                        Action = "Process",
                        PerformedBy = processedBy,
                        Details = $"ParentId: {dto.ParentId}, OutputWeight: {saved.OutputWeight}"
                    }, ct);
                }

                await _notificationService.NotifyProcessingCompleteAsync(dto.ShipmentId, savedItems.First().ProcessedItemId, ct);
                await _notificationService.NotifyApprovalRequiredAsync(savedItems.First().ProcessedItemId, ct);

                Log.Information("Shipment {ShipmentId} processed into {Count} children by user {UserId}",
                    dto.ShipmentId, savedItems.Count, processedBy);

                return await GetByIdAsync(savedItems.First().ProcessedItemId, ct);
            }
            catch (AppException) { throw; }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error processing shipment {ShipmentId}", dto.ShipmentId);
                throw new DatabaseException();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error processing shipment {ShipmentId}", dto.ShipmentId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<ProcessedItemViewModel> ApproveAsync(int processedItemId, int approvedBy, CancellationToken ct = default)
        {
            try
            {
                var item = await _db.ProcessedItems.FindAsync(new object[] { processedItemId }, ct)
                    ?? throw new NotFoundException(ErrorMessages.Process.NotFound);

                if (item.Status == "Approved")
                    throw new ConflictException(ErrorMessages.Process.AlreadyApproved);

                if (item.Status == "Rejected")
                    throw new ConflictException(ErrorMessages.Process.AlreadyRejected);

                if (item.ProcessedBy == approvedBy)
                    throw new ForbiddenException(ErrorMessages.Process.CannotApproveOwn);

                item.Status = "Approved";
                await _db.SaveChangesAsync(ct);

                await _auditLogService.LogAsync(new DTOs.Report.AuditLogDto
                {
                    EntityName = "ProcessedItem",
                    EntityId = processedItemId,
                    Action = "Approve",
                    PerformedBy = approvedBy
                }, ct);

                await _notificationService.NotifyApprovalDecisionAsync(processedItemId, "Approved", ct);

                Log.Information("ProcessedItem {ProcessedItemId} approved by user {UserId}", processedItemId, approvedBy);
                return await GetByIdAsync(processedItemId, ct);
            }
            catch (AppException) { throw; }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error approving processed item {ProcessedItemId}", processedItemId);
                throw new DatabaseException();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error approving processed item {ProcessedItemId}", processedItemId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<ProcessedItemViewModel> RejectAsync(int processedItemId, string reason, int rejectedBy, CancellationToken ct = default)
        {
            try
            {
                var item = await _db.ProcessedItems.FindAsync(new object[] { processedItemId }, ct)
                    ?? throw new NotFoundException(ErrorMessages.Process.NotFound);

                if (item.Status == "Approved")
                    throw new ConflictException(ErrorMessages.Process.AlreadyApproved);

                if (item.Status == "Rejected")
                    throw new ConflictException(ErrorMessages.Process.AlreadyRejected);

                item.Status = "Rejected";
                await _db.SaveChangesAsync(ct);

                await _auditLogService.LogAsync(new DTOs.Report.AuditLogDto
                {
                    EntityName = "ProcessedItem",
                    EntityId = processedItemId,
                    Action = "Reject",
                    PerformedBy = rejectedBy,
                    Details = $"Reason: {reason}"
                }, ct);

                await _notificationService.NotifyApprovalDecisionAsync(processedItemId, "Rejected", ct);

                Log.Information("ProcessedItem {ProcessedItemId} rejected by user {UserId}", processedItemId, rejectedBy);
                return await GetByIdAsync(processedItemId, ct);
            }
            catch (AppException) { throw; }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error rejecting processed item {ProcessedItemId}", processedItemId);
                throw new DatabaseException();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error rejecting processed item {ProcessedItemId}", processedItemId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<IEnumerable<ProcessedItemViewModel>> GetChildrenAsync(int parentId, CancellationToken ct = default)
        {
            try
            {
                var children = await _db.ProcessedItems
                    .AsNoTracking()
                    .Include(p => p.Item)
                    .Include(p => p.ProcessedByUser)
                    .Where(p => p.ParentId == parentId)
                    .ToListAsync(ct);

                return children.Select(MapToViewModel);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching children for parent {ParentId}", parentId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<IEnumerable<ProcessedItemViewModel>> GetPendingApprovalsAsync(CancellationToken ct = default)
        {
            try
            {
                var items = await _db.ProcessedItems
                    .AsNoTracking()
                    .Include(p => p.Item)
                    .Include(p => p.ProcessedByUser)
                    .Where(p => p.Status == "Pending")
                    .OrderBy(p => p.ProcessedAt)
                    .ToListAsync(ct);

                return items.Select(MapToViewModel);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching pending approvals");
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        private async Task<int> GetDepthAsync(int processedItemId, CancellationToken ct)
        {
            var depth = 0;
            var currentId = (int?)processedItemId;

            while (currentId.HasValue)
            {
                var parentId = await _db.ProcessedItems
                    .AsNoTracking()
                    .Where(p => p.ProcessedItemId == currentId.Value)
                    .Select(p => p.ParentId)
                    .FirstOrDefaultAsync(ct);

                currentId = parentId;
                depth++;

                if (depth > MAX_TREE_DEPTH)
                    throw new ValidationException(ErrorMessages.Process.InvalidDepth);
            }

            return depth;
        }

        private static ProcessedItemViewModel MapToViewModel(Models.ProcessedItem p) => new()
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
            ProcessedByName = p.ProcessedByUser?.FullName ?? string.Empty,
            HasChildren = p.Children.Any()
        };
    }
}