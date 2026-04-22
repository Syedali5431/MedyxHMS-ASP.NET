using MedyxHMS.Data;
using MedyxHMS.Models;
using System.ComponentModel.DataAnnotations;

// Purpose: Contains application code for PatientPortalDtos and its related runtime behavior.
namespace MedyxHMS.DTOs
{
    // Patient Portal DTOs for patient-facing operations

    public class PatientPortalDto
    {
        public string Id { get; set; }
        public string PatientId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int Age => DateTime.Now.Year - DateOfBirth.Year;
        public string Gender { get; set; }
        public string BloodGroup { get; set; }
        public string Address { get; set; }
        public string GuardianName { get; set; }
        public string GuardianPhone { get; set; }
        public string MaritalStatus { get; set; }
        public string Occupation { get; set; }
        public string EmergencyContactName { get; set; }
        public string EmergencyContactPhone { get; set; }
        public string EmergencyContactRelation { get; set; }
        public string ProfileImagePath { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string StatusText => IsActive ? "Active" : "Inactive";
        public string StatusBadgeClass => IsActive ? "badge-success" : "badge-danger";
        public string FormattedLastLogin => LastLoginDate?.ToString("MMM dd, yyyy 'at' hh:mm tt") ?? "Never";
    }

    public class PatientPortalCreateDto
    {
        [Required(ErrorMessage = "Patient ID is required")]
        [StringLength(20, ErrorMessage = "Patient ID cannot exceed 20 characters")]
        public string PatientId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        public string BloodGroup { get; set; }
        public string Address { get; set; }
        public string GuardianName { get; set; }
        public string GuardianPhone { get; set; }
        public string MaritalStatus { get; set; }
        public string Occupation { get; set; }
        public string EmergencyContactName { get; set; }
        public string EmergencyContactPhone { get; set; }
        public string EmergencyContactRelation { get; set; }
    }

    public class PatientPortalUpdateDto
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; }

