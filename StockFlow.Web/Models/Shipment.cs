using System.ComponentModel.DataAnnotations;

namespace StockFlow.Web.Models
{
    public class Shipment
    {
        public int ShipmentId { get; set; }

        [Required]
        public int ItemId { get; set; }
        public Item? Item { get; set; }

        [Required, Range(0.001, double.MaxValue)]
        public double TotalWeight { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        public int ReceivedBy { get; set; }
        public User? ReceivedByUser { get; set; }
    }
}