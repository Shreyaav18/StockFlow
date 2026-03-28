using System.ComponentModel.DataAnnotations;

namespace StockFlow.Web.DTOs.Item
{
    public class CreateItemDto
    {
        [Required, MaxLength(150)]
        public string ItemName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Unit { get; set; } = "kg";
    }

    public class UpdateItemDto
    {
        [Required, MaxLength(150)]
        public string ItemName { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Unit { get; set; } = "kg";

        public bool IsActive { get; set; }
    }

    public class ItemViewModel
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
    }
}