using System.ComponentModel.DataAnnotations;

// Purpose: Contains application code for UserSession and its related runtime behavior.
namespace MedyxHMS.Models
{
    public class UserSession
    {
        public int Id { get; set; }

        [Required, MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required, MaxLength(128)]
        public string SessionId { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string ActiveRole { get; set; } = string.Empty;

        [MaxLength(64)]
        public string? IpAddress { get; set; }

        [MaxLength(512)]
        public string? UserAgent { get; set; }

        public DateTime LoginAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;

        public DateTime? LogoutAtUtc { get; set; }

        public bool IsActive { get; set; } = true;

        public ApplicationUser? User { get; set; }
    }
}
