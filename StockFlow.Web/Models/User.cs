using System.ComponentModel.DataAnnotations;

namespace StockFlow.Web.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Staff";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}