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

        // Settings & Configuration
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

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
                .WithMany()
                .HasForeignKey(o => o.PatientId);

            modelBuilder.Entity<IPDAdmission>()
                .HasOne(i => i.Patient)
                .WithMany()
                .HasForeignKey(i => i.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IPDAdmission>()
                .HasOne(i => i.Bed)
                .WithMany()
                .HasForeignKey(i => i.BedId);

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
                    IsActive = true
                }
            );

            // Seed basic roles and features will be added via migrations
            // This ensures we have the foundation for RBAC
        }
    }
}