using System.ComponentModel.DataAnnotations;
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
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? FirstLoginDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string? ProfileImage { get; set; }

        // Personal details the user maintains in My Profile.
        [StringLength(20)]
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        [StringLength(300)]
        public string? Address { get; set; }
        [StringLength(100)]
        public string? City { get; set; }
        [StringLength(150)]
        public string? EmergencyContactName { get; set; }
        [StringLength(30)]
        public string? EmergencyContactPhone { get; set; }
        [StringLength(1000)]
        public string? About { get; set; }

        public bool MFAEnabled { get; set; }
        public string? MFASecretKey { get; set; }
        public string? MFATempSecret { get; set; }
        public string? MFARecoveryCodes { get; set; }

    }
}
