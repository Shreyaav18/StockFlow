using System.ComponentModel.DataAnnotations;

namespace StockFlow.Web.DTOs.Process
{
    public class CreateProcessDto
    {
        [Required]
        public int ShipmentId { get; set; }

        public int? ParentId { get; set; }

        [Required, MinLength(1)]
        public List<ChildItemDto> Children { get; set; } = new();
    }

    public class ChildItemDto
    {
        [Required]
        public int ItemId { get; set; }

        [Required, Range(0.001, double.MaxValue, ErrorMessage = "Weight must be greater than zero.")]
        public double OutputWeight { get; set; }
    }

    public class ProcessedItemViewModel
    {
        public int ProcessedItemId { get; set; }
        public int? ParentId { get; set; }
        public int ShipmentId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public double InputWeight { get; set; }
        public double OutputWeight { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public string ProcessedByName { get; set; } = string.Empty;
        public bool HasChildren { get; set; }
    }

    public class TreeNodeViewModel
    {
        public int ProcessedItemId { get; set; }
        public int? ParentId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public double InputWeight { get; set; }
        public double OutputWeight { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Depth { get; set; }
        public List<TreeNodeViewModel> Children { get; set; } = new();
    }
}