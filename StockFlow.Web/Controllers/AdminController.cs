using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : BaseController
    {
        private readonly IAuthService _authService;
        private readonly IRoleService _roleService;
        private readonly IAuditLogService _auditLogService;

        public AdminController(
            IAuthService authService,
            IRoleService roleService,
            IAuditLogService auditLogService)
        {
            _authService = authService;
            _roleService = roleService;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public IActionResult Index()
            => RedirectToAction(nameof(Users));

        [HttpGet]
        public async Task<IActionResult> Users(CancellationToken ct = default)
        {
            var recentLogs = await _auditLogService.GetRecentAsync(20, ct);
            ViewBag.RecentLogs = recentLogs;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(int targetUserId, string role, CancellationToken ct = default)
        {
            try
            {
                await _roleService.AssignRoleAsync(targetUserId, role, CurrentUserId, ct);
                SetSuccessMessage($"Role updated to '{role}' successfully.");
            }
            catch (ForbiddenException ex)
            {
                SetErrorMessage(ex.Message);
            }
            catch (ValidationException ex)
            {
                SetErrorMessage(ex.Message);
            }
            catch (NotFoundException ex)
            {
                SetErrorMessage(ex.Message);
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> AuditLogs(CancellationToken ct = default)
        {
            var logs = await _auditLogService.GetRecentAsync(100, ct);
            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> AuditLogsByEntity(string entityName, int entityId, CancellationToken ct = default)
        {
            var logs = await _auditLogService.GetByEntityAsync(entityName, entityId, ct);
            ViewBag.EntityName = entityName;
            ViewBag.EntityId = entityId;
            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> AuditLogsByUser(int userId, CancellationToken ct = default)
        {
            var logs = await _auditLogService.GetByUserAsync(userId, ct);
            var user = await _authService.GetCurrentUserAsync(userId, ct);
            ViewBag.TargetUser = user;
            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> Permissions(CancellationToken ct = default)
        {
            var adminPerms = await _roleService.GetAllPermissionsForRoleAsync("Admin", ct);
            var managerPerms = await _roleService.GetAllPermissionsForRoleAsync("Manager", ct);
            var staffPerms = await _roleService.GetAllPermissionsForRoleAsync("Staff", ct);

            ViewBag.AdminPermissions = adminPerms;
            ViewBag.ManagerPermissions = managerPerms;
            ViewBag.StaffPermissions = staffPerms;

            return View();
        }
    }
}