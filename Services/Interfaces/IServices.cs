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
}