using MedyxHMS.Models;
using System.Security.Claims;

namespace MedyxHMS.Services.Interfaces
{
    public interface ISettingService
    {
        Task<HospitalSettings> GetHospitalSettingsAsync();
        Task<FeatureToggles> GetFeatureTogglesAsync();
        Task<string?> GetSettingValueAsync(string key);
        Task<bool> UpdateSettingAsync(string key, string value);
        Task<IEnumerable<Language>> GetSupportedLanguagesAsync();
    }

    public interface IPatientService
    {
        Task<IEnumerable<Patient>> GetAllPatientsAsync();
        Task<Patient?> GetPatientByIdAsync(int id);
        Task<Patient> CreatePatientAsync(Patient patient);
        Task<Patient?> UpdatePatientAsync(Patient patient);
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
        Task LogActivityAsync(string? userId, string action, string entityName, string entityId, string? oldValues = null, string? newValues = null);
        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, string? userId = null);
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
        Task<Patient?> GetPatientByIdAsync(string patientId);
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
        Task<Appointment?> GetAppointmentDetailsAsync(string appointmentId);
        Task<Appointment> BookAppointmentAsync(Appointment appointment);
        Task<bool> RescheduleAppointmentAsync(string appointmentId, DateTime newDate, TimeSpan newTime);
        Task<bool> CancelAppointmentAsync(string appointmentId, string cancelReason);
        Task<IEnumerable<Staff>> GetAvailableDoctorsAsync(DateTime date);
        Task<List<TimeSpan>> GetAvailableTimeSlotAsync(string doctorId, DateTime date);
        
        // Patient Medical Records
        Task<IEnumerable<MedicalRecord>> GetPatientMedicalRecordsAsync(string patientId, DateTime? startDate = null, DateTime? endDate = null);
        Task<MedicalRecord?> GetMedicalRecordDetailsAsync(string recordId);
        Task<IEnumerable<TestResult>> GetPatientTestResultsAsync(string patientId, DateTime? startDate = null, DateTime? endDate = null);
        Task<TestResult?> GetTestResultDetailsAsync(string testResultId);
        Task<byte[]> DownloadTestReportAsync(string testResultId);
        
        // Patient Bills and Payments
        Task<IEnumerable<Bill>> GetPatientBillsAsync(string patientId, string filter = "all");
        Task<Bill?> GetBillDetailsAsync(string billId);
        Task<IEnumerable<Payment>> GetPaymentHistoryAsync(string patientId);
        Task<decimal> GetTotalOutstandingAsync(string patientId);
        Task<int> GetOverdueBillsCountAsync(string patientId);
        
        // Doctor Information
        Task<IEnumerable<Staff>> GetAvailableDoctorsForBookingAsync(string? departmentFilter = null);
        Task<Staff?> GetDoctorDetailsAsync(string doctorId);
        Task<List<DoctorAvailability>> GetDoctorAvailabilityAsync(string doctorId);
        
