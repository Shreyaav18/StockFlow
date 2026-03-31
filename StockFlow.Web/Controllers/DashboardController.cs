using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Controllers
{
    [Authorize(Policy = "AllStaff")]
    public class DashboardController : BaseController
    {
        private readonly IReportService _reportService;
        private readonly IShipmentService _shipmentService;
        private readonly IProcessService _processService;

        public DashboardController(
            IReportService reportService,
            IShipmentService shipmentService,
            IProcessService processService)
        {
            _reportService = reportService;
            _shipmentService = shipmentService;
            _processService = processService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            var stats = await _reportService.GetProcessingStatsAsync(ct);
            var pending = await _shipmentService.GetPendingAsync(ct);

            ViewBag.Stats = stats;
            ViewBag.PendingShipments = pending.Take(5);
            ViewBag.UserRole = CurrentUserRole;

            if (CurrentUserRole is "Admin" or "Manager")
            {
                var pendingApprovals = await _processService.GetPendingApprovalsAsync(ct);
                ViewBag.PendingApprovals = pendingApprovals.Take(5);
            }

            return View();
        }
    }
}