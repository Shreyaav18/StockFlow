using System.ComponentModel.DataAnnotations;

namespace StockFlow.Web.Models
{
    public class Item
    {
        public int ItemId { get; set; }

        [Required, MaxLength(150)]
        public string ItemName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Unit { get; set; } = "kg";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int CreatedBy { get; set; }
        public User? Creator { get; set; }
    }
}