using MedyxHMS.Data;
using MedyxHMS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MedyxHMS.Services.Implementations
{
    public class DatabaseInitializer
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DatabaseInitializer(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task InitializeAsync()
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            // Ensure newly introduced module tables exist for existing databases
            await EnsureStep42TablesAsync();
            await EnsureNotificationDeliveryLogTableAsync();

            // Seed initial public website and booking data for Step 4.2
            await SeedStep42DefaultsAsync();

            // Seed roles and features
            await SeedRolesAndFeaturesAsync();

            // Seed SuperAdmin user
            await SeedSuperAdminUserAsync();
        }

        private async Task SeedStep42DefaultsAsync()
        {
            var now = DateTime.UtcNow;

            if (!await _context.CmsPages.AnyAsync())
            {
                _context.CmsPages.AddRange(
                    new CmsPage
                    {
                        Title = "About Us",
                        Slug = "about-us",
                        Content = "<h2>About Medyx Hospital</h2><p>Medyx Hospital provides patient-centered care with modern facilities and experienced clinicians.</p>",
                        MetaDescription = "About Medyx Hospital",
                        Status = "Published",
                        ShowInMenu = true,
                        SortOrder = 1,
                        CreatedAt = now,
                        CreatedBy = "System"
                    },
                    new CmsPage
                    {
                        Title = "Services",
                        Slug = "services",
                        Content = "<h2>Our Services</h2><ul><li>Outpatient care</li><li>Inpatient care</li><li>Diagnostics</li><li>Emergency support</li></ul>",
                        MetaDescription = "Hospital services",
                        Status = "Published",
                        ShowInMenu = true,
                        SortOrder = 2,
                        CreatedAt = now,
                        CreatedBy = "System"
                    },
                    new CmsPage
                    {
                        Title = "Contact",
                        Slug = "contact",
                        Content = "<h2>Contact Us</h2><p>Phone: +000-000-0000</p><p>Email: info@medyxhospital.com</p>",
                        MetaDescription = "Contact Medyx Hospital",
                        Status = "Published",
                        ShowInMenu = true,
                        SortOrder = 3,
                        CreatedAt = now,
                        CreatedBy = "System"
                    }
                );
                await _context.SaveChangesAsync();
            }

            if (!await _context.CmsMenuItems.AnyAsync())
            {
                var pages = await _context.CmsPages.Where(p => p.Status == "Published").ToListAsync();
                var about = pages.FirstOrDefault(p => p.Slug == "about-us");
                var services = pages.FirstOrDefault(p => p.Slug == "services");
                var contact = pages.FirstOrDefault(p => p.Slug == "contact");

                _context.CmsMenuItems.AddRange(
                    new CmsMenuItem { Label = "Home", Url = "/Site", SortOrder = 0, IsActive = true },
                    new CmsMenuItem { Label = "About", Url = "/site/page/about-us", CmsPageId = about?.Id, SortOrder = 1, IsActive = true },
                    new CmsMenuItem { Label = "Services", Url = "/site/page/services", CmsPageId = services?.Id, SortOrder = 2, IsActive = true },
                    new CmsMenuItem { Label = "Notices", Url = "/Site/Notices?type=Notice", SortOrder = 3, IsActive = true },
                    new CmsMenuItem { Label = "Doctors", Url = "/Site/Doctors", SortOrder = 4, IsActive = true },
                    new CmsMenuItem { Label = "Book Appointment", Url = "/Site/BookAppointment", SortOrder = 5, IsActive = true },
                    new CmsMenuItem { Label = "Contact", Url = "/site/page/contact", CmsPageId = contact?.Id, SortOrder = 6, IsActive = true }
                );
                await _context.SaveChangesAsync();
            }

            if (!await _context.CmsNotices.AnyAsync())
            {
                _context.CmsNotices.AddRange(
                    new CmsNotice
                    {
                        Title = "Outpatient Registration Hours Updated",
                        Slug = "opd-registration-hours-updated",
                        Summary = "Registration desk now opens earlier on weekdays.",
                        Content = "<p>From next week, OPD registration opens at 7:00 AM Monday through Friday.</p>",
                        Type = "Notice",
                        IsActive = true,
                        PublishedAt = now.AddDays(-2),
                        CreatedAt = now.AddDays(-2),
                        CreatedBy = "System"
                    },
                    new CmsNotice
                    {
                        Title = "New Radiology Suite Now Operational",
                        Slug = "new-radiology-suite-operational",
                        Summary = "Advanced imaging services are now available.",
                        Content = "<p>Our new radiology suite with improved turnaround time is now open for patient services.</p>",
                        Type = "News",
                        IsActive = true,
                        PublishedAt = now.AddDays(-1),
                        CreatedAt = now.AddDays(-1),
                        CreatedBy = "System"
                    }
                );
                await _context.SaveChangesAsync();
            }

            if (!await _context.DoctorShifts.AnyAsync())
            {
                var activeDoctors = await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.Id).Take(3).ToListAsync();
                if (activeDoctors.Count > 0)
                {
                    foreach (var doctor in activeDoctors)
                    {
                        for (var day = 1; day <= 5; day++)
                        {
                            _context.DoctorShifts.Add(new DoctorShift
                            {
                                DoctorId = doctor.Id,
                                DayOfWeek = day,
                                StartTime = new TimeSpan(9, 0, 0),
                                EndTime = new TimeSpan(13, 0, 0),
                                SlotDurationMinutes = 20,
                                MaxPatientsPerSlot = 1,
                                IsActive = true
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task EnsureStep42TablesAsync()
        {
            // CmsPages
            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[CmsPages]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CmsPages] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Title] NVARCHAR(200) NOT NULL,
        [Slug] NVARCHAR(200) NOT NULL,
        [Content] NVARCHAR(MAX) NULL,
        [MetaDescription] NVARCHAR(300) NULL,
        [Status] NVARCHAR(20) NOT NULL,
        [ShowInMenu] BIT NOT NULL DEFAULT(0),
        [SortOrder] INT NOT NULL DEFAULT(0),
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        [UpdatedBy] NVARCHAR(100) NULL
    );
    CREATE UNIQUE INDEX [IX_CmsPages_Slug] ON [dbo].[CmsPages]([Slug]);
END");

            // CmsNotices
            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[CmsNotices]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CmsNotices] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Title] NVARCHAR(200) NOT NULL,
        [Slug] NVARCHAR(200) NOT NULL,
        [Summary] NVARCHAR(MAX) NULL,
        [Content] NVARCHAR(MAX) NULL,
        [Type] NVARCHAR(20) NOT NULL,
        [FeaturedImage] NVARCHAR(300) NULL,
        [IsActive] BIT NOT NULL DEFAULT(1),
        [PublishedAt] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(100) NULL
    );
    CREATE UNIQUE INDEX [IX_CmsNotices_Slug] ON [dbo].[CmsNotices]([Slug]);
END");

            // DoctorShifts
            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[DoctorShifts]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DoctorShifts] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [DoctorId] INT NOT NULL,
        [DayOfWeek] INT NOT NULL,
        [StartTime] TIME NOT NULL,
        [EndTime] TIME NOT NULL,
        [SlotDurationMinutes] INT NOT NULL DEFAULT(15),
        [MaxPatientsPerSlot] INT NOT NULL DEFAULT(1),
        [IsActive] BIT NOT NULL DEFAULT(1),
        CONSTRAINT [FK_DoctorShifts_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [dbo].[Doctors]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_DoctorShifts_DoctorId] ON [dbo].[DoctorShifts]([DoctorId]);
END");

            // PublicAppointmentRequests
            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[PublicAppointmentRequests]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PublicAppointmentRequests] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [PatientName] NVARCHAR(150) NOT NULL,
        [Phone] NVARCHAR(20) NOT NULL,
        [Email] NVARCHAR(200) NULL,
        [Gender] NVARCHAR(10) NULL,
        [Age] NVARCHAR(10) NULL,
        [PatientId] INT NOT NULL,
        [DoctorId] INT NOT NULL,
        [PreferredDate] DATETIME2 NOT NULL,
        [PreferredTime] TIME NOT NULL,
        [Symptoms] NVARCHAR(500) NULL,
        [Notes] NVARCHAR(500) NULL,
        [Status] NVARCHAR(20) NOT NULL,
        [AdminNotes] NVARCHAR(300) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NULL,
        [IpAddress] NVARCHAR(45) NULL,
        CONSTRAINT [FK_PublicAppointmentRequests_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PublicAppointmentRequests_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [dbo].[Doctors]([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_PublicAppointmentRequests_PatientId] ON [dbo].[PublicAppointmentRequests]([PatientId]);
    CREATE INDEX [IX_PublicAppointmentRequests_DoctorId] ON [dbo].[PublicAppointmentRequests]([DoctorId]);
END");

            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[PublicAppointmentRequests]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[PublicAppointmentRequests]', N'PatientId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [PatientId] INT NULL;
    END
END");

            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[PublicAppointmentRequests]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[PublicAppointmentRequests]', N'PatientId') IS NOT NULL
BEGIN
    UPDATE pr
    SET pr.PatientId = p.Id
    FROM [dbo].[PublicAppointmentRequests] pr
    INNER JOIN [dbo].[Patients] p ON p.Phone = pr.Phone
    WHERE pr.PatientId IS NULL;

    UPDATE pr
    SET pr.PatientId = p.Id
    FROM [dbo].[PublicAppointmentRequests] pr
    INNER JOIN [dbo].[Patients] p ON p.Email = pr.Email
    WHERE pr.PatientId IS NULL AND pr.Email IS NOT NULL;

    UPDATE pr
    SET pr.PatientId = fallbackPatient.Id
    FROM [dbo].[PublicAppointmentRequests] pr
    CROSS APPLY (SELECT TOP 1 Id FROM [dbo].[Patients] ORDER BY Id) fallbackPatient
    WHERE pr.PatientId IS NULL;
END");

            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[PublicAppointmentRequests]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[PublicAppointmentRequests]', N'PatientId') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PublicAppointmentRequests_Patients_PatientId'
   )
BEGIN
    ALTER TABLE [dbo].[PublicAppointmentRequests] ALTER COLUMN [PatientId] INT NOT NULL;
    ALTER TABLE [dbo].[PublicAppointmentRequests]
        ADD CONSTRAINT [FK_PublicAppointmentRequests_Patients_PatientId]
        FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients]([Id]) ON DELETE NO ACTION;
