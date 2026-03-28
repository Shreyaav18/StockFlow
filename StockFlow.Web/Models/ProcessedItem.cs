using System.ComponentModel.DataAnnotations;

namespace StockFlow.Web.Models
{
    public class ProcessedItem
    {
        public int ProcessedItemId { get; set; }

        public int? ParentId { get; set; }
        public ProcessedItem? Parent { get; set; }
        public ICollection<ProcessedItem> Children { get; set; } = new List<ProcessedItem>();

        [Required]
        public int ItemId { get; set; }
        public Item? Item { get; set; }

        public int ShipmentId { get; set; }
        public Shipment? Shipment { get; set; }

        [Required, Range(0.001, double.MaxValue)]
        public double InputWeight { get; set; }

        [Required, Range(0.001, double.MaxValue)]
        public double OutputWeight { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        public int ProcessedBy { get; set; }
        public User? ProcessedByUser { get; set; }
    }
}