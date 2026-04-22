using System.ComponentModel.DataAnnotations;

// Purpose: Contains application code for PatientDtos and its related runtime behavior.
namespace MedyxHMS.DTOs
{
    public class PatientDto
    {
        public int Id { get; set; }
        public string PatientId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; } // Computed: FirstName + LastName
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; } // Computed from DateOfBirth
        public string Gender { get; set; }
        public string BloodGroup { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        public string EmergencyContactName { get; set; }
        public string EmergencyContactPhone { get; set; }
        public string MedicalHistory { get; set; }
        public string Allergies { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastVisitDate { get; set; }
    }

    public class PatientCreateDto
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string City { get; set; }

        [StringLength(100, ErrorMessage = "State cannot exceed 100 characters")]
        public string State { get; set; }

        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
        public string Country { get; set; }

        [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; }

        [Display(Name = "Blood Group")]
        public string BloodGroup { get; set; }

        [StringLength(100, ErrorMessage = "Emergency contact name cannot exceed 100 characters")]
        [Display(Name = "Emergency Contact Name")]
        public string EmergencyContactName { get; set; }

        [Phone(ErrorMessage = "Invalid emergency contact phone number")]
        [StringLength(20, ErrorMessage = "Emergency contact phone cannot exceed 20 characters")]
        [Display(Name = "Emergency Contact Phone")]
        public string EmergencyContactPhone { get; set; }

        [StringLength(1000, ErrorMessage = "Medical history cannot exceed 1000 characters")]
        [Display(Name = "Medical History")]
        public string MedicalHistory { get; set; }

        [StringLength(500, ErrorMessage = "Allergies cannot exceed 500 characters")]
        public string Allergies { get; set; }
    }

    public class PatientUpdateDto
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string City { get; set; }

        [StringLength(100, ErrorMessage = "State cannot exceed 100 characters")]
        public string State { get; set; }

        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
        public string Country { get; set; }

        [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; }

        [Display(Name = "Blood Group")]
        public string BloodGroup { get; set; }

        [StringLength(100, ErrorMessage = "Emergency contact name cannot exceed 100 characters")]
        [Display(Name = "Emergency Contact Name")]
        public string EmergencyContactName { get; set; }

        [Phone(ErrorMessage = "Invalid emergency contact phone number")]
        [StringLength(20, ErrorMessage = "Emergency contact phone cannot exceed 20 characters")]
        [Display(Name = "Emergency Contact Phone")]
        public string EmergencyContactPhone { get; set; }

        [StringLength(1000, ErrorMessage = "Medical history cannot exceed 1000 characters")]
        [Display(Name = "Medical History")]
        public string MedicalHistory { get; set; }

        [StringLength(500, ErrorMessage = "Allergies cannot exceed 500 characters")]
        public string Allergies { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
