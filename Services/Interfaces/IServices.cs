using MedyxHMS.Models;

namespace MedyxHMS.Services.Interfaces
{
    public interface ISettingService
    {
        Task<HospitalSettings> GetHospitalSettingsAsync();
        Task<FeatureToggles> GetFeatureTogglesAsync();
        Task<string> GetSettingValueAsync(string key);
        Task<bool> UpdateSettingAsync(string key, string value);
        Task<IEnumerable<Language>> GetSupportedLanguagesAsync();
    }

    public interface IPatientService
    {
        Task<IEnumerable<Patient>> GetAllPatientsAsync();
        Task<Patient> GetPatientByIdAsync(int id);
        Task<Patient> CreatePatientAsync(Patient patient);
        Task<Patient> UpdatePatientAsync(Patient patient);
        Task<bool> DeletePatientAsync(int id);
        Task<IEnumerable<Patient>> SearchPatientsAsync(string searchTerm);
    }

    public interface IAppointmentService
    {
        Task<IEnumerable<Appointment>> GetAllAppointmentsAsync();
        Task<Appointment> GetAppointmentByIdAsync(int id);
        Task<Appointment> CreateAppointmentAsync(Appointment appointment);
        Task<Appointment> UpdateAppointmentAsync(Appointment appointment);
        Task<bool> DeleteAppointmentAsync(int id);
        Task<IEnumerable<Appointment>> GetAppointmentsByPatientAsync(int patientId);
        Task<IEnumerable<Appointment>> GetAppointmentsByDoctorAsync(int doctorId);
        Task<IEnumerable<Appointment>> GetAppointmentsByDateAsync(DateTime date);
    }

    public interface IBillingService
    {
        Task<IEnumerable<Bill>> GetAllBillsAsync();
        Task<Bill> GetBillByIdAsync(int id);
        Task<Bill> CreateBillAsync(Bill bill);
        Task<Bill> UpdateBillAsync(Bill bill);
        Task<bool> DeleteBillAsync(int id);
        Task<IEnumerable<Bill>> GetBillsByPatientAsync(int patientId);
        Task<bool> ProcessPaymentAsync(Payment payment);
        Task<decimal> GetTotalRevenueAsync(DateTime startDate, DateTime endDate);
    }

