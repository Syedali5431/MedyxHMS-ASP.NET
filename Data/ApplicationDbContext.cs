using MedyxHMS.Data;
using MedyxHMS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

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
        public DbSet<Referral> Referrals { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
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

        // Audit & Reporting
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<NotificationDeliveryLog> NotificationDeliveryLogs { get; set; }
        public DbSet<UserActionLog> UserActionLogs { get; set; }
        public DbSet<GeneratedReport> GeneratedReports { get; set; }
        public DbSet<ReportSchedule> ReportSchedules { get; set; }
        public DbSet<LicenseRecord> LicenseRecords { get; set; }
        public DbSet<LicenseAuditLog> LicenseAuditLogs { get; set; }
        public DbSet<LicenseReminderLog> LicenseReminderLogs { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatFeedback> ChatFeedback { get; set; }
        public DbSet<ChatEscalation> ChatEscalations { get; set; }
        public DbSet<ChatbotEventLog> ChatbotEventLogs { get; set; }

        // Settings & Configuration
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Language> Languages { get; set; }

        // CMS & Public Website
        public DbSet<CmsPage> CmsPages { get; set; }
        public DbSet<CmsMenuItem> CmsMenuItems { get; set; }
        public DbSet<CmsNotice> CmsNotices { get; set; }
        public DbSet<DoctorShift> DoctorShifts { get; set; }
        public DbSet<PublicAppointmentRequest> PublicAppointmentRequests { get; set; }

        // RBAC Entities (from migration analysis)
        public new DbSet<Role> Roles { get; set; }
        public DbSet<Feature> Features { get; set; }
        public DbSet<RoleFeature> RoleFeatures { get; set; }
        public DbSet<StaffRole> StaffRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            ConfigureRelationships(modelBuilder);

            // Configure decimal precision/scale to avoid SQL Server truncation defaults
            ConfigureDecimalPrecision(modelBuilder);

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

            // Appointment relationships
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId);

            // OPD/IPD relationships
            modelBuilder.Entity<OPDVisit>()
                .HasOne(o => o.Patient)
                .WithMany(p => p.OPDVisits)
                .HasForeignKey(o => o.PatientId);

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

            modelBuilder.Entity<ChatSession>()
                .HasMany(s => s.Messages)
                .WithOne(m => m.Session)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatSession>()
                .HasMany(s => s.FeedbackItems)
                .WithOne(f => f.Session)
                .HasForeignKey(f => f.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

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
                .OnDelete(DeleteBehavior.Cascade);

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
        }

        private void SeedInitialData(ModelBuilder modelBuilder)
        {
            // Seed SuperAdmin user (from migration analysis)
            var superAdminId = "superadmin-user-id";
            modelBuilder.Entity<ApplicationUser>().HasData(
                new ApplicationUser
                {
                    Id = superAdminId,
                    UserName = "superadmin@hospital.com",
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
    }
}