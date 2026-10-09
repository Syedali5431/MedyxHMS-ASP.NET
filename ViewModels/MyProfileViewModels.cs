using System.ComponentModel.DataAnnotations;

// Purpose: My Profile – the personal details every signed-in staff user can change themselves. E-mail, user name,
// employee ID and roles are managed by administrators (User Management).
namespace MedyxHMS.ViewModels
{
    public class MyProfileEditViewModel
    {
        public static readonly string[] Genders = { "Female", "Male", "Other", "Prefer not to say" };

        [Required, StringLength(50), Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50), Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Phone, StringLength(30), Display(Name = "Phone")]
        public string? PhoneNumber { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [DataType(DataType.Date), Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(150), Display(Name = "Emergency contact")]
        public string? EmergencyContactName { get; set; }

        [Phone, StringLength(30), Display(Name = "Emergency contact phone")]
        public string? EmergencyContactPhone { get; set; }

        [StringLength(1000), Display(Name = "About me")]
        public string? About { get; set; }
    }
}
