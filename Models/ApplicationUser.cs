using Microsoft.AspNetCore.Identity;

namespace MedyxHMS.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginDate { get; set; }
        public string? ProfileImage { get; set; }

        // Navigation properties
        public ICollection<StaffRole> StaffRoles { get; set; }
    }
}