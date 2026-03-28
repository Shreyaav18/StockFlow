using System.ComponentModel.DataAnnotations;

namespace StockFlow.Web.DTOs.Shipment
{
    public class CreateShipmentDto
    {
        [Required]
        public int ItemId { get; set; }

        [Required, Range(0.001, double.MaxValue, ErrorMessage = "Weight must be greater than zero.")]
        public double TotalWeight { get; set; }
    }

    public class ShipmentViewModel
    {
        public int ShipmentId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public double TotalWeight { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public string ReceivedByName { get; set; } = string.Empty;
        public bool IsStale { get; set; }
    }
}