        // Patient Notifications
        Task<IEnumerable<Notification>> GetPatientNotificationsAsync(string patientId);
        Task<bool> MarkNotificationAsReadAsync(string notificationId);
        Task<bool> DeleteNotificationAsync(string notificationId);
    }

    public interface IPublicBookingNotificationService
    {
        Task NotifyAppointmentConfirmedAsync(PublicAppointmentRequest request, string doctorDisplayName);
    }

    public interface IEmailNotificationProvider
    {
        Task SendAsync(string toEmail, string subject, string body);
    }

    public interface ISmsNotificationProvider
    {
        Task SendAsync(string toPhone, string message);
    }

    public interface INotificationDeliveryAuditService
    {
        Task LogAsync(
            string channel,
            string provider,
            string recipient,
            string subject,
            string messageBody,
            string status,
            string? providerResponse = null,
            string? relatedEntityType = null,
            string? relatedEntityId = null,
            bool isTest = false);
    }

    public interface IExportService
    {
        byte[] BuildCsv(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows);
        byte[] BuildPdfTable(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows);
    }

    public interface IChatbotModerationService
    {
        ChatModerationResult Evaluate(string input);
        ChatModerationResult EvaluateOutput(string output, int sourceCount, decimal confidenceScore);
    }

    public interface IChatbotPromptBuilder
    {
        string BuildSystemPrompt(ClaimsPrincipal user, ChatKnowledgeContext context);
    }

    public interface IChatbotKnowledgeService
    {
        Task<ChatKnowledgeContext> RetrieveContextAsync(ClaimsPrincipal user, string message, string? languageCode = null);
    }

    public interface IChatbotService
    {
        Task<ChatbotAskResponse> AskAsync(ClaimsPrincipal user, string message, string? sessionId = null, string? languageCode = null);
        Task<IReadOnlyList<ChatMessage>> GetSessionMessagesAsync(string sessionId, ClaimsPrincipal user, int take = 30);
        Task<bool> SubmitFeedbackAsync(ClaimsPrincipal user, string sessionId, long? messageId, string feedbackType, string? comment = null);
        Task<bool> IsChatbotEnabledForUserAsync(ClaimsPrincipal user);
        Task<ChatEscalation?> EscalateAsync(ClaimsPrincipal user, string sessionId, long? messageId, string reason, string escalationType = "Support");
        Task<bool> MarkSessionUnresolvedAsync(ClaimsPrincipal user, string sessionId, string reason);
        Task<IReadOnlyList<ChatEscalation>> GetEscalationsAsync(string status = "Pending", int take = 100);
        Task<bool> ResolveEscalationAsync(long escalationId, string targetContact, string resolverUserId);
        Task<ChatbotAnalyticsSnapshot> GetAnalyticsAsync(int days = 30);
        Task<ChatbotAdminSettings> GetAdminSettingsAsync();
        Task<bool> UpdateAdminSettingsAsync(ChatbotAdminSettings settings, string modifiedByUserId);
    }

    public interface IChatbotConsentService
    {
        /// <summary>
        /// Get the current consent status for a user.
        /// Returns null if user has never provided consent.
        /// </summary>
        Task<ChatbotConsent?> GetCurrentConsentAsync(string? userId);

        /// <summary>
        /// Check if user has active consent for chatbot use.
        /// </summary>
        Task<bool> HasActiveConsentAsync(string? userId);

        /// <summary>
        /// Record user acceptance of consent terms.
        /// </summary>
        Task<ChatbotConsent> AcceptConsentAsync(string? userId, bool aiProcessing, bool dataRetention, 
            bool thirdPartyProcessing, string consentVersion, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Record user rejection of consent terms.
        /// </summary>
        Task<ChatbotConsentAudit> RejectConsentAsync(string? userId, string consentVersion, 
            string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Revoke existing consent for a user.
        /// </summary>
        Task<bool> RevokeConsentAsync(string? userId, string? reason = null);

        /// <summary>
        /// Renew consent with the latest version.
        /// </summary>
        Task<ChatbotConsent> RenewConsentAsync(string? userId, bool aiProcessing, bool dataRetention, 
            bool thirdPartyProcessing, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Get consent audit trail for a user.
        /// </summary>
        Task<IReadOnlyList<ChatbotConsentAudit>> GetConsentAuditAsync(string? userId, int take = 50);

        /// <summary>
        /// Get consent version text/terms.
        /// </summary>
        Task<string> GetConsentTermsAsync(string version);

        /// <summary>
        /// Check if user must provide/renew consent (based on version/policy changes).
        /// </summary>
        Task<bool> RequiresConsentRenewalAsync(string? userId);
    }

    public interface IChatbotPiiRedactionService
    {
        string RedactEventDetails(string details, string eventType, string redactionLevel);
    }

    public interface IChatbotDataCleanupService
    {
        Task<ChatbotCleanupResult> CleanupExpiredSessionsAsync(CancellationToken cancellationToken);
        Task<ChatbotCleanupResult> CleanupExpiredEventLogsAsync(CancellationToken cancellationToken);
        Task<ChatbotCleanupResult> CleanupUnconsentedDataAsync(CancellationToken cancellationToken);
    }

    public interface ISmtpHealthService
    {
        Task<SmtpHealthStatus> CheckAsync();
    }

    public interface ILicenseService
    {
        Task<LicenseSnapshot> GetCurrentSnapshotAsync();
        Task<IReadOnlyList<LicenseAuditLog>> GetAuditHistoryAsync(int take = 20);
        Task<IReadOnlyList<LicenseReminderLog>> GetReminderHistoryAsync(int take = 20);
        Task<LicenseRecord> RenewAsync(int termYears, string performedByUserId, string? notes = null, string? ipAddress = null);
        Task<ReminderDispatchResult> SendReminderAsync(bool force, string? performedByUserId = null, string? ipAddress = null);
        Task<bool> ShouldRestrictAccessAsync(ClaimsPrincipal user, string requestPath);
        Task<bool> IsModuleLicensedForCurrentLicenseAsync(string moduleKey);
    }

    public interface ILicenseFileService
    {
        Task<LicenseRecord> ValidateAndActivateAsync(IFormFile licenseFile, string performedByUserId, string? ipAddress = null);
        Task<bool> IsCurrentLicenseCryptographicallyValidAsync();
    }

    public interface IConcurrentSessionService
    {
        Task<ConcurrentLoginDecision> TryRegisterLoginAsync(string userId, string activeRole, string sessionId, string? ipAddress, string? userAgent);
        Task EndSessionAsync(string sessionId);
        Task MarkActivityAsync(string sessionId);
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

    public interface IOPDService
    {
        Task<IEnumerable<OPDVisit>> GetAllOPDVisitsAsync();
        Task<OPDVisit> GetOPDVisitByIdAsync(int id);
        Task<OPDVisit> CreateOPDVisitAsync(OPDVisit visit);
        Task<OPDVisit> UpdateOPDVisitAsync(OPDVisit visit);
        Task<bool> DeleteOPDVisitAsync(int id);
        Task<IEnumerable<OPDVisit>> GetOPDVisitsByPatientAsync(int patientId);
        Task<IEnumerable<OPDVisit>> GetOPDVisitsByDoctorAsync(int doctorId);
        Task<IEnumerable<OPDVisit>> GetOPDVisitsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<OPDVisit>> GetTodayOPDVisitsAsync();
        Task<int> GetOPDVisitCountAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetOPDRevenueAsync(DateTime startDate, DateTime endDate);
    }

    public interface IIPDService
    {
        Task<IEnumerable<IPDAdmission>> GetAllIPDAdmissionsAsync();
        Task<IPDAdmission> GetIPDAdmissionByIdAsync(int id);
        Task<IPDAdmission> CreateIPDAdmissionAsync(IPDAdmission admission);
        Task<IPDAdmission> UpdateIPDAdmissionAsync(IPDAdmission admission);
        Task<bool> DeleteIPDAdmissionAsync(int id);
        Task<IEnumerable<IPDAdmission>> GetIPDAdmissionsByPatientAsync(int patientId);
        Task<IEnumerable<IPDAdmission>> GetIPDAdmissionsByDoctorAsync(int doctorId);
        Task<IEnumerable<IPDAdmission>> GetIPDAdmissionsByStatusAsync(string status);
        Task<IEnumerable<IPDAdmission>> GetCurrentIPDAdmissionsAsync();
        Task<bool> DischargePatientAsync(int admissionId, DateTime dischargeDate);
        Task<IEnumerable<IPDAdmission>> GetIPDAdmissionsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<int> GetCurrentAdmissionCountAsync();
        Task<decimal> GetIPDRevenueAsync(DateTime startDate, DateTime endDate);
    }

    public interface IWardService
    {
        Task<IEnumerable<Ward>> GetAllWardsAsync();
        Task<Ward> GetWardByIdAsync(int id);
        Task<Ward> CreateWardAsync(Ward ward);
        Task<Ward> UpdateWardAsync(Ward ward);
        Task<bool> DeleteWardAsync(int id);
        Task<IEnumerable<Ward>> GetActiveWardsAsync();
        Task<int> GetAvailableBedCountAsync(int wardId);
        Task<int> GetOccupiedBedCountAsync(int wardId);
        Task<double> GetWardOccupancyRateAsync(int wardId);
    }

    public interface IBedService
    {
        Task<IEnumerable<Bed>> GetAllBedsAsync();
        Task<Bed> GetBedByIdAsync(int id);
        Task<Bed> CreateBedAsync(Bed bed);
        Task<Bed> UpdateBedAsync(Bed bed);
        Task<bool> DeleteBedAsync(int id);
        Task<IEnumerable<Bed>> GetBedsByWardAsync(int wardId);
        Task<IEnumerable<Bed>> GetAvailableBedsAsync();
        Task<IEnumerable<Bed>> GetBedsByStatusAsync(string status);
        Task<bool> UpdateBedStatusAsync(int bedId, string status);
        Task<Bed> GetBedByBedNumberAsync(string bedNumber, int wardId);
    }

    public interface IPrescriptionService
    {
        // Prescription methods
        Task<IEnumerable<Prescription>> GetAllPrescriptionsAsync();
        Task<Prescription> GetPrescriptionByIdAsync(int id);
        Task<Prescription> CreatePrescriptionAsync(Prescription prescription);
        Task<Prescription> UpdatePrescriptionAsync(Prescription prescription);
        Task<bool> DeletePrescriptionAsync(int id);
        Task<IEnumerable<Prescription>> GetPrescriptionsByPharmacyBillAsync(int pharmacyBillId);
        Task<IEnumerable<Prescription>> GetPrescriptionsByPatientAsync(int patientId);
        Task<IEnumerable<Prescription>> GetPrescriptionsByMedicineAsync(int medicineId);
        Task<IEnumerable<Prescription>> GetPrescriptionsByDateRangeAsync(DateTime startDate, DateTime endDate);

        // Medicine methods
        Task<IEnumerable<Medicine>> GetAllMedicinesAsync();
        Task<Medicine?> GetMedicineByIdAsync(int id);
        Task<Medicine> CreateMedicineAsync(Medicine medicine);
        Task<Medicine> UpdateMedicineAsync(Medicine medicine);
        Task<bool> DeleteMedicineAsync(int id);
        Task<IEnumerable<Medicine>> GetLowStockMedicinesAsync();
        Task<IEnumerable<Medicine>> GetExpiringMedicinesAsync(int daysAhead = 30);
        Task<IEnumerable<Medicine>> SearchMedicinesAsync(string searchTerm);
        Task<bool> UpdateMedicineStockAsync(int medicineId, int quantityUsed);
        Task<int> GetTotalMedicineStockAsync();

        // Pharmacy Bill methods
        Task<IEnumerable<PharmacyBill>> GetAllPharmacyBillsAsync();
        Task<PharmacyBill?> GetPharmacyBillByIdAsync(int id);
        Task<PharmacyBill> CreatePharmacyBillAsync(PharmacyBill bill);
        Task<PharmacyBill> UpdatePharmacyBillAsync(PharmacyBill bill);
        Task<bool> DeletePharmacyBillAsync(int id);
        Task<IEnumerable<PharmacyBill>> GetPharmacyBillsByPatientAsync(int patientId);
        Task<IEnumerable<PharmacyBill>> GetPharmacyBillsByStatusAsync(string status);
        Task<IEnumerable<PharmacyBill>> GetPharmacyBillsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetPharmacyRevenueAsync(DateTime startDate, DateTime endDate);
        Task<int> GetTotalPrescriptionsCountAsync(DateTime startDate, DateTime endDate);
    }

    public interface ILabService
    {
        // Lab Test Catalog methods
        Task<IEnumerable<LabTest>> GetAllLabTestsAsync();
        Task<LabTest?> GetLabTestByIdAsync(int id);
        Task<LabTest> CreateLabTestAsync(LabTest labTest);
        Task<LabTest?> UpdateLabTestAsync(LabTest labTest);
        Task<bool> DeleteLabTestAsync(int id);
        Task<IEnumerable<LabTest>> GetActiveLabTestsAsync();
        Task<IEnumerable<LabTest>> SearchLabTestsByCategoryAsync(string category);
        Task<IEnumerable<LabTest>> SearchLabTestsByNameAsync(string testName);

        // Lab Result methods
        Task<IEnumerable<LabResult>> GetAllLabResultsAsync();
        Task<LabResult?> GetLabResultByIdAsync(int id);
        Task<LabResult> CreateLabResultAsync(LabResult labResult);
        Task<LabResult?> UpdateLabResultAsync(LabResult labResult);
        Task<bool> DeleteLabResultAsync(int id);
        Task<IEnumerable<LabResult>> GetLabResultsByPatientAsync(int patientId);
        Task<IEnumerable<LabResult>> GetLabResultsByStatusAsync(string status);
        Task<IEnumerable<LabResult>> GetPendingLabResultsAsync();
        Task<IEnumerable<LabResult>> GetLabResultsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<LabResult>> GetPatientLabResultsByTestAsync(int patientId, int testId);
        Task<bool> UpdateLabResultStatusAsync(int labResultId, string status);
        Task<int> GetPendingLabTestsCountAsync();
        Task<decimal> GetLabRevenueAsync(DateTime startDate, DateTime endDate);
    }

    public interface IRadiologyService
    {
        // Radiology Test Catalog methods
        Task<IEnumerable<RadiologyTest>> GetAllRadiologyTestsAsync();
        Task<RadiologyTest> GetRadiologyTestByIdAsync(int id);
        Task<RadiologyTest> CreateRadiologyTestAsync(RadiologyTest radiologyTest);
        Task<RadiologyTest> UpdateRadiologyTestAsync(RadiologyTest radiologyTest);
        Task<bool> DeleteRadiologyTestAsync(int id);
        Task<IEnumerable<RadiologyTest>> GetActiveRadiologyTestsAsync();
        Task<IEnumerable<RadiologyTest>> SearchRadiologyTestsByCategoryAsync(string category);
        Task<IEnumerable<RadiologyTest>> SearchRadiologyTestsByNameAsync(string testName);

        // Radiology Result methods
        Task<IEnumerable<RadiologyResult>> GetAllRadiologyResultsAsync();
        Task<RadiologyResult?> GetRadiologyResultByIdAsync(int id);
        Task<RadiologyResult> CreateRadiologyResultAsync(RadiologyResult radiologyResult);
        Task<RadiologyResult?> UpdateRadiologyResultAsync(RadiologyResult radiologyResult);
        Task<bool> DeleteRadiologyResultAsync(int id);
        Task<IEnumerable<RadiologyResult>> GetRadiologyResultsByPatientAsync(int patientId);
        Task<IEnumerable<RadiologyResult>> GetRadiologyResultsByStatusAsync(string status);
        Task<IEnumerable<RadiologyResult>> GetPendingRadiologyResultsAsync();
        Task<IEnumerable<RadiologyResult>> GetRadiologyResultsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<RadiologyResult>> GetPatientRadiologyResultsByTestAsync(int patientId, int testId);
        Task<bool> UpdateRadiologyResultStatusAsync(int radiologyResultId, string status);
        Task<int> GetPendingRadiologyTestsCountAsync();
        Task<decimal> GetRadiologyRevenueAsync(DateTime startDate, DateTime endDate);
    }

    public interface IBloodBankService
    {
        Task<IEnumerable<BloodInventory>> GetBloodInventoryAsync();
        Task<BloodInventory> UpsertInventoryAsync(string bloodGroup, int unitsAvailable, int minimumLevel);
        Task<IEnumerable<BloodIssue>> GetBloodIssuesAsync();
        Task<BloodIssue> IssueBloodAsync(BloodIssue issue);
        Task<bool> DeleteBloodIssueAsync(int id);
    }

    public interface IOperationTheatreService
    {
        Task<IEnumerable<OTSchedule>> GetSchedulesAsync();
        Task<OTSchedule> GetScheduleByIdAsync(int id);
        Task<OTSchedule> CreateScheduleAsync(OTSchedule schedule);
        Task<bool> UpdateStatusAsync(int id, string status);
    }

    public interface IReferralService
    {
        Task<IEnumerable<Referral>> GetReferralsAsync();
        Task<Referral> GetReferralByIdAsync(int id);
        Task<Referral> CreateReferralAsync(Referral referral);
        Task<bool> UpdateStatusAsync(int id, string status);
    }

    public interface IAttendanceService
    {
        Task<IEnumerable<StaffAttendance>> GetAttendanceAsync(DateTime date, string staffId = null);
        Task<StaffAttendance> GetAttendanceByIdAsync(int id);
        Task<StaffAttendance> MarkAttendanceAsync(StaffAttendance attendance);
        Task<StaffAttendance> CheckInAsync(string staffId, DateTime checkInTime, string notes = null);
        Task<StaffAttendance> CheckOutAsync(string staffId, DateTime checkOutTime, string notes = null);
        Task<Dictionary<string, int>> GetAttendanceSummaryAsync(DateTime startDate, DateTime endDate);
    }

    public interface ILeaveService
    {
        Task<IEnumerable<LeaveType>> GetLeaveTypesAsync(bool activeOnly = false);
        Task<LeaveType> CreateLeaveTypeAsync(LeaveType leaveType);
        Task<LeaveType> UpdateLeaveTypeAsync(LeaveType leaveType);

        Task<IEnumerable<LeaveRequest>> GetLeaveRequestsAsync(string? staffId = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<LeaveRequest> GetLeaveRequestByIdAsync(int id);
        Task<LeaveRequest> CreateLeaveRequestAsync(LeaveRequest leaveRequest);
        Task<bool> UpdateLeaveRequestStatusAsync(int requestId, string status, string? approverId, string? remarks = null);

        Task<IEnumerable<LeaveBalance>> GetLeaveBalancesAsync(string? staffId = null, int? year = null);
    }

    public interface IPayrollService
    {
        Task<IEnumerable<PayrollRecord>> GetPayrollRecordsAsync(DateTime? month = null, string? staffId = null);
        Task<PayrollRecord?> GetPayrollRecordByIdAsync(int id);
        Task<PayrollRecord> GeneratePayrollAsync(string staffId, DateTime payrollMonth, decimal allowances = 0, decimal deductions = 0, string? notes = null);
        Task<bool> MarkPayrollAsPaidAsync(int payrollRecordId, DateTime paymentDate, string? notes = null);
    }

    public interface IFrontOfficeService
    {
        Task<IEnumerable<VisitorLog>> GetVisitorsAsync(DateTime? date = null);
        Task<VisitorLog> AddVisitorAsync(VisitorLog visitor);
        Task<bool> CheckOutVisitorAsync(int visitorId, DateTime checkOutTime, string? notes = null);

        Task<IEnumerable<ComplaintRecord>> GetComplaintsAsync(string? status = null);
        Task<ComplaintRecord> AddComplaintAsync(ComplaintRecord complaint);
        Task<bool> UpdateComplaintStatusAsync(int complaintId, string status, string? resolutionNotes = null);

        Task<IEnumerable<DispatchReceiveRecord>> GetDispatchReceiveRecordsAsync(string? recordType = null, DateTime? date = null);
        Task<DispatchReceiveRecord> AddDispatchReceiveRecordAsync(DispatchReceiveRecord record);
    }

    public interface ICertificateService
    {
        Task<IEnumerable<CertificateRecord>> GetCertificatesAsync(string? staffId = null);
        Task<CertificateRecord> GenerateCertificateAsync(CertificateRecord certificate);

        Task<IEnumerable<IdCardRecord>> GetIdCardsAsync(string? staffId = null);
        Task<IdCardRecord> GenerateIdCardAsync(IdCardRecord idCard);
    }

    public interface IReportService
    {
        // Department Reports
        Task<List<dynamic>> GenerateDepartmentReportAsync(int? departmentId, DateTime startDate, DateTime endDate);

        // Financial Reports
        Task<Dictionary<string, decimal>> GenerateFinancialReportAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalRevenueByDepartmentAsync(int departmentId, DateTime startDate, DateTime endDate);

        // Occupancy Reports
        Task<Dictionary<string, int>> GenerateOccupancyReportAsync(DateTime date);
        Task<double> GetAverageOccupancyRateAsync(DateTime startDate, DateTime endDate);

        // Staff Reports
        Task<List<dynamic>> GenerateStaffAttendanceReportAsync(string staffId, DateTime startDate, DateTime endDate);
        Task<List<dynamic>> GeneratePayrollReportAsync(DateTime month);
        Task<Dictionary<string, int>> GetStaffDepartmentDistributionAsync();

        // General Report Management
        Task<GeneratedReport> SaveReportAsync(GeneratedReport report);
        Task<IEnumerable<GeneratedReport>> GetGeneratedReportsAsync(string? reportType = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<bool> DeleteGeneratedReportAsync(int reportId);

        // Report Scheduling
        Task<ReportSchedule> CreateReportScheduleAsync(ReportSchedule schedule);
        Task<IEnumerable<ReportSchedule>> GetReportSchedulesAsync(bool activeOnly = true);
        Task<bool> UpdateReportScheduleAsync(ReportSchedule schedule);
        Task<bool> DeleteReportScheduleAsync(int scheduleId);
    }

    /// <summary>
    /// Manages system-module visibility:
    ///   - SuperAdmin can toggle a module globally on/off.
    ///   - Admin and SuperAdmin can enable/disable a module for a specific user.
    /// </summary>
    public interface IModuleService
    {
        /// <summary>Returns all registered system modules.</summary>
        Task<IReadOnlyList<SystemModule>> GetAllModulesAsync();

        /// <summary>
        /// Returns true when the module is globally enabled AND either:
        ///   (a) no per-user access record exists (default = enabled), or
        ///   (b) the per-user access record explicitly sets IsEnabled = true.
        /// SuperAdmin users bypass both global and per-user restrictions.
        /// </summary>
        Task<bool> IsModuleEnabledForUserAsync(string moduleKey, string userId, bool isSuperAdmin = false);

        /// <summary>
        /// Returns a dictionary of moduleKey → isEnabled for a given user,
        /// respecting global and per-user overrides.
        /// </summary>
        Task<Dictionary<string, bool>> GetUserModuleMapAsync(string userId, bool isSuperAdmin = false);

        /// <summary>SuperAdmin only: toggle global on/off for a module.</summary>
        Task<bool> SetGlobalModuleEnabledAsync(int moduleId, bool isEnabled, string performedByUserId);

        /// <summary>Admin/SuperAdmin: set per-user access for a module.</summary>
        Task<bool> SetUserModuleAccessAsync(string userId, int moduleId, bool isEnabled, string performedByUserId);

        /// <summary>
        /// Returns all modules with each user's per-module access status.
        /// Used by the admin "User Module Access" screen.
        /// </summary>
        Task<IReadOnlyList<UserModuleAccessRow>> GetUserModuleAccessRowsAsync(string userId);
    }

    /// <summary>Flat projection used in the module-management UI.</summary>
    public class UserModuleAccessRow
    {
        public int ModuleId { get; set; }
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public bool IsGloballyEnabled { get; set; }
        public bool? UserOverride { get; set; }   // null = no explicit record (inherits global)
        public bool EffectivelyEnabled { get; set; }
        public int SortOrder { get; set; }
    }
}