    public interface IAuditService
    {
        Task LogActivityAsync(string userId, string action, string entityName, string entityId, string oldValues = null, string newValues = null);
        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, string userId = null);
        Task<IEnumerable<AuditLog>> GetEntityAuditLogsAsync(string entityName, string entityId);
    }

    public interface IFileService
    {
        Task<string> UploadFileAsync(IFormFile file, string subDirectory = "");
        Task<bool> DeleteFileAsync(string filePath);
        Task<byte[]> GetFileAsync(string filePath);
        Task<bool> FileExistsAsync(string filePath);
        string GetFileUrl(string filePath);
    }

    public interface IAuthorizationService
    {
        Task<bool> HasPermissionAsync(string userId, string permission);
        Task<bool> HasAnyPermissionAsync(string userId, IEnumerable<string> permissions);
        Task<IEnumerable<string>> GetUserPermissionsAsync(string userId);
        Task<IEnumerable<string>> GetUserRolesAsync(string userId);
        Task<bool> AssignRoleToUserAsync(string userId, int roleId);
        Task<bool> RemoveRoleFromUserAsync(string userId, int roleId);
    }

    public interface IStaffService
    {
        Task<IEnumerable<Staff>> GetAllStaffAsync();
        Task<Staff> GetStaffByIdAsync(string id);
        Task<Staff> GetStaffByEmployeeIdAsync(string employeeId);
        Task<Staff> CreateStaffAsync(Staff staff, string password, List<int> roleIds);
        Task<Staff> UpdateStaffAsync(Staff staff, List<int> roleIds);
        Task<bool> DeleteStaffAsync(string id);
        Task<bool> ActivateStaffAsync(string id);
        Task<bool> DeactivateStaffAsync(string id);
        Task<IEnumerable<Staff>> SearchStaffAsync(string searchTerm);
        Task<IEnumerable<Staff>> GetStaffByDepartmentAsync(string department);
        Task<IEnumerable<Staff>> GetStaffByRoleAsync(string roleName);
        Task<bool> ChangeStaffPasswordAsync(string staffId, string currentPassword, string newPassword);
        Task<bool> ResetStaffPasswordAsync(string staffId, string newPassword);
        Task<bool> UpdateStaffProfileAsync(string staffId, string firstName, string lastName, string phone, string address, IFormFile profileImage);
        Task<IEnumerable<string>> GetStaffPermissionsAsync(string staffId);
        Task<IEnumerable<string>> GetStaffRolesAsync(string staffId);
        Task<bool> AssignRolesToStaffAsync(string staffId, List<int> roleIds);
        Task<bool> RemoveRolesFromStaffAsync(string staffId, List<int> roleIds);
        Task<Dictionary<string, int>> GetStaffStatisticsAsync();
        Task<IEnumerable<Staff>> GetRecentStaffAsync(int count = 10);
    }

    public interface IPatientPortalService
    {
        // Patient Account Management
        Task<Patient> RegisterPatientAsync(Patient patient, string password);
        Task<Patient> GetPatientByIdAsync(string patientId);
        Task<Patient> UpdatePatientProfileAsync(Patient patient);
        Task<bool> ChangePatientPasswordAsync(string patientId, string currentPassword, string newPassword);
        Task<bool> ResetPatientPasswordAsync(string email, string newPassword);
        
        // Patient Dashboard
        Task<Dictionary<string, object>> GetPatientDashboardDataAsync(string patientId);
        Task<int> GetUpcomingAppointmentsCountAsync(string patientId);
        Task<int> GetPendingBillsCountAsync(string patientId);
        Task<decimal> GetTotalOutstandingAmountAsync(string patientId);
        
        // Patient Appointments
        Task<IEnumerable<Appointment>> GetPatientAppointmentsAsync(string patientId, string filter = "all");
        Task<Appointment> GetAppointmentDetailsAsync(string appointmentId);
        Task<Appointment> BookAppointmentAsync(Appointment appointment);
        Task<bool> RescheduleAppointmentAsync(string appointmentId, DateTime newDate, TimeSpan newTime);
        Task<bool> CancelAppointmentAsync(string appointmentId, string cancelReason);
        Task<IEnumerable<Staff>> GetAvailableDoctorsAsync(DateTime date);
        Task<List<TimeSpan>> GetAvailableTimeSlotAsync(string doctorId, DateTime date);
        
        // Patient Medical Records
        Task<IEnumerable<MedicalRecord>> GetPatientMedicalRecordsAsync(string patientId, DateTime? startDate = null, DateTime? endDate = null);
        Task<MedicalRecord> GetMedicalRecordDetailsAsync(string recordId);
        Task<IEnumerable<TestResult>> GetPatientTestResultsAsync(string patientId, DateTime? startDate = null, DateTime? endDate = null);
        Task<TestResult> GetTestResultDetailsAsync(string testResultId);
        Task<byte[]> DownloadTestReportAsync(string testResultId);
        
        // Patient Bills and Payments
        Task<IEnumerable<Bill>> GetPatientBillsAsync(string patientId, string filter = "all");
        Task<Bill> GetBillDetailsAsync(string billId);
        Task<IEnumerable<Payment>> GetPaymentHistoryAsync(string patientId);
        Task<decimal> GetTotalOutstandingAsync(string patientId);
        Task<int> GetOverdueBillsCountAsync(string patientId);
        
        // Doctor Information
        Task<IEnumerable<Staff>> GetAvailableDoctorsForBookingAsync(string departmentFilter = null);
        Task<Staff> GetDoctorDetailsAsync(string doctorId);
        Task<List<DoctorAvailability>> GetDoctorAvailabilityAsync(string doctorId);
        
        // Patient Notifications
        Task<IEnumerable<Notification>> GetPatientNotificationsAsync(string patientId);
        Task<bool> MarkNotificationAsReadAsync(string notificationId);
        Task<bool> DeleteNotificationAsync(string notificationId);
    }

    public class DoctorAvailability
    {
        public string DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class Notification
    {
        public string Id { get; set; }
        public string PatientId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // Appointment, Bill, TestResult, General
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
        public string RelatedEntityId { get; set; }
    }
}