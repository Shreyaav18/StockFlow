using Microsoft.EntityFrameworkCore;
using Serilog;
using StockFlow.Web.Data;
using StockFlow.Web.DTOs.Shipment;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Services
{
    public class ShipmentService : IShipmentService
    {
        private readonly AppDbContext _db;
        private readonly IAuditLogService _auditLogService;
        private readonly IConfiguration _config;

        public ShipmentService(AppDbContext db, IAuditLogService auditLogService, IConfiguration config)
        {
            _db = db;
            _auditLogService = auditLogService;
            _config = config;
        }

        public async Task<ShipmentViewModel> GetByIdAsync(int shipmentId, CancellationToken ct = default)
        {
            try
            {
                var shipment = await _db.Shipments
                    .AsNoTracking()
                    .Include(s => s.Item)
                    .Include(s => s.ReceivedByUser)
                    .FirstOrDefaultAsync(s => s.ShipmentId == shipmentId, ct)
                    ?? throw new NotFoundException(ErrorMessages.Shipment.NotFound);

                return MapToViewModel(shipment);
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching shipment {ShipmentId}", shipmentId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<IEnumerable<ShipmentViewModel>> GetAllAsync(CancellationToken ct = default)
        {
            try
            {
                var shipments = await _db.Shipments
                    .AsNoTracking()
                    .Include(s => s.Item)
                    .Include(s => s.ReceivedByUser)
                    .OrderByDescending(s => s.ReceivedAt)
                    .ToListAsync(ct);

                return shipments.Select(MapToViewModel);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching all shipments");
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<IEnumerable<ShipmentViewModel>> GetPendingAsync(CancellationToken ct = default)
        {
            try
            {
                var shipments = await _db.Shipments
                    .AsNoTracking()
                    .Include(s => s.Item)
                    .Include(s => s.ReceivedByUser)
                    .Where(s => s.Status == "Pending")
                    .OrderBy(s => s.ReceivedAt)
                    .ToListAsync(ct);

                return shipments.Select(MapToViewModel);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching pending shipments");
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<IEnumerable<ShipmentViewModel>> GetStaleAsync(int olderThanHours, CancellationToken ct = default)
        {
            try
            {
                var threshold = DateTime.UtcNow.AddHours(-olderThanHours);

                var shipments = await _db.Shipments
                    .AsNoTracking()
                    .Include(s => s.Item)
                    .Include(s => s.ReceivedByUser)
                    .Where(s => s.Status == "Pending" && s.ReceivedAt < threshold)
                    .OrderBy(s => s.ReceivedAt)
                    .ToListAsync(ct);

                return shipments.Select(MapToViewModel);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching stale shipments");
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<ShipmentViewModel> ReceiveAsync(CreateShipmentDto dto, int receivedBy, CancellationToken ct = default)
        {
            try
            {
                var itemExists = await _db.Items.AnyAsync(i => i.ItemId == dto.ItemId && i.IsActive, ct);
                if (!itemExists)
                    throw new NotFoundException(ErrorMessages.Item.NotFound);

                var shipment = new Models.Shipment
                {
                    ItemId = dto.ItemId,
                    TotalWeight = dto.TotalWeight,
                    Status = "Pending",
                    ReceivedAt = DateTime.UtcNow,
                    ReceivedBy = receivedBy
                };

                _db.Shipments.Add(shipment);
                await _db.SaveChangesAsync(ct);

                await _auditLogService.LogAsync(new DTOs.Report.AuditLogDto
                {
                    EntityName = "Shipment",
                    EntityId = shipment.ShipmentId,
                    Action = "Receive",
                    PerformedBy = receivedBy,
                    Details = $"ItemId: {dto.ItemId}, Weight: {dto.TotalWeight}"
                }, ct);

                Log.Information("Shipment {ShipmentId} received by user {UserId}", shipment.ShipmentId, receivedBy);
                return await GetByIdAsync(shipment.ShipmentId, ct);
            }
            catch (AppException) { throw; }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error receiving shipment");
                throw new DatabaseException();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error receiving shipment");
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task UpdateStatusAsync(int shipmentId, string status, int updatedBy, CancellationToken ct = default)
        {
            try
            {
                var shipment = await _db.Shipments.FindAsync(new object[] { shipmentId }, ct)
                    ?? throw new NotFoundException(ErrorMessages.Shipment.NotFound);

                var previousStatus = shipment.Status;
                shipment.Status = status;
                await _db.SaveChangesAsync(ct);

                await _auditLogService.LogAsync(new DTOs.Report.AuditLogDto
                {
                    EntityName = "Shipment",
                    EntityId = shipmentId,
                    Action = "StatusUpdate",
                    PerformedBy = updatedBy,
                    Details = $"Status changed from {previousStatus} to {status}"
                }, ct);

                Log.Information("Shipment {ShipmentId} status updated to {Status}", shipmentId, status);
            }
            catch (AppException) { throw; }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error updating status for shipment {ShipmentId}", shipmentId);
                throw new DatabaseException();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error updating status for shipment {ShipmentId}", shipmentId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task DeleteAsync(int shipmentId, int deletedBy, CancellationToken ct = default)
        {
            try
            {
                var shipment = await _db.Shipments.FindAsync(new object[] { shipmentId }, ct)
                    ?? throw new NotFoundException(ErrorMessages.Shipment.NotFound);

                if (shipment.Status != "Pending")
                    throw new ConflictException(ErrorMessages.Shipment.CannotDelete);

                _db.Shipments.Remove(shipment);
                await _db.SaveChangesAsync(ct);

                await _auditLogService.LogAsync(new DTOs.Report.AuditLogDto
                {
                    EntityName = "Shipment",
                    EntityId = shipmentId,
                    Action = "Delete",
                    PerformedBy = deletedBy
                }, ct);

                Log.Information("Shipment {ShipmentId} deleted by user {UserId}", shipmentId, deletedBy);
            }
            catch (AppException) { throw; }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error deleting shipment {ShipmentId}", shipmentId);
                throw new DatabaseException();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error deleting shipment {ShipmentId}", shipmentId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        private ShipmentViewModel MapToViewModel(Models.Shipment s)
        {
            var staleHours = _config.GetValue<int>("App:UnprocessedAlertHours", 24);
            return new ShipmentViewModel
            {
                ShipmentId = s.ShipmentId,
                ItemId = s.ItemId,
                ItemName = s.Item?.ItemName ?? string.Empty,
                SKU = s.Item?.SKU ?? string.Empty,
                TotalWeight = s.TotalWeight,
                Unit = s.Item?.Unit ?? string.Empty,
                Status = s.Status,
                ReceivedAt = s.ReceivedAt,
                ReceivedByName = s.ReceivedByUser?.FullName ?? string.Empty,
                IsStale = s.Status == "Pending" && s.ReceivedAt < DateTime.UtcNow.AddHours(-staleHours)
            };
        }
    }
}