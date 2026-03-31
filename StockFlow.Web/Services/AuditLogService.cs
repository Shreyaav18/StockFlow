using Microsoft.EntityFrameworkCore;
using Serilog;
using StockFlow.Web.Data;
using StockFlow.Web.DTOs.Report;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Models;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _db;

        public AuditLogService(AppDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(AuditLogDto dto, CancellationToken ct = default)
        {
            try
            {
                var log = new AuditLog
                {
                    EntityName = dto.EntityName,
                    EntityId = dto.EntityId,
                    Action = dto.Action,
                    PerformedBy = dto.PerformedBy,
                    Details = dto.Details,
                    PerformedAt = DateTime.UtcNow
                };

                _db.AuditLogs.Add(log);
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to write audit log for {EntityName} {EntityId} action {Action}",
                    dto.EntityName, dto.EntityId, dto.Action);
            }
        }

        public async Task<IEnumerable<AuditLogViewModel>> GetByEntityAsync(string entityName, int entityId, CancellationToken ct = default)
        {
            try
            {
                var logs = await _db.AuditLogs
                    .AsNoTracking()
                    .Include(a => a.PerformedByUser)
                    .Where(a => a.EntityName == entityName && a.EntityId == entityId)
                    .OrderByDescending(a => a.PerformedAt)
                    .ToListAsync(ct);

                return logs.Select(MapToViewModel);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching audit logs for {EntityName} {EntityId}", entityName, entityId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<IEnumerable<AuditLogViewModel>> GetByUserAsync(int userId, CancellationToken ct = default)
        {
            try
            {
                var logs = await _db.AuditLogs
                    .AsNoTracking()
                    .Include(a => a.PerformedByUser)
                    .Where(a => a.PerformedBy == userId)
                    .OrderByDescending(a => a.PerformedAt)
                    .ToListAsync(ct);

                return logs.Select(MapToViewModel);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching audit logs for user {UserId}", userId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<IEnumerable<AuditLogViewModel>> GetRecentAsync(int count = 50, CancellationToken ct = default)
        {
            try
            {
                var logs = await _db.AuditLogs
                    .AsNoTracking()
                    .Include(a => a.PerformedByUser)
                    .OrderByDescending(a => a.PerformedAt)
                    .Take(count)
                    .ToListAsync(ct);

                return logs.Select(MapToViewModel);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching recent audit logs");
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        private static AuditLogViewModel MapToViewModel(AuditLog a) => new()
        {
            AuditLogId = a.AuditLogId,
            EntityName = a.EntityName,
            EntityId = a.EntityId,
            Action = a.Action,
            PerformedByName = a.PerformedByUser?.FullName ?? "System",
            Details = a.Details,
            PerformedAt = a.PerformedAt
        };
    }
}