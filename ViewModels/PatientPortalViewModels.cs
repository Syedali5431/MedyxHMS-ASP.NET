using MedyxHMS.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.ViewModels
{
    // Patient Portal ViewModels for MVC views

    public class PatientPortalLoginViewModel
    {
        public PatientPortalLoginDto Login { get; set; } = new();
        public string ReturnUrl { get; set; }
        public bool RememberMe { get; set; }
    }

    public class PatientPortalRegisterViewModel
    {
        public PatientPortalRegisterDto Register { get; set; } = new();
        public bool AcceptTerms { get; set; }
    }

    public class PatientPortalDashboardViewModel
    {
        public PatientPortalDashboardDto Dashboard { get; set; } = new();
        public string WelcomeMessage { get; set; }
        public DateTime CurrentDate { get; set; } = DateTime.Now;
    }

    public class PatientPortalProfileViewModel
    {
        public PatientPortalDto Patient { get; set; } = new();
        public PatientPortalUpdateDto UpdateProfile { get; set; } = new();
        public bool IsEditMode { get; set; }
    }

    public class PatientPortalPasswordChangeViewModel
    {
        public string PatientId { get; set; }
        public PatientPortalPasswordChangeDto PasswordChange { get; set; } = new();
        public string PatientName { get; set; }
        public string PatientEmail { get; set; }
    }

    public class PatientPortalAppointmentsViewModel
    {
        public List<PatientPortalAppointmentDto> Appointments { get; set; } = new();
        public string Filter { get; set; } = "all"; // all, upcoming, past, cancelled
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalRecords / (double)PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public List<string> StatusOptions => new() { "all", "pending", "confirmed", "completed", "cancelled" };
    }

    public class PatientPortalBookAppointmentViewModel
    {
        public PatientPortalAppointmentCreateDto Appointment { get; set; } = new();
        public List<PatientPortalDoctorDto> AvailableDoctors { get; set; } = new();
        public List<PatientPortalDoctorAvailabilityDto> SelectedDoctorAvailability { get; set; } = new();
        public string SelectedDoctorId { get; set; }
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public List<TimeSlotViewModel> AvailableTimeSlots { get; set; } = new();
    }

    public class TimeSlotViewModel
    {
        public TimeSpan Time { get; set; }
        public string FormattedTime => Time.ToString(@"hh\:mm tt");
        public bool IsAvailable { get; set; }
        public bool IsBooked { get; set; }
        public string Status => IsBooked ? "Booked" : IsAvailable ? "Available" : "Unavailable";
        public string CssClass => IsBooked ? "btn-danger" : IsAvailable ? "btn-success" : "btn-secondary";
    }

    public class PatientPortalAppointmentDetailsViewModel
    {
        public PatientPortalAppointmentDto Appointment { get; set; } = new();
        public PatientPortalDoctorDto Doctor { get; set; } = new();
        public List<PatientPortalMedicalRecordDto> RelatedRecords { get; set; } = new();
        public bool CanReschedule => Appointment.CanReschedule;
        public bool CanCancel => Appointment.CanCancel;
    }

    public class PatientPortalMedicalRecordsViewModel
    {
        public List<PatientPortalMedicalRecordDto> MedicalRecords { get; set; } = new();
        public string Filter { get; set; } = "all"; // all, recent, by-doctor
        public string DoctorFilter { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalRecords / (double)PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public List<string> DoctorOptions { get; set; } = new();
    }

    public class PatientPortalMedicalRecordDetailsViewModel
    {
        public PatientPortalMedicalRecordDto MedicalRecord { get; set; } = new();
        public PatientPortalDoctorDto Doctor { get; set; } = new();
        public List<PatientPortalTestResultDto> TestResults { get; set; } = new();
        public bool CanDownloadAttachments => MedicalRecord.Attachments.Any();
    }

    public class PatientPortalTestResultsViewModel
    {
        public List<PatientPortalTestResultDto> TestResults { get; set; } = new();
        public string Filter { get; set; } = "all"; // all, pending, completed, abnormal
        public string TestTypeFilter { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalRecords / (double)PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public List<string> TestTypeOptions { get; set; } = new();
        public int PendingResults => TestResults.Count(r => r.Status == "Pending");
        public int AbnormalResults => TestResults.Count(r => r.Status == "Abnormal" || r.Status == "Critical");
    }

    public class PatientPortalBillsViewModel
    {
        public List<PatientPortalBillDto> Bills { get; set; } = new();
        public string Filter { get; set; } = "all"; // all, pending, paid, overdue
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalRecords / (double)PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public decimal TotalOutstandingAmount => Bills.Where(b => b.Status == "Pending").Sum(b => b.PendingAmount ?? 0);
        public int OverdueBills => Bills.Count(b => b.Status == "Overdue");
    }

    public class PatientPortalBillDetailsViewModel
    {
        public PatientPortalBillDto Bill { get; set; } = new();
        public bool CanMakePayment => Bill.CanPay;
        public decimal MinimumPayment { get; set; } = 0;
        public List<string> PaymentMethods { get; set; } = new() { "Credit Card", "Debit Card", "Net Banking", "UPI", "Wallet" };
    }

    public class PatientPortalPaymentViewModel
    {
        public string BillId { get; set; }
        public decimal BillAmount { get; set; }
        public decimal PendingAmount { get; set; }

        [Required(ErrorMessage = "Payment amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than 0")]
        public decimal PaymentAmount { get; set; }

        [Required(ErrorMessage = "Payment method is required")]
        public string PaymentMethod { get; set; }

        public string CardNumber { get; set; }
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
        public string CVV { get; set; }
        public string CardHolderName { get; set; }
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }
        public string UPIId { get; set; }
        public string WalletType { get; set; }

        public List<string> AvailablePaymentMethods => new()
        {
            "Credit Card", "Debit Card", "Net Banking", "UPI", "PayPal", "PhonePe", "Google Pay", "Amazon Pay"
        };

        public List<string> Months => Enumerable.Range(1, 12).Select(m => m.ToString("D2")).ToList();
        public List<string> Years => Enumerable.Range(DateTime.Now.Year, 10).Select(y => y.ToString()).ToList();
    }

    public class PatientPortalDoctorsViewModel
    {
        public List<PatientPortalDoctorDto> Doctors { get; set; } = new();
        public string DepartmentFilter { get; set; }
        public string SearchTerm { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalRecords / (double)PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public List<string> DepartmentOptions { get; set; } = new();
    }

    public class PatientPortalDoctorDetailsViewModel
    {
        public PatientPortalDoctorDto Doctor { get; set; } = new();
        public List<PatientPortalAppointmentDto> RecentAppointments { get; set; } = new();
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public bool CanBookAppointment { get; set; }
    }

    public class PatientPortalForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }
    }

    public class PatientPortalResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

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

    public class PatientPortalEmailConfirmationViewModel
    {
        public string Email { get; set; }
        public bool IsConfirmed { get; set; }
        public string Message { get; set; }
    }

    public class PatientPortalSettingsViewModel
    {
        public PatientPortalDto Patient { get; set; } = new();
        public NotificationPreferences NotificationPreferences { get; set; } = new();
        public bool EmailNotifications { get; set; } = true;
        public bool SMSNotifications { get; set; } = true;
        public bool AppointmentReminders { get; set; } = true;
        public bool TestResultNotifications { get; set; } = true;
        public bool BillNotifications { get; set; } = true;
        public string LanguagePreference { get; set; } = "en";
        public string TimeZone { get; set; } = "UTC";
        public string PreferredLanguage
        {
            get => LanguagePreference;
            set => LanguagePreference = value;
        }
        public string PreferredTimezone
        {
            get => TimeZone;
            set => TimeZone = value;
        }
        public List<string> AvailableLanguages => new() { "en", "es", "fr", "de", "hi", "ar" };
        public List<string> AvailableTimeZones => new()
        {
            "UTC", "EST", "CST", "MST", "PST", "GMT", "CET", "IST", "JST", "AEST"
        };
    }
}