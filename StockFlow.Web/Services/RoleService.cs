using Microsoft.EntityFrameworkCore;
using Serilog;
using StockFlow.Web.Data;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Services
{
    public class RoleService : IRoleService
    {
        private readonly AppDbContext _db;
        private readonly IAuditLogService _auditLogService;

        private static readonly Dictionary<string, int> RoleHierarchy = new()
        {
            { "Staff",   1 },
            { "Manager", 2 },
            { "Admin",   3 }
        };

        private static readonly Dictionary<string, HashSet<string>> RolePermissions = new()
        {
            ["Staff"] = new()
            {
                "shipment.receive",
                "process.execute",
                "process.view",
                "item.view",
                "search.use",
                "tree.view"
            },
            ["Manager"] = new()
            {
                "shipment.receive",
                "shipment.view",
                "process.execute",
                "process.view",
                "process.approve",
                "process.reject",
                "item.create",
                "item.view",
                "item.update",
                "search.use",
                "tree.view",
                "report.view",
                "export.use"
            },
            ["Admin"] = new()
            {
                "shipment.receive",
                "shipment.view",
                "shipment.delete",
                "process.execute",
                "process.view",
                "process.approve",
                "process.reject",
                "item.create",
                "item.view",
                "item.update",
                "item.delete",
                "search.use",
                "tree.view",
                "report.view",
                "export.use",
                "user.manage",
                "audit.view",
                "hangfire.access"
            }
        };

        public RoleService(AppDbContext db, IAuditLogService auditLogService)
        {
            _db = db;
            _auditLogService = auditLogService;
        }

        public async Task<bool> HasPermissionAsync(int userId, string permission, CancellationToken ct = default)
        {
            try
            {
                var role = await GetRoleAsync(userId, ct);
                return RolePermissions.TryGetValue(role, out var permissions) &&
                       permissions.Contains(permission);
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking permission {Permission} for user {UserId}", permission, userId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<string> GetRoleAsync(int userId, CancellationToken ct = default)
        {
            try
            {
                var role = await _db.Users
                    .AsNoTracking()
                    .Where(u => u.UserId == userId)
                    .Select(u => u.Role)
                    .FirstOrDefaultAsync(ct)
                    ?? throw new NotFoundException(ErrorMessages.Auth.AccountNotFound);

                return role;
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching role for user {UserId}", userId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task AssignRoleAsync(int targetUserId, string role, int requestedByUserId, CancellationToken ct = default)
        {
            try
            {
                if (!RoleHierarchy.ContainsKey(role))
                    throw new ValidationException(ErrorMessages.Auth.InsufficientRole);

                var requesterRole = await GetRoleAsync(requestedByUserId, ct);

                if (!IsRoleHigherOrEqual(requesterRole, role))
                    throw new ForbiddenException(ErrorMessages.Auth.InsufficientRole);

                var user = await _db.Users.FindAsync(new object[] { targetUserId }, ct)
                    ?? throw new NotFoundException(ErrorMessages.Auth.AccountNotFound);

                var previousRole = user.Role;
                user.Role = role;
                await _db.SaveChangesAsync(ct);

                await _auditLogService.LogAsync(new DTOs.Report.AuditLogDto
                {
                    EntityName = "User",
                    EntityId = targetUserId,
                    Action = "RoleAssigned",
                    PerformedBy = requestedByUserId,
                    Details = $"Role changed from {previousRole} to {role}"
                }, ct);

                Log.Information("User {TargetUserId} role changed to {Role} by {RequestedByUserId}",
                    targetUserId, role, requestedByUserId);
            }
            catch (AppException) { throw; }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Database error assigning role for user {UserId}", targetUserId);
                throw new DatabaseException();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error assigning role for user {UserId}", targetUserId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public Task<IEnumerable<string>> GetAllPermissionsForRoleAsync(string role, CancellationToken ct = default)
        {
            try
            {
                if (!RolePermissions.TryGetValue(role, out var permissions))
                    throw new ValidationException(ErrorMessages.Auth.InsufficientRole);

                return Task.FromResult<IEnumerable<string>>(permissions);
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching permissions for role {Role}", role);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public bool IsRoleHigherOrEqual(string requesterRole, string targetRole)
        {
            var requesterLevel = RoleHierarchy.GetValueOrDefault(requesterRole, 0);
            var targetLevel = RoleHierarchy.GetValueOrDefault(targetRole, 0);
            return requesterLevel >= targetLevel;
        }
    }
}