END");

            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[PublicAppointmentRequests]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[PublicAppointmentRequests]', N'PatientId') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.indexes
       WHERE name = N'IX_PublicAppointmentRequests_PatientId'
         AND object_id = OBJECT_ID(N'[dbo].[PublicAppointmentRequests]')
   )
BEGIN
    CREATE INDEX [IX_PublicAppointmentRequests_PatientId] ON [dbo].[PublicAppointmentRequests]([PatientId]);
END");

            // CmsMenuItems (depends on CmsPages)
            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[CmsMenuItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CmsMenuItems] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Label] NVARCHAR(100) NOT NULL,
        [Url] NVARCHAR(300) NULL,
        [CmsPageId] INT NULL,
        [SortOrder] INT NOT NULL DEFAULT(0),
        [IsActive] BIT NOT NULL DEFAULT(1),
        [OpenInNewTab] BIT NOT NULL DEFAULT(0),
        CONSTRAINT [FK_CmsMenuItems_CmsPages_CmsPageId] FOREIGN KEY ([CmsPageId]) REFERENCES [dbo].[CmsPages]([Id]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_CmsMenuItems_CmsPageId] ON [dbo].[CmsMenuItems]([CmsPageId]);
END");
        }

        private async Task EnsureNotificationDeliveryLogTableAsync()
        {
            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[NotificationDeliveryLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[NotificationDeliveryLogs] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Channel] NVARCHAR(20) NOT NULL,
        [Provider] NVARCHAR(50) NOT NULL,
        [Recipient] NVARCHAR(200) NOT NULL,
        [Subject] NVARCHAR(200) NULL,
        [MessageBody] NVARCHAR(MAX) NOT NULL,
        [Status] NVARCHAR(20) NOT NULL,
        [ProviderResponse] NVARCHAR(2000) NULL,
        [RelatedEntityType] NVARCHAR(50) NULL,
        [RelatedEntityId] NVARCHAR(100) NULL,
        [IsTest] BIT NOT NULL DEFAULT(0),
        [CreatedAt] DATETIME2 NOT NULL
    );

    CREATE INDEX [IX_NotificationDeliveryLogs_CreatedAt_Channel_Status]
        ON [dbo].[NotificationDeliveryLogs]([CreatedAt], [Channel], [Status]);
