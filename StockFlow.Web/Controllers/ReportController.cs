using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Controllers
{
    [Authorize(Policy = "ManagerAndAbove")]
    public class ReportController : BaseController
    {
        private readonly IReportService _reportService;
        private readonly IExportService _exportService;
        private readonly IItemService _itemService;

        public ReportController(
            IReportService reportService,
            IExportService exportService,
            IItemService itemService)
        {
            _reportService = reportService;
            _exportService = exportService;
            _itemService = itemService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Today = DateTime.UtcNow.Date;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Daily(DateTime? date, CancellationToken ct = default)
        {
            var targetDate = date ?? DateTime.UtcNow.Date;
            var report = await _reportService.GetDailyReportAsync(targetDate, ct);
            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> Range(DateTime? from, DateTime? to, CancellationToken ct = default)
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-7).Date;
            var toDate = to ?? DateTime.UtcNow.Date;

            if (fromDate > toDate)
            {
                SetErrorMessage("From date cannot be after to date.");
                return View();
            }

            if ((toDate - fromDate).TotalDays > 90)
            {
                SetErrorMessage("Date range cannot exceed 90 days.");
                return View();
            }

            var reports = await _reportService.GetRangeReportAsync(fromDate, toDate, ct);
            ViewBag.From = fromDate;
            ViewBag.To = toDate;
            return View(reports);
        }

        [HttpGet]
        public async Task<IActionResult> ItemBreakdown(int itemId, DateTime? from, DateTime? to, CancellationToken ct = default)
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-30).Date;
            var toDate = to ?? DateTime.UtcNow.Date;

            try
            {
                var breakdown = await _reportService.GetItemBreakdownAsync(itemId, fromDate, toDate, ct);
                ViewBag.Items = await _itemService.GetAllAsync(ct);
                return View(breakdown);
            }
            catch (NotFoundException ex)
            {
                SetErrorMessage(ex.Message);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Stats(CancellationToken ct = default)
        {
            var stats = await _reportService.GetProcessingStatsAsync(ct);
            return View(stats);
        }

        [HttpGet]
        public async Task<IActionResult> ExportTreePdf(int shipmentId, CancellationToken ct = default)
        {
            try
            {
                var bytes = await _exportService.ExportTreeToPdfAsync(shipmentId, ct);
                return File(bytes, "application/pdf", $"tree-shipment-{shipmentId}-{DateTime.UtcNow:yyyyMMdd}.pdf");
            }
            catch (ExportException ex)
            {
                SetErrorMessage(ex.Message);
                return RedirectToAction("Tree", "Process", new { shipmentId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportTreeExcel(int shipmentId, CancellationToken ct = default)
        {
            try
            {
                var bytes = await _exportService.ExportTreeToExcelAsync(shipmentId, ct);
                return File(bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"tree-shipment-{shipmentId}-{DateTime.UtcNow:yyyyMMdd}.xlsx");
            }
            catch (ExportException ex)
            {
                SetErrorMessage(ex.Message);
                return RedirectToAction("Tree", "Process", new { shipmentId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportReportPdf(DateTime from, DateTime to, CancellationToken ct = default)
        {
            try
            {
                var bytes = await _exportService.ExportReportToPdfAsync(from, to, ct);
                return File(bytes, "application/pdf",
                    $"report-{from:yyyyMMdd}-to-{to:yyyyMMdd}.pdf");
            }
            catch (ExportException ex)
            {
                SetErrorMessage(ex.Message);
                return RedirectToAction(nameof(Range));
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportReportExcel(DateTime from, DateTime to, CancellationToken ct = default)
        {
            try
            {
                var bytes = await _exportService.ExportReportToExcelAsync(from, to, ct);
                return File(bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"report-{from:yyyyMMdd}-to-{to:yyyyMMdd}.xlsx");
            }
            catch (ExportException ex)
            {
                SetErrorMessage(ex.Message);
                return RedirectToAction(nameof(Range));
            }
        }
    }
}