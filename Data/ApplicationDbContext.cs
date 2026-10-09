using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for ApplicationDbContext and its related runtime behavior.
namespace MedyxHMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHospitalContext? _hospitalContext;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Used by dependency injection: the hospital context limits hospital-scoped records to the active hospital.
        // The constructor above (no hospital context, so no filter) stays for tools, tests and start-up code.
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHospitalContext hospitalContext)
            : base(options)
        {
            _hospitalContext = hospitalContext;
        }

        // Read by the global query filters each time a query runs (EF Core parameterises these per context instance).
        private bool HospitalFilterEnabled => _hospitalContext?.FilterEnabled == true;
        private int? HospitalFilterId => _hospitalContext?.ActiveHospitalId;

        // Multi-hospital (branches of one group)
        public DbSet<Hospital> Hospitals { get; set; }
        public DbSet<UserHospitalAccess> UserHospitalAccesses { get; set; }

        // Network printers (Settings → Printers)
        public DbSet<Printer> Printers { get; set; }

        // Security hardening: database backup history
        public DbSet<DatabaseBackup> DatabaseBackups { get; set; }

        // Quality management (incidents + CAPA, controlled documents, internal audits, training records)
        public DbSet<QualityIncident> QualityIncidents { get; set; }
        public DbSet<CapaAction> CapaActions { get; set; }
        public DbSet<ControlledDocument> ControlledDocuments { get; set; }
        public DbSet<DocumentVersion> DocumentVersions { get; set; }
        public DbSet<InternalAudit> InternalAudits { get; set; }
        public DbSet<AuditFinding> AuditFindings { get; set; }
        public DbSet<TrainingRecord> TrainingRecords { get; set; }

        // Equipment register (maintenance / calibration)
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<EquipmentServiceRecord> EquipmentServiceRecords { get; set; }

        // Lab traceability (chain of custody)
        public DbSet<LabSampleEvent> LabSampleEvents { get; set; }

        // Vendors master
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<PurchaseBill> PurchaseBills { get; set; }
        public DbSet<PurchaseBillItem> PurchaseBillItems { get; set; }
        public DbSet<VendorPayment> VendorPayments { get; set; }

        // Core Hospital Entities
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Department> Departments { get; set; }

        // OPD/IPD Entities
        public DbSet<OPDVisit> OPDVisits { get; set; }
        public DbSet<IPDAdmission> IPDAdmissions { get; set; }
        public DbSet<Ward> Wards { get; set; }
        public DbSet<Bed> Beds { get; set; }

        // Billing & Payment Entities
        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillItem> BillItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        // Pharmacy Entities
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<PharmacyBill> PharmacyBills { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }

        // Lab & Radiology Entities
        public DbSet<LabTest> LabTests { get; set; }
        public DbSet<LabResult> LabResults { get; set; }
        public DbSet<RadiologyTest> RadiologyTests { get; set; }
        public DbSet<RadiologyResult> RadiologyResults { get; set; }
        public DbSet<BloodInventory> BloodInventories { get; set; }
        public DbSet<BloodIssue> BloodIssues { get; set; }
        public DbSet<OTSchedule> OTSchedules { get; set; }
        public DbSet<OperationTheatre> OperationTheatres { get; set; }
        public DbSet<OTBlock> OTBlocks { get; set; }
        public DbSet<Referral> Referrals { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<PatientDocument> PatientDocuments { get; set; }
        public DbSet<DischargeSummary> DischargeSummaries { get; set; }
        public DbSet<StaffAttendance> StaffAttendances { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<LeaveBalance> LeaveBalances { get; set; }
        public DbSet<PayrollRecord> PayrollRecords { get; set; }
        public DbSet<VisitorLog> VisitorLogs { get; set; }
        public DbSet<ComplaintRecord> ComplaintRecords { get; set; }
        public DbSet<DispatchReceiveRecord> DispatchReceiveRecords { get; set; }
        public DbSet<CertificateRecord> CertificateRecords { get; set; }
        public DbSet<IdCardRecord> IdCardRecords { get; set; }
        public DbSet<PatientInsurance> PatientInsurances { get; set; }
        public DbSet<VisitNoteHistory> VisitNoteHistories { get; set; }
        public DbSet<LabNoteHistory> LabNoteHistories { get; set; }

        // Audit & Reporting
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<NotificationDeliveryLog> NotificationDeliveryLogs { get; set; }
        public DbSet<UserActionLog> UserActionLogs { get; set; }
        public DbSet<GeneratedReport> GeneratedReports { get; set; }
        public DbSet<ReportSchedule> ReportSchedules { get; set; }
        public DbSet<LicenseRecord> LicenseRecords { get; set; }
        public DbSet<LicenseAuditLog> LicenseAuditLogs { get; set; }
        public DbSet<LicenseReminderLog> LicenseReminderLogs { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<SystemNotification> SystemNotifications { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatFeedback> ChatFeedback { get; set; }
        public DbSet<ChatEscalation> ChatEscalations { get; set; }
        public DbSet<ChatbotEventLog> ChatbotEventLogs { get; set; }
        public DbSet<ChatbotConsent> ChatbotConsents { get; set; }
        public DbSet<ChatbotConsentAudit> ChatbotConsentAudits { get; set; }

        // Settings & Configuration
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Language> Languages { get; set; }

        // CMS & Public Website
        public DbSet<CmsPage> CmsPages { get; set; }
        public DbSet<CmsMenuItem> CmsMenuItems { get; set; }
        public DbSet<CmsNotice> CmsNotices { get; set; }
        public DbSet<DoctorShift> DoctorShifts { get; set; }
        public DbSet<PublicAppointmentRequest> PublicAppointmentRequests { get; set; }

        // M10 · Ambulance
        public DbSet<AmbulanceVehicle> AmbulanceVehicles { get; set; }
        public DbSet<AmbulanceDispatch> AmbulanceDispatches { get; set; }

        // M12 · Birth / Death Records
        public DbSet<BirthRecord> BirthRecords { get; set; }
        public DbSet<DeathRecord> DeathRecords { get; set; }

        // M15 · TPA
        public DbSet<TpaProvider> TpaProviders { get; set; }
        public DbSet<TpaClaim> TpaClaims { get; set; }

        // M17 · Internal Messaging
        public DbSet<InternalMessage> InternalMessages { get; set; }

        // M18 · Inventory
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }

        // M19 · Download Center
        public DbSet<DownloadFile> DownloadFiles { get; set; }

        // M22 · Live Consultation
        public DbSet<LiveConsultationSession> LiveConsultationSessions { get; set; }

        // RBAC Entities (from migration analysis)
        public new DbSet<Role> Roles { get; set; }
        public DbSet<Feature> Features { get; set; }
        public DbSet<RoleFeature> RoleFeatures { get; set; }
        public DbSet<StaffRole> StaffRoles { get; set; }

        // Module Management
        public DbSet<SystemModule> SystemModules { get; set; }
        public DbSet<UserModuleAccess> UserModuleAccesses { get; set; }
        public DbSet<AccountApprovalRequest> AccountApprovalRequests { get; set; }
        public DbSet<UserThemePreference> UserThemePreferences { get; set; }

            // Report Templates & Customization (STEP 5.3)
            public DbSet<ReportTemplate> ReportTemplates { get; set; }
            public DbSet<ReportField> ReportFields { get; set; }
            public DbSet<ReportFilter> ReportFilters { get; set; }
            public DbSet<ReportDesign> ReportDesigns { get; set; }
            public DbSet<ReportChart> ReportCharts { get; set; }
            public DbSet<SavedReport> SavedReports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            ConfigureRelationships(modelBuilder);

            // Configure decimal precision/scale to avoid SQL Server truncation defaults
            ConfigureDecimalPrecision(modelBuilder);

            // Multi-hospital: hospital tables and per-hospital filters
            ConfigureHospitals(modelBuilder);

            // Quality management
            ConfigureQuality(modelBuilder);

            // Equipment register
            ConfigureEquipment(modelBuilder);

            // Lab traceability
            ConfigureLabTraceability(modelBuilder);

            // Vendors / purchasing
            ConfigurePurchasing(modelBuilder);

            // Operation theatre master and availability
            ConfigureOperationTheatres(modelBuilder);

            // Patient records and documents
            ConfigurePatientRecords(modelBuilder);

            // Discharge reports of in-patient admissions
            ConfigureDischargeSummaries(modelBuilder);

            // Seed initial data
            SeedInitialData(modelBuilder);
        }

        private void ConfigureRelationships(ModelBuilder modelBuilder)
        {
            // Patient relationships
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.Appointments)
                .WithOne(a => a.Patient)
                .HasForeignKey(a => a.PatientId);

            modelBuilder.Entity<Patient>()
                .HasMany(p => p.Insurances)
                .WithOne(i => i.Patient)
                .HasForeignKey(i => i.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PatientInsurance>()
                .HasIndex(i => new { i.PatientId, i.IsActive, i.ValidTo });

            modelBuilder.Entity<UserThemePreference>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserThemePreference>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            // Appointment relationships
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId);

            // Staff navigation on Appointment is a legacy compatibility shim that maps to DoctorId.
            // Ignore it so EF Core does not create a spurious FK to the Staff table.
            modelBuilder.Entity<Appointment>()
                .Ignore(a => a.Staff);

            // OPD/IPD relationships
            modelBuilder.Entity<OPDVisit>()
                .HasOne(o => o.Patient)
                .WithMany(p => p.OPDVisits)
                .HasForeignKey(o => o.PatientId);

            modelBuilder.Entity<VisitNoteHistory>()
                .HasOne(h => h.OPDVisit)
                .WithMany(v => v.NoteHistory)
                .HasForeignKey(h => h.OPDVisitId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VisitNoteHistory>()
                .HasIndex(h => new { h.OPDVisitId, h.UpdatedAtUtc });

            modelBuilder.Entity<LabNoteHistory>()
                .HasOne(h => h.LabResult)
                .WithMany(r => r.NoteHistory)
                .HasForeignKey(h => h.LabResultId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LabNoteHistory>()
                .HasIndex(h => new { h.LabResultId, h.UpdatedAtUtc });

            modelBuilder.Entity<IPDAdmission>()
                .HasOne(i => i.Patient)
                .WithMany(p => p.IPDAdmissions)
                .HasForeignKey(i => i.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IPDAdmission>()
                .HasOne(i => i.Bed)
                .WithMany()
                .HasForeignKey(i => i.BedId);

            // Medical Record relationships
            modelBuilder.Entity<MedicalRecord>()
                .HasOne(m => m.Patient)
                .WithMany()
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TestResult>()
                .HasOne(t => t.Patient)
                .WithMany()
                .HasForeignKey(t => t.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Step 3.3 relationships
            modelBuilder.Entity<BloodIssue>()
                .HasOne(bi => bi.Patient)
                .WithMany()
                .HasForeignKey(bi => bi.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OTSchedule>()
                .HasOne(ot => ot.Patient)
                .WithMany()
                .HasForeignKey(ot => ot.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Referral>()
                .HasOne(r => r.Patient)
                .WithMany()
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StaffAttendance>()
                .HasOne(sa => sa.Staff)
                .WithMany()
                .HasForeignKey(sa => sa.StaffId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StaffAttendance>()
                .HasIndex(sa => new { sa.StaffId, sa.AttendanceDate })
                .IsUnique();

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(lr => lr.Staff)
                .WithMany()
                .HasForeignKey(lr => lr.StaffId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(lr => lr.LeaveType)
                .WithMany()
                .HasForeignKey(lr => lr.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveBalance>()
                .HasOne(lb => lb.Staff)
                .WithMany()
                .HasForeignKey(lb => lb.StaffId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveBalance>()
                .HasOne(lb => lb.LeaveType)
                .WithMany()
                .HasForeignKey(lb => lb.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveBalance>()
                .HasIndex(lb => new { lb.StaffId, lb.LeaveTypeId, lb.Year })
                .IsUnique();

            modelBuilder.Entity<PayrollRecord>()
                .HasOne(pr => pr.Staff)
                .WithMany()
                .HasForeignKey(pr => pr.StaffId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PayrollRecord>()
                .HasIndex(pr => new { pr.StaffId, pr.PayrollMonth })
                .IsUnique();

            modelBuilder.Entity<CertificateRecord>()
                .HasOne(cr => cr.Staff)
                .WithMany()
                .HasForeignKey(cr => cr.StaffId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IdCardRecord>()
                .HasOne(ic => ic.Staff)
                .WithMany()
                .HasForeignKey(ic => ic.StaffId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IdCardRecord>()
                .HasIndex(ic => ic.CardNumber)
                .IsUnique();

            // Generated Report relationships
            modelBuilder.Entity<GeneratedReport>()
                .HasOne(gr => gr.StaffGenerated)
                .WithMany()
                .HasForeignKey(gr => gr.GeneratedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GeneratedReport>()
                .HasIndex(gr => new { gr.CreatedDate, gr.ReportType });

            // Report Schedule relationships
            modelBuilder.Entity<ReportSchedule>()
                .HasOne(rs => rs.StaffCreated)
                .WithMany()
                .HasForeignKey(rs => rs.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReportSchedule>()
                .HasIndex(rs => new { rs.IsActive, rs.NextRunDate });

            modelBuilder.Entity<NotificationDeliveryLog>()
                .HasIndex(n => new { n.CreatedAt, n.Channel, n.Status });

            modelBuilder.Entity<LicenseRecord>()
                .HasMany(l => l.AuditLogs)
                .WithOne(a => a.LicenseRecord)
                .HasForeignKey(a => a.LicenseRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LicenseRecord>()
                .HasMany(l => l.ReminderLogs)
                .WithOne(r => r.LicenseRecord)
                .HasForeignKey(r => r.LicenseRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LicenseRecord>()
                .HasIndex(l => new { l.IsActive, l.ExpiresAtUtc });

            modelBuilder.Entity<LicenseAuditLog>()
                .HasOne(a => a.PerformedByUser)
                .WithMany()
                .HasForeignKey(a => a.PerformedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<LicenseAuditLog>()
                .HasIndex(a => new { a.LicenseRecordId, a.PerformedAtUtc });

            modelBuilder.Entity<LicenseReminderLog>()
                .HasIndex(r => new { r.LicenseRecordId, r.TargetExpiryUtc, r.TriggeredAtUtc });

            modelBuilder.Entity<UserSession>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserSession>()
                .HasIndex(s => s.SessionId)
                .IsUnique();

            modelBuilder.Entity<UserSession>()
                .HasIndex(s => new { s.IsActive, s.LastActivityUtc, s.ActiveRole });

            modelBuilder.Entity<UserSession>()
                .HasIndex(s => new { s.UserId, s.IsActive, s.LastActivityUtc });

            modelBuilder.Entity<SystemNotification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SystemNotification>()
                .HasOne(n => n.Patient)
                .WithMany(p => p.Notifications)
                .HasForeignKey(n => n.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SystemNotification>()
                .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAtUtc });

            modelBuilder.Entity<ChatSession>()
                .HasMany(s => s.Messages)
                .WithOne(m => m.Session)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatSession>()
                .HasMany(s => s.FeedbackItems)
                .WithOne(f => f.Session)
                .HasForeignKey(f => f.SessionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChatMessage>()
                .HasIndex(m => new { m.SessionId, m.CreatedAtUtc });

            modelBuilder.Entity<ChatFeedback>()
                .HasOne(f => f.Message)
                .WithMany()
                .HasForeignKey(f => f.MessageId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ChatFeedback>()
                .HasIndex(f => new { f.SessionId, f.CreatedAtUtc });

            modelBuilder.Entity<ChatEscalation>()
                .HasOne(e => e.Session)
                .WithMany(s => s.Escalations)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChatEscalation>()
                .HasOne(e => e.Message)
                .WithMany()
                .HasForeignKey(e => e.MessageId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ChatEscalation>()
                .HasIndex(e => new { e.Status, e.CreatedAtUtc });

            modelBuilder.Entity<ChatbotEventLog>()
                .HasIndex(e => new { e.SessionId, e.CreatedAtUtc });

            // Billing relationships
            modelBuilder.Entity<Bill>()
                .HasMany(b => b.BillItems)
                .WithOne(bi => bi.Bill)
                .HasForeignKey(bi => bi.BillId);

            modelBuilder.Entity<Bill>()
                .HasMany(b => b.Payments)
                .WithOne(p => p.Bill)
                .HasForeignKey(p => p.BillId);

            // RBAC relationships
            modelBuilder.Entity<RoleFeature>()
                .HasKey(rf => new { rf.RoleId, rf.FeatureId });

            modelBuilder.Entity<RoleFeature>()
                .HasOne(rf => rf.Role)
                .WithMany(r => r.RoleFeatures)
                .HasForeignKey(rf => rf.RoleId);

            modelBuilder.Entity<RoleFeature>()
                .HasOne(rf => rf.Feature)
                .WithMany(f => f.RoleFeatures)
                .HasForeignKey(rf => rf.FeatureId);

            modelBuilder.Entity<StaffRole>()
                .HasKey(sr => new { sr.StaffId, sr.RoleId });

            modelBuilder.Entity<StaffRole>()
                .HasOne(sr => sr.Staff)
                .WithMany(s => s.StaffRoles)
                .HasForeignKey(sr => sr.StaffId);

            modelBuilder.Entity<StaffRole>()
                .HasOne(sr => sr.Role)
                .WithMany(r => r.StaffRoles)
                .HasForeignKey(sr => sr.RoleId);

            // SystemModule / UserModuleAccess relationships
            modelBuilder.Entity<UserModuleAccess>()
                .HasIndex(u => new { u.UserId, u.ModuleId })
                .IsUnique();

            modelBuilder.Entity<UserModuleAccess>()
                .HasOne(u => u.User)
                .WithMany()
                .HasForeignKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserModuleAccess>()
                .HasOne(u => u.Module)
                .WithMany(m => m.UserAccesses)
                .HasForeignKey(u => u.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SystemModule>()
                .HasIndex(m => m.Key)
                .IsUnique();

            modelBuilder.Entity<AccountApprovalRequest>()
                .HasIndex(a => new { a.Status, a.RequestedAtUtc });

            modelBuilder.Entity<AccountApprovalRequest>()
                .HasIndex(a => a.RequestedUserId)
                .IsUnique();

            // CMS relationships
            modelBuilder.Entity<CmsMenuItem>()
                .HasOne(m => m.CmsPage)
                .WithMany()
                .HasForeignKey(m => m.CmsPageId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CmsPage>()
                .HasIndex(p => p.Slug)
                .IsUnique();

            modelBuilder.Entity<CmsNotice>()
                .HasIndex(n => n.Slug)
                .IsUnique();

            modelBuilder.Entity<DoctorShift>()
                .HasOne(ds => ds.Doctor)
                .WithMany()
                .HasForeignKey(ds => ds.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PublicAppointmentRequest>()
                .HasOne(r => r.Doctor)
                .WithMany()
                .HasForeignKey(r => r.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PublicAppointmentRequest>()
                .HasOne(r => r.Patient)
                .WithMany()
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApplicationUser>()
                .Property(u => u.UserName)
                .IsRequired();

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.NormalizedUserName)
                .IsUnique();

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => new { u.Id, u.NormalizedUserName })
                .IsUnique();
        }

        private static void ConfigureDecimalPrecision(ModelBuilder modelBuilder)
        {
            const int precision = 18;
            const int scale = 2;

            modelBuilder.Entity<Bed>().Property(x => x.DailyCharges).HasPrecision(precision, scale);

            modelBuilder.Entity<Bill>().Property(x => x.TotalAmount).HasPrecision(precision, scale);
            modelBuilder.Entity<Bill>().Property(x => x.PaidAmount).HasPrecision(precision, scale);
            modelBuilder.Entity<Bill>().Property(x => x.PendingAmount).HasPrecision(precision, scale);

            modelBuilder.Entity<BillItem>().Property(x => x.Amount).HasPrecision(precision, scale);
            modelBuilder.Entity<BillItem>().Property(x => x.Quantity).HasPrecision(precision, scale);
            modelBuilder.Entity<BillItem>().Property(x => x.UnitPrice).HasPrecision(precision, scale);
            modelBuilder.Entity<BillItem>().Property(x => x.TotalPrice).HasPrecision(precision, scale);

            modelBuilder.Entity<IPDAdmission>().Property(x => x.DailyCharges).HasPrecision(precision, scale);
            modelBuilder.Entity<LabTest>().Property(x => x.Price).HasPrecision(precision, scale);
            modelBuilder.Entity<Medicine>().Property(x => x.UnitPrice).HasPrecision(precision, scale);
            modelBuilder.Entity<OPDVisit>().Property(x => x.ConsultationFee).HasPrecision(precision, scale);
            modelBuilder.Entity<Referral>().Property(x => x.ApprovedAmount).HasPrecision(precision, scale);
            modelBuilder.Entity<Payment>().Property(x => x.Amount).HasPrecision(precision, scale);

            modelBuilder.Entity<PharmacyBill>().Property(x => x.TotalAmount).HasPrecision(precision, scale);
            modelBuilder.Entity<PharmacyBill>().Property(x => x.PaidAmount).HasPrecision(precision, scale);

            modelBuilder.Entity<PayrollRecord>().Property(x => x.BasicSalary).HasPrecision(precision, scale);
            modelBuilder.Entity<PayrollRecord>().Property(x => x.Allowances).HasPrecision(precision, scale);
            modelBuilder.Entity<PayrollRecord>().Property(x => x.Deductions).HasPrecision(precision, scale);
            modelBuilder.Entity<PayrollRecord>().Property(x => x.NetSalary).HasPrecision(precision, scale);

            modelBuilder.Entity<Prescription>().Property(x => x.UnitPrice).HasPrecision(precision, scale);
            modelBuilder.Entity<Prescription>().Property(x => x.TotalPrice).HasPrecision(precision, scale);

            modelBuilder.Entity<RadiologyTest>().Property(x => x.Price).HasPrecision(precision, scale);
            modelBuilder.Entity<Staff>().Property(x => x.Salary).HasPrecision(precision, scale);
            modelBuilder.Entity<Transaction>().Property(x => x.Amount).HasPrecision(precision, scale);

            // M10 · Ambulance (new modules)
            modelBuilder.Entity<AmbulanceDispatch>().Property(x => x.DistanceKm).HasPrecision(precision, scale);
            modelBuilder.Entity<AmbulanceDispatch>().Property(x => x.Charges).HasPrecision(precision, scale);

            // M12 · Birth / Death Records
            modelBuilder.Entity<BirthRecord>().Property(x => x.WeightKg).HasPrecision(precision, scale);

            // M15 · TPA
            modelBuilder.Entity<TpaClaim>().Property(x => x.ClaimedAmount).HasPrecision(precision, scale);
            modelBuilder.Entity<TpaClaim>().Property(x => x.ApprovedAmount).HasPrecision(precision, scale);
            modelBuilder.Entity<TpaClaim>().Property(x => x.SettledAmount).HasPrecision(precision, scale);

            // M18 · Inventory
            modelBuilder.Entity<InventoryItem>().Property(x => x.CurrentStock).HasPrecision(precision, scale);
            modelBuilder.Entity<InventoryItem>().Property(x => x.MinimumStock).HasPrecision(precision, scale);
            modelBuilder.Entity<InventoryItem>().Property(x => x.ReorderLevel).HasPrecision(precision, scale);
            modelBuilder.Entity<InventoryItem>().Property(x => x.UnitCost).HasPrecision(precision, scale);

            modelBuilder.Entity<InventoryTransaction>().Property(x => x.Quantity).HasPrecision(precision, scale);
            modelBuilder.Entity<InventoryTransaction>().Property(x => x.UnitCost).HasPrecision(precision, scale);

                // Report entities
                modelBuilder.Entity<SavedReport>().Property(x => x.ExecutionTimeMs).HasPrecision(precision, scale);
        }

        private void SeedInitialData(ModelBuilder modelBuilder)
        {
            // Seed SuperAdmin user (from migration analysis)
            var superAdminId = "1";
            modelBuilder.Entity<ApplicationUser>().HasData(
                new ApplicationUser
                {
                    Id = superAdminId,
                    UserName = "superadmin",
                    Email = "superadmin@hospital.com",
                    EmailConfirmed = true,
                    EmployeeId = "SUPER001",
                    FirstName = "Super",
                    LastName = "Admin",
                    IsActive = true
                }
            );

            // Seed basic roles and features will be added via migrations
            // This ensures we have the foundation for RBAC
        }

        private void ConfigureHospitals(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Hospital>().HasIndex(h => h.Code).IsUnique();

            modelBuilder.Entity<UserHospitalAccess>()
                .HasOne(a => a.Hospital).WithMany().HasForeignKey(a => a.HospitalId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserHospitalAccess>()
                .HasOne<ApplicationUser>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserHospitalAccess>().HasIndex(a => new { a.UserId, a.HospitalId }).IsUnique();

            // Printers of one hospital, or of every hospital (HospitalId null). Not filtered by the active hospital:
            // the printer pages choose the printers themselves.
            modelBuilder.Entity<Printer>()
                .HasOne(p => p.Hospital).WithMany().HasForeignKey(p => p.HospitalId).OnDelete(DeleteBehavior.Restrict);

            // Records that belong to one hospital: shown only for the active hospital (no filter when viewing all
            // hospitals, in the patient portal, on the public site, at start-up and in background jobs).
            ApplyHospitalFilter<Appointment>(modelBuilder);
            ApplyHospitalFilter<OPDVisit>(modelBuilder);
            ApplyHospitalFilter<IPDAdmission>(modelBuilder);
            ApplyHospitalFilter<Ward>(modelBuilder);
            ApplyHospitalFilter<Bill>(modelBuilder);
            ApplyHospitalFilter<PharmacyBill>(modelBuilder);
            ApplyHospitalFilter<InventoryItem>(modelBuilder);
            ApplyHospitalFilter<OTSchedule>(modelBuilder);

            // Child records follow their parent's hospital.
            modelBuilder.Entity<Bed>().HasQueryFilter(b => !HospitalFilterEnabled || b.Ward.HospitalId == HospitalFilterId);
            modelBuilder.Entity<BillItem>().HasQueryFilter(i => !HospitalFilterEnabled || i.Bill.HospitalId == HospitalFilterId);
            modelBuilder.Entity<Payment>().HasQueryFilter(p => !HospitalFilterEnabled || p.Bill.HospitalId == HospitalFilterId);
            modelBuilder.Entity<Prescription>().HasQueryFilter(p => !HospitalFilterEnabled || p.PharmacyBill.HospitalId == HospitalFilterId);
            modelBuilder.Entity<InventoryTransaction>().HasQueryFilter(t => !HospitalFilterEnabled || t.InventoryItem.HospitalId == HospitalFilterId);
            modelBuilder.Entity<VisitNoteHistory>().HasQueryFilter(v => !HospitalFilterEnabled || v.OPDVisit.HospitalId == HospitalFilterId);
        }

        private void ConfigureQuality(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QualityIncident>().HasIndex(i => i.IncidentNumber).IsUnique();
            modelBuilder.Entity<QualityIncident>().HasOne(i => i.Patient).WithMany().HasForeignKey(i => i.PatientId).OnDelete(DeleteBehavior.ClientSetNull);
            modelBuilder.Entity<CapaAction>().HasOne(c => c.Incident).WithMany(i => i.CapaActions).HasForeignKey(c => c.IncidentId).OnDelete(DeleteBehavior.ClientSetNull);
            modelBuilder.Entity<CapaAction>().HasOne(c => c.AuditFinding).WithMany().HasForeignKey(c => c.AuditFindingId).OnDelete(DeleteBehavior.ClientSetNull);
            modelBuilder.Entity<ControlledDocument>().HasIndex(d => d.DocumentNumber).IsUnique();
            modelBuilder.Entity<DocumentVersion>().HasOne(v => v.Document).WithMany(d => d.Versions).HasForeignKey(v => v.DocumentId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<DocumentVersion>().HasIndex(v => new { v.DocumentId, v.VersionNumber }).IsUnique();
            modelBuilder.Entity<InternalAudit>().HasIndex(a => a.AuditNumber).IsUnique();
            modelBuilder.Entity<AuditFinding>().HasOne(f => f.Audit).WithMany(a => a.Findings).HasForeignKey(f => f.AuditId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TrainingRecord>().Property(t => t.StaffId).HasMaxLength(450);
            modelBuilder.Entity<TrainingRecord>().HasIndex(t => t.StaffId);

            // Incidents, CAPA and audits belong to a hospital; findings follow their audit.
            ApplyHospitalFilter<QualityIncident>(modelBuilder);
            ApplyHospitalFilter<CapaAction>(modelBuilder);
            ApplyHospitalFilter<InternalAudit>(modelBuilder);
            modelBuilder.Entity<AuditFinding>().HasQueryFilter(f => !HospitalFilterEnabled || f.Audit.HospitalId == HospitalFilterId);
        }

        private void ConfigureEquipment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Equipment>().ToTable("Equipment");
            modelBuilder.Entity<Equipment>().HasIndex(e => e.AssetTag).IsUnique();
            modelBuilder.Entity<EquipmentServiceRecord>().HasOne(r => r.Equipment).WithMany(e => e.ServiceRecords).HasForeignKey(r => r.EquipmentId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<EquipmentServiceRecord>().Property(r => r.Cost).HasPrecision(18, 2);
            ApplyHospitalFilter<Equipment>(modelBuilder);
            modelBuilder.Entity<EquipmentServiceRecord>().HasQueryFilter(r => !HospitalFilterEnabled || r.Equipment.HospitalId == HospitalFilterId);
        }

        private static void ConfigurePatientRecords(ModelBuilder modelBuilder)
        {
            // One medical record per source visit / admission.
            modelBuilder.Entity<MedicalRecord>().HasIndex(m => new { m.SourceType, m.SourceId }).IsUnique().HasFilter("[SourceId] IS NOT NULL");
            modelBuilder.Entity<PatientDocument>().HasOne(d => d.Patient).WithMany().HasForeignKey(d => d.PatientId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PatientDocument>().HasIndex(d => new { d.PatientId, d.IsDeleted });
        }

        private void ConfigureOperationTheatres(ModelBuilder modelBuilder)
        {
            // Theatre codes are unique within a hospital.
            modelBuilder.Entity<OperationTheatre>().HasIndex(t => new { t.HospitalId, t.Code }).IsUnique();
            modelBuilder.Entity<OTBlock>().HasOne(b => b.Theatre).WithMany(t => t.Blocks).HasForeignKey(b => b.OperationTheatreId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OTBlock>().HasIndex(b => new { b.OperationTheatreId, b.StartsAt });
            modelBuilder.Entity<OTSchedule>().HasOne(o => o.Theatre).WithMany().HasForeignKey(o => o.OperationTheatreId).OnDelete(DeleteBehavior.ClientSetNull);
            modelBuilder.Entity<OTSchedule>().HasOne(o => o.Surgeon).WithMany().HasForeignKey(o => o.SurgeonDoctorId).OnDelete(DeleteBehavior.ClientSetNull);
            modelBuilder.Entity<OTSchedule>().HasIndex(o => new { o.OperationTheatreId, o.ScheduledDate });

            // Theatres belong to a hospital; block times follow their theatre.
            ApplyHospitalFilter<OperationTheatre>(modelBuilder);
            modelBuilder.Entity<OTBlock>().HasQueryFilter(b => !HospitalFilterEnabled || b.Theatre.HospitalId == HospitalFilterId);
        }

        private void ConfigurePurchasing(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Vendor>().HasIndex(v => v.VendorCode).IsUnique();
            modelBuilder.Entity<InventoryItem>().HasOne(i => i.Vendor).WithMany().HasForeignKey(i => i.VendorId).OnDelete(DeleteBehavior.ClientSetNull);
            modelBuilder.Entity<InventoryTransaction>().Property(t => t.Department).HasMaxLength(100);

            modelBuilder.Entity<PurchaseBill>().HasIndex(b => b.BillNumber).IsUnique();
            // Duplicate vendor invoices are refused in code (a cancelled entry may be re-entered).
            modelBuilder.Entity<PurchaseBill>().HasIndex(b => new { b.VendorId, b.VendorInvoiceNumber });
            modelBuilder.Entity<PurchaseBill>().HasOne(b => b.Vendor).WithMany().HasForeignKey(b => b.VendorId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PurchaseBillItem>().HasOne(i => i.PurchaseBill).WithMany(b => b.Items).HasForeignKey(i => i.PurchaseBillId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PurchaseBillItem>().HasOne(i => i.InventoryItem).WithMany().HasForeignKey(i => i.InventoryItemId).OnDelete(DeleteBehavior.ClientSetNull);
            modelBuilder.Entity<VendorPayment>().HasOne(p => p.PurchaseBill).WithMany(b => b.Payments).HasForeignKey(p => p.PurchaseBillId).OnDelete(DeleteBehavior.Cascade);
            foreach (var p in new[] { "SubTotal", "TaxAmount", "TotalAmount", "PaidAmount" }) modelBuilder.Entity<PurchaseBill>().Property(p).HasPrecision(18, 2);
            foreach (var p in new[] { "Quantity", "UnitCost", "TaxPercent", "LineTotal" }) modelBuilder.Entity<PurchaseBillItem>().Property(p).HasPrecision(18, 2);
            modelBuilder.Entity<VendorPayment>().Property(p => p.Amount).HasPrecision(18, 2);

            // Purchase bills belong to a hospital; their lines and payments follow the bill.
            ApplyHospitalFilter<PurchaseBill>(modelBuilder);
            modelBuilder.Entity<PurchaseBillItem>().HasQueryFilter(i => !HospitalFilterEnabled || i.PurchaseBill.HospitalId == HospitalFilterId);
            modelBuilder.Entity<VendorPayment>().HasQueryFilter(p => !HospitalFilterEnabled || p.PurchaseBill.HospitalId == HospitalFilterId);
        }

        private static void ConfigureLabTraceability(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LabResult>().Property(r => r.AccessionNumber).HasMaxLength(30);
            modelBuilder.Entity<LabResult>().HasIndex(r => r.AccessionNumber).IsUnique().HasFilter("[AccessionNumber] IS NOT NULL");
            modelBuilder.Entity<LabResult>().Property(r => r.SampleType).HasMaxLength(50);
            modelBuilder.Entity<LabResult>().Property(r => r.SampleStatus).HasMaxLength(30);
            modelBuilder.Entity<LabSampleEvent>().Property(e => e.EventType).HasMaxLength(40);
            modelBuilder.Entity<LabSampleEvent>().HasOne(e => e.LabResult).WithMany(r => r.SampleEvents).HasForeignKey(e => e.LabResultId).OnDelete(DeleteBehavior.Cascade);
        }

        private void ConfigureDischargeSummaries(ModelBuilder modelBuilder)
        {
            // One discharge report per admission; the patient's reports are listed newest first.
            modelBuilder.Entity<DischargeSummary>().HasIndex(d => d.IPDAdmissionId).IsUnique();
            modelBuilder.Entity<DischargeSummary>().HasIndex(d => new { d.PatientId, d.DischargeDate });
            modelBuilder.Entity<DischargeSummary>().HasOne(d => d.Admission).WithMany().HasForeignKey(d => d.IPDAdmissionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DischargeSummary>().HasOne(d => d.Patient).WithMany().HasForeignKey(d => d.PatientId).OnDelete(DeleteBehavior.Restrict);
            ApplyHospitalFilter<DischargeSummary>(modelBuilder);
        }

        private void ApplyHospitalFilter<T>(ModelBuilder modelBuilder) where T : class, IHospitalScoped
        {
            modelBuilder.Entity<T>().HasQueryFilter(e => !HospitalFilterEnabled || e.HospitalId == HospitalFilterId);
            modelBuilder.Entity<T>().HasIndex(e => e.HospitalId);
        }

        // New hospital-scoped records get the active hospital (or the default hospital outside a staff request).
        // An admission takes the hospital of its bed, so the bed and the admission always match.
        private async Task AssignHospitalAsync(bool async, CancellationToken cancellationToken = default)
        {
            // Edit forms do not post HospitalId, so an Update() of a posted entity carries null: keep the stored hospital.
            foreach (var entry in ChangeTracker.Entries<IHospitalScoped>()
                         .Where(e => e.State == EntityState.Modified && e.Entity.HospitalId == null))
            {
                entry.Property(nameof(IHospitalScoped.HospitalId)).IsModified = false;
            }

            var added = ChangeTracker.Entries<IHospitalScoped>()
                .Where(e => e.State == EntityState.Added && e.Entity.HospitalId == null)
                .Select(e => e.Entity)
                .ToList();
            if (added.Count == 0)
            {
                return;
            }

            int? fallback = _hospitalContext?.HospitalIdForNewRecords;
            var fallbackLoaded = fallback.HasValue;
            foreach (var entity in added)
            {
                if (entity is IPDAdmission admission && admission.BedId.HasValue)
                {
                    var bedId = admission.BedId.Value;
                    var bedQuery = Beds.IgnoreQueryFilters().Where(b => b.Id == bedId).Select(b => b.Ward.HospitalId);
                    var bedHospital = async ? await bedQuery.FirstOrDefaultAsync(cancellationToken) : bedQuery.FirstOrDefault();
                    if (bedHospital.HasValue)
                    {
                        entity.HospitalId = bedHospital;
                        continue;
                    }
                }

                if (!fallbackLoaded)
                {
                    var defaultQuery = Hospitals.Where(h => h.IsActive).OrderByDescending(h => h.IsDefault).ThenBy(h => h.Id).Select(h => (int?)h.Id);
                    fallback = async ? await defaultQuery.FirstOrDefaultAsync(cancellationToken) : defaultQuery.FirstOrDefault();
                    fallbackLoaded = true;
                }
                entity.HospitalId = fallback;
            }
        }

        // MVC's default ConvertEmptyStringToNull binds an empty optional form field to null,
        // which format-only attributes like [Phone]/[EmailAddress] correctly skip during
        // validation (they only run on non-null values). But entities default these same
        // optional strings to string.Empty, and their DB columns are NOT NULL by convention,
        // so an explicit null from binding fails at insert/update time. Coalescing null to
        // string.Empty here (once, centrally) fixes that without touching per-field validation.
        /// <summary>
        /// Edit forms usually post the whole record without its creation date, so Update() would overwrite it with the
        /// model default (now). A record's creation date never changes after it was inserted.
        /// </summary>
        private void KeepCreationDates()
        {
            foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Modified))
            {
                foreach (var name in new[] { "CreatedDate", "CreatedAt" })
                {
                    if (entry.Metadata.FindProperty(name) != null)
                    {
                        entry.Property(name).IsModified = false;
                    }
                }
            }
        }

        private void CoalesceNullStringsToEmpty()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State != EntityState.Added && entry.State != EntityState.Modified)
                {
                    continue;
                }

                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.ClrType == typeof(string)
                        && !property.Metadata.IsNullable
                        && property.CurrentValue == null)
                    {
                        property.CurrentValue = string.Empty;
                    }
                }
            }
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            CoalesceNullStringsToEmpty();
            KeepCreationDates();
            AssignHospitalAsync(async: false).GetAwaiter().GetResult();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            CoalesceNullStringsToEmpty();
            KeepCreationDates();
            await AssignHospitalAsync(async: true, cancellationToken);
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
