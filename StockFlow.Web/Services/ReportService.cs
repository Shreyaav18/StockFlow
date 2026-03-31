using Microsoft.EntityFrameworkCore;
using Serilog;
using StockFlow.Web.Data;
using StockFlow.Web.DTOs.Report;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Services
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _db;

        public ReportService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<DailyReportViewModel> GetDailyReportAsync(DateTime date, CancellationToken ct = default)
        {
            try
            {
                var start = date.Date;
                var end = start.AddDays(1);

                var shipments = await _db.Shipments
                    .AsNoTracking()
                    .Where(s => s.ReceivedAt >= start && s.ReceivedAt < end)
                    .CountAsync(ct);

                var processed = await _db.ProcessedItems
                    .AsNoTracking()
                    .Include(p => p.Item)
                    .Where(p => p.ProcessedAt >= start && p.ProcessedAt < end)
                    .ToListAsync(ct);

                var breakdown = processed
                    .GroupBy(p => new { p.Item!.ItemName, p.Item.SKU })
                    .Select(g => new ItemWeightSummary
                    {
                        ItemName = g.Key.ItemName,
                        SKU = g.Key.SKU,
                        TotalWeight = g.Sum(p => p.OutputWeight),
                        Count = g.Count()
                    })
                    .ToList();

                return new DailyReportViewModel
                {
                    Date = date,
                    TotalShipmentsReceived = shipments,
                    TotalItemsProcessed = processed.Count,
                    TotalApproved = processed.Count(p => p.Status == "Approved"),
                    TotalRejected = processed.Count(p => p.Status == "Rejected"),
                    TotalPending = processed.Count(p => p.Status == "Pending"),
                    TotalInputWeight = processed.Sum(p => p.InputWeight),
                    TotalOutputWeight = processed.Sum(p => p.OutputWeight),
                    WeightLoss = processed.Sum(p => p.InputWeight) - processed.Sum(p => p.OutputWeight),
                    ItemBreakdown = breakdown
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating daily report for {Date}", date);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<IEnumerable<DailyReportViewModel>> GetRangeReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
        {
            try
            {
                var reports = new List<DailyReportViewModel>();
                var current = from.Date;

                while (current <= to.Date)
                {
                    reports.Add(await GetDailyReportAsync(current, ct));
                    current = current.AddDays(1);
                }

                return reports;
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating range report from {From} to {To}", from, to);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<ItemBreakdownViewModel> GetItemBreakdownAsync(int itemId, DateTime from, DateTime to, CancellationToken ct = default)
        {
            try
            {
                var item = await _db.Items.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.ItemId == itemId, ct)
                    ?? throw new NotFoundException(ErrorMessages.Item.NotFound);

                var processed = await _db.ProcessedItems
                    .AsNoTracking()
                    .Where(p => p.ItemId == itemId && p.ProcessedAt >= from && p.ProcessedAt <= to)
                    .ToListAsync(ct);

                var daily = processed
                    .GroupBy(p => p.ProcessedAt.Date)
                    .Select(g => new DailyWeightPoint
                    {
                        Date = g.Key,
                        Weight = g.Sum(p => p.OutputWeight)
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                return new ItemBreakdownViewModel
                {
                    ItemId = itemId,
                    ItemName = item.ItemName,
                    SKU = item.SKU,
                    From = from,
                    To = to,
                    TotalInputWeight = processed.Sum(p => p.InputWeight),
                    TotalOutputWeight = processed.Sum(p => p.OutputWeight),
                    ProcessingCount = processed.Count,
                    DailyBreakdown = daily
                };
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating item breakdown for item {ItemId}", itemId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public async Task<ProcessingStatsViewModel> GetProcessingStatsAsync(CancellationToken ct = default)
        {
            try
            {
                var totalShipments = await _db.Shipments.CountAsync(ct);
                var allProcessed = await _db.ProcessedItems.AsNoTracking().ToListAsync(ct);

                return new ProcessingStatsViewModel
                {
                    TotalShipments = totalShipments,
                    TotalProcessed = allProcessed.Count,
                    TotalPending = allProcessed.Count(p => p.Status == "Pending"),
                    TotalApproved = allProcessed.Count(p => p.Status == "Approved"),
                    TotalRejected = allProcessed.Count(p => p.Status == "Rejected"),
                    TotalWeightProcessed = allProcessed.Sum(p => p.OutputWeight),
                    AverageProcessingDepth = allProcessed.Count > 0
                        ? allProcessed.Average(p => p.ParentId.HasValue ? 1 : 0)
                        : 0
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating processing stats");
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }
    }
}