        public string BloodGroup { get; set; }
        public string Address { get; set; }
        public string GuardianName { get; set; }
        public string GuardianPhone { get; set; }
        public string MaritalStatus { get; set; }
        public string Occupation { get; set; }
        public string EmergencyContactName { get; set; }
        public string EmergencyContactPhone { get; set; }
        public string EmergencyContactRelation { get; set; }
    }

    public class PatientPortalPasswordChangeDto
    {
        public string PatientId { get; set; }

        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }

    public class PatientPortalLoginDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public class PatientPortalRegisterDto
    {
        [Required(ErrorMessage = "User name is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "User name must be between 3 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "User name can contain letters, numbers, dot, underscore, and hyphen only")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Terms and conditions must be accepted")]
        public bool AcceptTerms { get; set; }
    }

    public class PatientPortalAppointmentDto
    {
        public string Id { get; set; }
        public int AppointmentId { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public string FormattedAppointmentDate => AppointmentDate?.ToString("MMM dd, yyyy 'at' hh:mm tt") ?? "N/A";
        public string DoctorName { get; set; }
        public string Department { get; set; }
        public string Status { get; set; }
        public string StatusBadgeClass => Status switch
        {
            "Pending" => "badge-warning",
            "Confirmed" => "badge-info",
            "Completed" => "badge-success",
            "Cancelled" => "badge-danger",
            _ => "badge-secondary"
        };
        public string Symptoms { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal? ConsultationFee { get; set; }
        public bool CanReschedule => Status == "Pending" || Status == "Confirmed";
        public bool CanCancel => Status == "Pending" || Status == "Confirmed";
    }

    public class PatientPortalAppointmentCreateDto
    {
        [Required(ErrorMessage = "Doctor is required")]
        public string DoctorId { get; set; }

        [Required(ErrorMessage = "Appointment date is required")]
        [DataType(DataType.DateTime)]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Symptoms are required")]
        [StringLength(500, ErrorMessage = "Symptoms cannot exceed 500 characters")]
        public string Symptoms { get; set; }

        public string Notes { get; set; }
        public string Priority { get; set; } = "Normal";
    }

    public class PatientPortalMedicalRecordDto
    {
        public string Id { get; set; }
        public DateTime? RecordDate { get; set; }
        public string FormattedRecordDate => RecordDate?.ToString("MMM dd, yyyy") ?? "N/A";
        public string DoctorName { get; set; }
        public string Department { get; set; }
        public string Diagnosis { get; set; }
        public string Treatment { get; set; }
        public string Prescription { get; set; }
        public string Notes { get; set; }
        public List<PatientPortalTestResultDto> TestResults { get; set; } = new();
        public List<string> Attachments { get; set; } = new();
    }

    public class PatientPortalTestResultDto
    {
        public string Id { get; set; }
        public string TestName { get; set; }
        public string TestType { get; set; }
        public DateTime? TestDate { get; set; }
        public string FormattedTestDate => TestDate?.ToString("MMM dd, yyyy") ?? "N/A";
        public string Result { get; set; }
        public string Units { get; set; }
        public string ReferenceRange { get; set; }
        public string Status { get; set; }
        public string StatusBadgeClass => Status switch
        {
            "Normal" => "badge-success",
            "Abnormal" => "badge-danger",
            "Critical" => "badge-danger",
            "Pending" => "badge-warning",
            _ => "badge-secondary"
        };
        public string Notes { get; set; }
        public string ReportPath { get; set; }
    }

    public class PatientPortalBillDto
    {
        public string Id { get; set; }
        public string BillNumber { get; set; }
        public DateTime? BillDate { get; set; }
        public string FormattedBillDate => BillDate?.ToString("MMM dd, yyyy") ?? "N/A";
        public decimal? TotalAmount { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal? PendingAmount => (TotalAmount ?? 0) - (PaidAmount ?? 0);
        public string Status { get; set; }
        public string StatusBadgeClass => Status switch
        {
            "Paid" => "badge-success",
            "Pending" => "badge-warning",
            "Overdue" => "badge-danger",
            "Cancelled" => "badge-secondary",
            _ => "badge-secondary"
        };
        public DateTime? DueDate { get; set; }
        public string FormattedDueDate => DueDate?.ToString("MMM dd, yyyy") ?? "N/A";
        public List<PatientPortalBillItemDto> Items { get; set; } = new();
        public List<PatientPortalPaymentDto> Payments { get; set; } = new();
        public bool CanPay => Status == "Pending" && (PendingAmount ?? 0) > 0;
    }

    public class PatientPortalBillItemDto
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }
        public DateTime? ItemDate { get; set; }
        public string FormattedItemDate => ItemDate?.ToString("MMM dd, yyyy") ?? "N/A";
    }

    public class PatientPortalPaymentDto
    {
        public string Id { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string FormattedPaymentDate => PaymentDate?.ToString("MMM dd, yyyy") ?? "N/A";
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; }
        public string StatusBadgeClass => Status switch
        {
            "Completed" => "badge-success",
            "Failed" => "badge-danger",
            "Pending" => "badge-warning",
            _ => "badge-secondary"
        };
    }

    public class PatientPortalDashboardDto
    {
        public PatientPortalDto Patient { get; set; }
        public int UpcomingAppointments { get; set; }
        public int PendingBills { get; set; }
        public decimal? TotalOutstandingAmount { get; set; }
        public int RecentTestResults { get; set; }
        public List<PatientPortalAppointmentDto> RecentAppointments { get; set; } = new();
        public List<PatientPortalBillDto> RecentBills { get; set; } = new();
        public List<PatientPortalTestResultDto> RecentTestResultsList { get; set; } = new();
    }

    public class PatientPortalDoctorDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string Department { get; set; }
        public string Designation { get; set; }
        public string Specialization { get; set; }
        public string ProfileImagePath { get; set; }
        public string About { get; set; }
        public List<PatientPortalDoctorAvailabilityDto> Availability { get; set; } = new();
    }

    public class PatientPortalDoctorAvailabilityDto
    {
        public string DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string FormattedTime => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
        public bool IsAvailable { get; set; }
    }
}
