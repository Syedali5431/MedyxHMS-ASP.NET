using Microsoft.AspNetCore.Identity;

// Purpose: Contains application code for ApplicationUser and its related runtime behavior.
namespace MedyxHMS.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? FirstLoginDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string? ProfileImage { get; set; }

        public bool MFAEnabled { get; set; }
        public string? MFASecretKey { get; set; }
        public string? MFATempSecret { get; set; }
        public string? MFARecoveryCodes { get; set; }

    }
}