END");
        }

        private async Task SeedRolesAndFeaturesAsync()
        {
            // Define roles
            var roles = new[]
            {
                "SuperAdmin",
                "Admin",
                "Doctor",
                "Nurse",
                "Staff",
                "Receptionist",
                "Pharmacist",
                "LabTechnician",
                "Radiologist"
            };

            // Define features/permissions
            var features = new[]
            {
                // Patient Management
                new { Name = "ViewPatients", Module = "Patient", Description = "View patient records" },
                new { Name = "AddPatients", Module = "Patient", Description = "Add new patients" },
                new { Name = "EditPatients", Module = "Patient", Description = "Edit patient information" },
                new { Name = "DeletePatients", Module = "Patient", Description = "Delete patient records" },

                // Appointments
                new { Name = "ViewAppointments", Module = "Appointment", Description = "View appointments" },
                new { Name = "AddAppointments", Module = "Appointment", Description = "Schedule appointments" },
                new { Name = "EditAppointments", Module = "Appointment", Description = "Edit appointments" },
                new { Name = "DeleteAppointments", Module = "Appointment", Description = "Cancel appointments" },

                // Billing
                new { Name = "ViewBills", Module = "Billing", Description = "View bills and invoices" },
                new { Name = "AddBills", Module = "Billing", Description = "Create bills" },
                new { Name = "EditBills", Module = "Billing", Description = "Edit bills" },
                new { Name = "DeleteBills", Module = "Billing", Description = "Delete bills" },
                new { Name = "ProcessPayments", Module = "Billing", Description = "Process payments" },

                // OPD/IPD
                new { Name = "ViewOPDVisits", Module = "OPD", Description = "View OPD visits" },
                new { Name = "AddOPDVisits", Module = "OPD", Description = "Add OPD visits" },
                new { Name = "ViewIPDAdmissions", Module = "IPD", Description = "View IPD admissions" },
                new { Name = "AddIPDAdmissions", Module = "IPD", Description = "Add IPD admissions" },

                // Pharmacy
                new { Name = "ViewMedicines", Module = "Pharmacy", Description = "View medicines" },
                new { Name = "AddMedicines", Module = "Pharmacy", Description = "Add medicines" },
                new { Name = "DispenseMedicines", Module = "Pharmacy", Description = "Dispense medicines" },

                // Lab & Radiology
                new { Name = "ViewLabTests", Module = "Lab", Description = "View lab tests" },
                new { Name = "AddLabTests", Module = "Lab", Description = "Order lab tests" },
                new { Name = "ViewRadiologyTests", Module = "Radiology", Description = "View radiology tests" },
                new { Name = "AddRadiologyTests", Module = "Radiology", Description = "Order radiology tests" },

                // Administration
                new { Name = "ManageUsers", Module = "Admin", Description = "Manage user accounts" },
                new { Name = "ManageRoles", Module = "Admin", Description = "Manage roles and permissions" },
                new { Name = "ViewReports", Module = "Reports", Description = "View reports" },
                new { Name = "ManageSettings", Module = "Admin", Description = "Manage system settings" }
            };

            // Seed features
            foreach (var featureData in features)
            {
                if (!await _context.Features.AnyAsync(f => f.Name == featureData.Name))
                {
                    var feature = new Feature
                    {
                        Name = featureData.Name,
                        Module = featureData.Module,
                        Description = featureData.Description,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.Features.Add(feature);
                }
            }

            // Seed roles
            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }

                // Create corresponding Role entity for RBAC
                if (!await _context.Roles.AnyAsync(r => r.Name == roleName))
                {
                    var role = new Role
                    {
                        Name = roleName,
                        Description = $"{roleName} role",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.Roles.Add(role);
                    await _context.SaveChangesAsync();

                    // Assign permissions based on role
                    await AssignRolePermissionsAsync(role.Id, roleName);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task AssignRolePermissionsAsync(int roleId, string roleName)
        {
            var permissions = roleName switch
            {
                "SuperAdmin" => new[] {
                    "ViewPatients", "AddPatients", "EditPatients", "DeletePatients",
                    "ViewAppointments", "AddAppointments", "EditAppointments", "DeleteAppointments",
                    "ViewBills", "AddBills", "EditBills", "DeleteBills", "ProcessPayments",
                    "ViewOPDVisits", "AddOPDVisits", "ViewIPDAdmissions", "AddIPDAdmissions",
                    "ViewMedicines", "AddMedicines", "DispenseMedicines",
                    "ViewLabTests", "AddLabTests", "ViewRadiologyTests", "AddRadiologyTests",
                    "ManageUsers", "ManageRoles", "ViewReports", "ManageSettings"
                },
                "Admin" => new[] {
                    "ViewPatients", "AddPatients", "EditPatients",
                    "ViewAppointments", "AddAppointments", "EditAppointments",
                    "ViewBills", "AddBills", "EditBills", "ProcessPayments",
                    "ViewOPDVisits", "AddOPDVisits", "ViewIPDAdmissions", "AddIPDAdmissions",
                    "ViewMedicines", "AddMedicines", "DispenseMedicines",
                    "ViewLabTests", "AddLabTests", "ViewRadiologyTests", "AddRadiologyTests",
                    "ManageUsers", "ViewReports", "ManageSettings"
                },
                "Doctor" => new[] {
                    "ViewPatients", "EditPatients",
                    "ViewAppointments", "AddAppointments", "EditAppointments",
                    "ViewOPDVisits", "AddOPDVisits", "ViewIPDAdmissions", "AddIPDAdmissions",
                    "ViewLabTests", "AddLabTests", "ViewRadiologyTests", "AddRadiologyTests"
                },
                "Nurse" => new[] {
                    "ViewPatients",
                    "ViewAppointments",
                    "ViewOPDVisits", "AddOPDVisits", "ViewIPDAdmissions", "AddIPDAdmissions",
                    "ViewMedicines", "DispenseMedicines"
                },
                "Staff" => new[] {
                    "ViewPatients",
                    "ViewAppointments", "AddAppointments",
                    "ViewBills"
                },
                "Receptionist" => new[] {
                    "ViewPatients", "AddPatients", "EditPatients",
                    "ViewAppointments", "AddAppointments", "EditAppointments",
                    "ViewBills", "AddBills"
                },
                "Pharmacist" => new[] {
                    "ViewPatients",
                    "ViewMedicines", "AddMedicines", "DispenseMedicines"
                },
                "LabTechnician" => new[] {
                    "ViewPatients",
                    "ViewLabTests", "AddLabTests"
                },
                "Radiologist" => new[] {
                    "ViewPatients",
                    "ViewRadiologyTests", "AddRadiologyTests"
                },
                _ => Array.Empty<string>()
            };

            foreach (var permission in permissions)
            {
                var feature = await _context.Features.FirstOrDefaultAsync(f => f.Name == permission);
                if (feature != null)
                {
                    var roleFeature = new RoleFeature
                    {
                        RoleId = roleId,
                        FeatureId = feature.Id,
                        CanView = true,
                        CanAdd = permission.Contains("Add"),
                        CanEdit = permission.Contains("Edit"),
                        CanDelete = permission.Contains("Delete"),
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.RoleFeatures.Add(roleFeature);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedSuperAdminUserAsync()
        {
            const string superAdminEmail = "superadmin@hospital.com";
            const string superAdminEmployeeId = "SUPER001";

            var superAdminUser = await _userManager.FindByEmailAsync(superAdminEmail);
            if (superAdminUser == null)
            {
                superAdminUser = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    EmailConfirmed = true,
                    EmployeeId = superAdminEmployeeId,
                    FirstName = "Super",
                    LastName = "Admin",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(superAdminUser, "SuperAdmin@123!");
                if (result.Succeeded)
                {
                    // Assign SuperAdmin role
                    var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
                    if (superAdminRole != null)
                    {
                        var staffRole = new StaffRole
                        {
                            StaffId = superAdminUser.Id,
                            RoleId = superAdminRole.Id,
                            AssignedDate = DateTime.UtcNow,
                            AssignedBy = "System"
                        };
                        _context.StaffRoles.Add(staffRole);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }
    }
}