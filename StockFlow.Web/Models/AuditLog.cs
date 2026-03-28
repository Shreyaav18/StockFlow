using System.ComponentModel.DataAnnotations;

namespace StockFlow.Web.Models
{
    public class AuditLog
    {
        public int AuditLogId { get; set; }

        [Required, MaxLength(100)]
        public string EntityName { get; set; } = string.Empty;

        public int EntityId { get; set; }

        [Required, MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        public int PerformedBy { get; set; }
        public User? PerformedByUser { get; set; }

        [MaxLength(500)]
        public string? Details { get; set; }

        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    }
}