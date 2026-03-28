namespace StockFlow.Web.DTOs.Report
{
    public class DailyReportViewModel
    {
        public DateTime Date { get; set; }
        public int TotalShipmentsReceived { get; set; }
        public int TotalItemsProcessed { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public int TotalPending { get; set; }
        public double TotalInputWeight { get; set; }
        public double TotalOutputWeight { get; set; }
        public double WeightLoss { get; set; }
        public List<ItemWeightSummary> ItemBreakdown { get; set; } = new();
    }

    public class ItemWeightSummary
    {
        public string ItemName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public double TotalWeight { get; set; }
        public int Count { get; set; }
    }

    public class ItemBreakdownViewModel
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public double TotalInputWeight { get; set; }
        public double TotalOutputWeight { get; set; }
        public int ProcessingCount { get; set; }
        public List<DailyWeightPoint> DailyBreakdown { get; set; } = new();
    }

    public class DailyWeightPoint
    {
        public DateTime Date { get; set; }
        public double Weight { get; set; }
    }

    public class ProcessingStatsViewModel
    {
        public int TotalShipments { get; set; }
        public int TotalProcessed { get; set; }
        public int TotalPending { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public double AverageProcessingDepth { get; set; }
        public double TotalWeightProcessed { get; set; }
    }

    public class AuditLogDto
    {
        public string EntityName { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public int PerformedBy { get; set; }
        public string? Details { get; set; }
    }

    public class AuditLogViewModel
    {
        public int AuditLogId { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string PerformedByName { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime PerformedAt { get; set; }
    }
}