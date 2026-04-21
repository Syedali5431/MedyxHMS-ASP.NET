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
            await EnsureLicenseTablesAsync();
            await EnsureChatbotTablesAsync();
            await EnsureModuleTablesAsync();
            await EnsureAccountApprovalTableAsync();

            // Seed initial public website and booking data for Step 4.2
            await SeedStep42DefaultsAsync();
            await SeedLicenseDefaultsAsync();
            await SeedSystemModulesAsync();

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

            await EnsureSystemSettingAsync("PublicSiteAddress", "Medyx Hospital, Main Road, Your City", "string", "PublicSite", "Public website address displayed on contact/location pages.");
            await EnsureSystemSettingAsync("PublicSitePhone", "+000-000-0000", "string", "PublicSite", "Public website contact phone number.");
            await EnsureSystemSettingAsync("PublicSiteEmail", "info@medyxhospital.com", "string", "PublicSite", "Public website contact email address.");
            await EnsureSystemSettingAsync("PublicSiteMapEmbedUrl", "", "string", "PublicSite", "Optional Google map embed URL; when empty, map is generated from address.");
            await EnsureSystemSettingAsync("PublicSiteCareersContent", "We are hiring doctors, nurses, technicians, and support staff. Share your resume using the contact email.", "string", "PublicSite", "Public careers page content.");
            await EnsureSystemSettingAsync("PublicSiteHomeTitle", "Medyx Hospital", "string", "PublicSite", "Home page title text.");
            await EnsureSystemSettingAsync("PublicSiteHomeTagline", "Compassionate Care, Advanced Medicine", "string", "PublicSite", "Home page tagline text.");
            await EnsureSystemSettingAsync("PublicSiteHomeDescription", "", "string", "PublicSite", "Optional home page supporting paragraph.");
            await EnsureSystemSettingAsync("PublicSiteHomeFontFamily", "", "string", "PublicSite", "Optional home page font family style.");
            await EnsureSystemSettingAsync("PublicSiteHomeHeroImage", "", "string", "PublicSite", "Optional home page hero image path.");
            await EnsureSystemSettingAsync("PublicSiteContactDescription", "", "string", "PublicSite", "Optional contact page supporting paragraph.");
            await EnsureSystemSettingAsync("PublicSiteContactFontFamily", "", "string", "PublicSite", "Optional contact page font family style.");
            await EnsureSystemSettingAsync("PublicSiteContactHeroImage", "", "string", "PublicSite", "Optional contact page hero image path.");
            await EnsureSystemSettingAsync("PublicSiteLocationDescription", "", "string", "PublicSite", "Optional location page supporting paragraph.");
            await EnsureSystemSettingAsync("PublicSiteLocationFontFamily", "", "string", "PublicSite", "Optional location page font family style.");
            await EnsureSystemSettingAsync("PublicSiteLocationHeroImage", "", "string", "PublicSite", "Optional location page hero image path.");
            await EnsureSystemSettingAsync("PublicSitePrimaryColor", "#1a5276", "string", "PublicSite", "Primary theme color used on public-site header and buttons.");
            await EnsureSystemSettingAsync("PublicSiteAccentColor", "#2980b9", "string", "PublicSite", "Accent theme color used on public-site highlights.");
            await EnsureSystemSettingAsync("PublicSiteSurfaceColor", "#f4f8fb", "string", "PublicSite", "Background surface color for the public site.");
            await EnsureSystemSettingAsync("PublicSiteThemePreset", "Custom", "string", "PublicSite", "Optional style preset key selected in style studio.");
            await EnsureSystemSettingAsync("PublicSiteHeadingStyle", "Normal", "string", "PublicSite", "Heading text transform style: Normal, Uppercase, Capitalize.");
            await EnsureSystemSettingAsync("PublicSiteButtonStyle", "Rounded", "string", "PublicSite", "Button shape style: Rounded, Pill, Square.");
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
        [FeaturedImage] NVARCHAR(300) NULL,
        [FontFamily] NVARCHAR(100) NULL,
        [FontSizePx] INT NULL,
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

            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[CmsPages]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[CmsPages]', N'FeaturedImage') IS NULL
        ALTER TABLE [dbo].[CmsPages] ADD [FeaturedImage] NVARCHAR(300) NULL;

    IF COL_LENGTH(N'[dbo].[CmsPages]', N'FontFamily') IS NULL
        ALTER TABLE [dbo].[CmsPages] ADD [FontFamily] NVARCHAR(100) NULL;

    IF COL_LENGTH(N'[dbo].[CmsPages]', N'FontSizePx') IS NULL
        ALTER TABLE [dbo].[CmsPages] ADD [FontSizePx] INT NULL;
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

        private async Task EnsureLicenseTablesAsync()
        {
            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[LicenseRecords]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LicenseRecords] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [LicenseReference] NVARCHAR(100) NOT NULL,
        [ProductName] NVARCHAR(100) NOT NULL CONSTRAINT [DF_LicenseRecords_ProductName] DEFAULT('MedyxHMS'),
        [TenantId] NVARCHAR(150) NOT NULL CONSTRAINT [DF_LicenseRecords_TenantId] DEFAULT('UNCONFIGURED'),
        [LicenseGuid] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_LicenseRecords_LicenseGuid] DEFAULT('00000000-0000-0000-0000-000000000000'),
        [IssuedAtUtc] DATETIME2 NOT NULL,
        [ExpiresAtUtc] DATETIME2 NOT NULL,
        [MaxConcurrentUsers] INT NOT NULL CONSTRAINT [DF_LicenseRecords_MaxConcurrentUsers] DEFAULT(0),
        [VerificationKey] NVARCHAR(64) NOT NULL CONSTRAINT [DF_LicenseRecords_VerificationKey] DEFAULT(''),
        [LicensedModulesCsv] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_LicenseRecords_LicensedModulesCsv] DEFAULT(''),
        [PublicKeyModulusHex] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_LicenseRecords_PublicKeyModulusHex] DEFAULT(''),
        [PublicKeyExponentHex] NVARCHAR(40) NOT NULL CONSTRAINT [DF_LicenseRecords_PublicKeyExponentHex] DEFAULT(''),
        [Nonce] NVARCHAR(120) NOT NULL CONSTRAINT [DF_LicenseRecords_Nonce] DEFAULT('N/A'),
        [SignatureAlgorithm] NVARCHAR(40) NOT NULL CONSTRAINT [DF_LicenseRecords_SignatureAlgorithm] DEFAULT('RSA-SHA256'),
        [SignatureHex] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_LicenseRecords_SignatureHex] DEFAULT(''),
        [EncodedLicenseFile] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_LicenseRecords_EncodedLicenseFile] DEFAULT(''),
        [CanonicalPayloadJson] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_LicenseRecords_CanonicalPayloadJson] DEFAULT(''),
        [PayloadSha256Hex] NVARCHAR(64) NOT NULL CONSTRAINT [DF_LicenseRecords_PayloadSha256Hex] DEFAULT(''),
        [IsSignatureValid] BIT NOT NULL CONSTRAINT [DF_LicenseRecords_IsSignatureValid] DEFAULT(0),
        [LastValidatedAtUtc] DATETIME2 NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [LastReminderSentAtUtc] DATETIME2 NULL,
        [LastReminderCycleExpiryUtc] DATETIME2 NULL,
        [RenewedByUserId] NVARCHAR(450) NULL,
        [RenewedAtUtc] DATETIME2 NULL,
        [RenewalTermYears] INT NULL,
        [Notes] NVARCHAR(1000) NULL,
        [IsActive] BIT NOT NULL DEFAULT(1),
        [CreatedAtUtc] DATETIME2 NOT NULL,
        [UpdatedAtUtc] DATETIME2 NOT NULL
    );

    CREATE INDEX [IX_LicenseRecords_IsActive_ExpiresAtUtc]
        ON [dbo].[LicenseRecords]([IsActive], [ExpiresAtUtc]);
END");

            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[LicenseAuditLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LicenseAuditLogs] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [LicenseRecordId] INT NOT NULL,
        [ActionType] NVARCHAR(50) NOT NULL,
        [PerformedByUserId] NVARCHAR(450) NULL,
        [PerformedAtUtc] DATETIME2 NOT NULL,
        [OldExpiresAtUtc] DATETIME2 NULL,
        [NewExpiresAtUtc] DATETIME2 NULL,
        [RenewalTermYears] INT NULL,
        [Details] NVARCHAR(2000) NULL,
        [IpAddress] NVARCHAR(64) NULL,
        CONSTRAINT [FK_LicenseAuditLogs_LicenseRecords_LicenseRecordId]
            FOREIGN KEY ([LicenseRecordId]) REFERENCES [dbo].[LicenseRecords]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_LicenseAuditLogs_LicenseRecordId_PerformedAtUtc]
        ON [dbo].[LicenseAuditLogs]([LicenseRecordId], [PerformedAtUtc]);
END");

            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[LicenseReminderLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LicenseReminderLogs] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [LicenseRecordId] INT NOT NULL,
        [ReminderType] NVARCHAR(50) NOT NULL,
        [TargetExpiryUtc] DATETIME2 NOT NULL,
        [TriggeredAtUtc] DATETIME2 NOT NULL,
        [SentToCount] INT NOT NULL DEFAULT(0),
        [Status] NVARCHAR(30) NOT NULL,
        [ErrorMessage] NVARCHAR(2000) NULL,
        CONSTRAINT [FK_LicenseReminderLogs_LicenseRecords_LicenseRecordId]
            FOREIGN KEY ([LicenseRecordId]) REFERENCES [dbo].[LicenseRecords]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_LicenseReminderLogs_LicenseRecordId_TargetExpiryUtc_TriggeredAtUtc]
        ON [dbo].[LicenseReminderLogs]([LicenseRecordId], [TargetExpiryUtc], [TriggeredAtUtc]);
END");

            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[LicenseRecords]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[LicenseRecords]', N'ProductName') IS NULL
        ALTER TABLE [dbo].[LicenseRecords] ADD [ProductName] NVARCHAR(100) NOT NULL CONSTRAINT [DF_LicenseRecords_ProductName_Mig] DEFAULT('MedyxHMS');

    IF COL_LENGTH(N'[dbo].[LicenseRecords]', N'TenantId') IS NULL
        ALTER TABLE [dbo].[LicenseRecords] ADD [TenantId] NVARCHAR(150) NOT NULL CONSTRAINT [DF_LicenseRecords_TenantId_Mig] DEFAULT('UNCONFIGURED');

    IF COL_LENGTH(N'[dbo].[LicenseRecords]', N'LicenseGuid') IS NULL
        ALTER TABLE [dbo].[LicenseRecords] ADD [LicenseGuid] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_LicenseRecords_LicenseGuid_Mig] DEFAULT('00000000-0000-0000-0000-000000000000');

    IF COL_LENGTH(N'[dbo].[LicenseRecords]', N'MaxConcurrentUsers') IS NULL
        ALTER TABLE [dbo].[LicenseRecords] ADD [MaxConcurrentUsers] INT NOT NULL CONSTRAINT [DF_LicenseRecords_MaxConcurrentUsers_Mig] DEFAULT(0);

    IF COL_LENGTH(N'[dbo].[LicenseRecords]', N'VerificationKey') IS NULL
        ALTER TABLE [dbo].[LicenseRecords] ADD [VerificationKey] NVARCHAR(64) NOT NULL CONSTRAINT [DF_LicenseRecords_VerificationKey_Mig] DEFAULT('');

    IF COL_LENGTH(N'[dbo].[LicenseRecords]', N'LicensedModulesCsv') IS NULL
        ALTER TABLE [dbo].[LicenseRecords] ADD [LicensedModulesCsv] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_LicenseRecords_LicensedModulesCsv_Mig] DEFAULT('');

    IF COL_LENGTH(N'[dbo].[LicenseRecords]', N'PublicKeyModulusHex') IS NULL
        ALTER TABLE [dbo].[LicenseRecords] ADD [PublicKeyModulusHex] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_LicenseRecords_PublicKeyModulusHex_Mig] DEFAULT('');

    IF COL_LENGTH(N'[dbo].[LicenseRecords]', N'PublicKeyExponentHex') IS NULL
        ALTER TABLE [dbo].[LicenseRecords] ADD [PublicKeyExponentHex] NVARCHAR(40) NOT NULL CONSTRAINT [DF_LicenseRecords_PublicKeyExponentHex_Mig] DEFAULT('');

    IF COL_LENGTH(N'[dbo].[LicenseRecords]', N'Nonce') IS NULL
        ALTER TABLE [dbo].[LicenseRecords] ADD [Nonce] NVARCHAR(120) NOT NULL CONSTRAINT [DF_LicenseRecords_Nonce_Mig] DEFAULT('N/A');

    IF COL_LENGTH(N'[dbo].[LicenseRecords]', N'SignatureAlgorithm') IS NULL
        ALTER TABLE [dbo].[LicenseRecords] ADD [SignatureAlgorithm] NVARCHAR(40) NOT NULL CONSTRAINT [DF_LicenseRecords_SignatureAlgorithm_Mig] DEFAULT('RSA-SHA256');

    IF COL_LENGTH(N'[dbo].[LicenseRecords]', N'SignatureHex') IS NULL
        ALTER TABLE [dbo].[LicenseRecords] ADD [SignatureHex] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_LicenseRecords_SignatureHex_Mig] DEFAULT('');

    IF COL_LENGTH(N'[dbo].[LicenseRecords]', N'EncodedLicenseFile') IS NULL
        ALTER TABLE [dbo].[LicenseRecords] ADD [EncodedLicenseFile] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_LicenseRecords_EncodedLicenseFile_Mig] DEFAULT('');

    IF COL_LENGTH(N'[dbo].[LicenseRecords]', N'CanonicalPayloadJson') IS NULL
        ALTER TABLE [dbo].[LicenseRecords] ADD [CanonicalPayloadJson] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_LicenseRecords_CanonicalPayloadJson_Mig] DEFAULT('');

    IF COL_LENGTH(N'[dbo].[LicenseRecords]', N'PayloadSha256Hex') IS NULL
        ALTER TABLE [dbo].[LicenseRecords] ADD [PayloadSha256Hex] NVARCHAR(64) NOT NULL CONSTRAINT [DF_LicenseRecords_PayloadSha256Hex_Mig] DEFAULT('');

    IF COL_LENGTH(N'[dbo].[LicenseRecords]', N'IsSignatureValid') IS NULL
        ALTER TABLE [dbo].[LicenseRecords] ADD [IsSignatureValid] BIT NOT NULL CONSTRAINT [DF_LicenseRecords_IsSignatureValid_Mig] DEFAULT(0);

    IF COL_LENGTH(N'[dbo].[LicenseRecords]', N'LastValidatedAtUtc') IS NULL
        ALTER TABLE [dbo].[LicenseRecords] ADD [LastValidatedAtUtc] DATETIME2 NULL;
END");

            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[UserSessions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserSessions] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(450) NOT NULL,
        [SessionId] NVARCHAR(128) NOT NULL,
        [ActiveRole] NVARCHAR(50) NOT NULL,
        [IpAddress] NVARCHAR(64) NULL,
        [UserAgent] NVARCHAR(512) NULL,
        [LoginAtUtc] DATETIME2 NOT NULL,
        [LastActivityUtc] DATETIME2 NOT NULL,
        [LogoutAtUtc] DATETIME2 NULL,
        [IsActive] BIT NOT NULL DEFAULT(1),
        CONSTRAINT [FK_UserSessions_AspNetUsers_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [UX_UserSessions_SessionId] ON [dbo].[UserSessions]([SessionId]);
    CREATE INDEX [IX_UserSessions_IsActive_LastActivityUtc_ActiveRole] ON [dbo].[UserSessions]([IsActive], [LastActivityUtc], [ActiveRole]);
    CREATE INDEX [IX_UserSessions_UserId_IsActive_LastActivityUtc] ON [dbo].[UserSessions]([UserId], [IsActive], [LastActivityUtc]);
END");
        }

        private async Task EnsureChatbotTablesAsync()
        {
            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[ChatSessions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatSessions] (
        [Id] NVARCHAR(64) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(450) NULL,
        [UserRole] NVARCHAR(40) NOT NULL,
        [StartedAtUtc] DATETIME2 NOT NULL,
        [EndedAtUtc] DATETIME2 NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [Channel] NVARCHAR(20) NOT NULL,
        [IsEscalated] BIT NOT NULL DEFAULT(0),
        [IsUnresolved] BIT NOT NULL DEFAULT(0),
        [PreferredLanguage] NVARCHAR(12) NOT NULL DEFAULT('en')
    );

    CREATE INDEX [IX_ChatSessions_UserId_StartedAtUtc]
        ON [dbo].[ChatSessions]([UserId], [StartedAtUtc]);
END");

            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[ChatMessages]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatMessages] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SessionId] NVARCHAR(64) NOT NULL,
        [SenderType] NVARCHAR(20) NOT NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        [CreatedAtUtc] DATETIME2 NOT NULL,
        [ModerationStatus] NVARCHAR(30) NOT NULL,
        [TokenCount] INT NOT NULL DEFAULT(0),
        [Category] NVARCHAR(30) NOT NULL DEFAULT('General'),
        CONSTRAINT [FK_ChatMessages_ChatSessions_SessionId]
            FOREIGN KEY ([SessionId]) REFERENCES [dbo].[ChatSessions]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_ChatMessages_SessionId_CreatedAtUtc]
        ON [dbo].[ChatMessages]([SessionId], [CreatedAtUtc]);
END");

            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[ChatSessions]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[ChatSessions]', N'IsEscalated') IS NULL
        ALTER TABLE [dbo].[ChatSessions] ADD [IsEscalated] BIT NOT NULL CONSTRAINT [DF_ChatSessions_IsEscalated] DEFAULT(0);

    IF COL_LENGTH(N'[dbo].[ChatSessions]', N'IsUnresolved') IS NULL
        ALTER TABLE [dbo].[ChatSessions] ADD [IsUnresolved] BIT NOT NULL CONSTRAINT [DF_ChatSessions_IsUnresolved] DEFAULT(0);

    IF COL_LENGTH(N'[dbo].[ChatSessions]', N'PreferredLanguage') IS NULL
        ALTER TABLE [dbo].[ChatSessions] ADD [PreferredLanguage] NVARCHAR(12) NOT NULL CONSTRAINT [DF_ChatSessions_PreferredLanguage] DEFAULT('en');
END");

            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[ChatMessages]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[ChatMessages]', N'Category') IS NULL
        ALTER TABLE [dbo].[ChatMessages] ADD [Category] NVARCHAR(30) NOT NULL CONSTRAINT [DF_ChatMessages_Category] DEFAULT('General');
END");

            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[ChatFeedback]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatFeedback] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SessionId] NVARCHAR(64) NOT NULL,
        [MessageId] BIGINT NULL,
        [FeedbackType] NVARCHAR(20) NOT NULL,
        [Comment] NVARCHAR(1000) NULL,
        [CreatedAtUtc] DATETIME2 NOT NULL,
        CONSTRAINT [FK_ChatFeedback_ChatSessions_SessionId]
            FOREIGN KEY ([SessionId]) REFERENCES [dbo].[ChatSessions]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ChatFeedback_ChatMessages_MessageId]
            FOREIGN KEY ([MessageId]) REFERENCES [dbo].[ChatMessages]([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_ChatFeedback_SessionId_CreatedAtUtc]
        ON [dbo].[ChatFeedback]([SessionId], [CreatedAtUtc]);
END");

            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[ChatEscalations]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatEscalations] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SessionId] NVARCHAR(64) NOT NULL,
        [MessageId] BIGINT NULL,
        [UserId] NVARCHAR(450) NULL,
        [EscalationType] NVARCHAR(30) NOT NULL,
        [Reason] NVARCHAR(1200) NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [TargetContact] NVARCHAR(200) NULL,
        [ResolvedByUserId] NVARCHAR(450) NULL,
        [CreatedAtUtc] DATETIME2 NOT NULL,
        [ResolvedAtUtc] DATETIME2 NULL,
        CONSTRAINT [FK_ChatEscalations_ChatSessions_SessionId]
            FOREIGN KEY ([SessionId]) REFERENCES [dbo].[ChatSessions]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ChatEscalations_ChatMessages_MessageId]
            FOREIGN KEY ([MessageId]) REFERENCES [dbo].[ChatMessages]([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_ChatEscalations_Status_CreatedAtUtc]
        ON [dbo].[ChatEscalations]([Status], [CreatedAtUtc]);
END");

            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[ChatbotEventLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatbotEventLogs] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SessionId] NVARCHAR(64) NULL,
        [MessageId] BIGINT NULL,
        [EventType] NVARCHAR(50) NOT NULL,
        [Severity] NVARCHAR(20) NOT NULL,
        [Details] NVARCHAR(2000) NOT NULL,
        [CreatedAtUtc] DATETIME2 NOT NULL
    );

    CREATE INDEX [IX_ChatbotEventLogs_SessionId_CreatedAtUtc]
        ON [dbo].[ChatbotEventLogs]([SessionId], [CreatedAtUtc]);
END");

            await EnsureSystemSettingAsync("ChatbotEnabled", "true", "bool", "Chatbot", "Enable or disable chatbot globally.");
            await EnsureSystemSettingAsync("ChatbotEnabledForPatients", "true", "bool", "Chatbot", "Allow chatbot access for patient users.");
            await EnsureSystemSettingAsync("ChatbotEnabledForStaff", "true", "bool", "Chatbot", "Allow chatbot access for staff users.");
            await EnsureSystemSettingAsync("ChatbotEnabledForAdmins", "true", "bool", "Chatbot", "Allow chatbot access for admin and superadmin users.");
            await EnsureSystemSettingAsync("ChatbotEnableEscalation", "true", "bool", "Chatbot", "Allow escalation handoff workflow.");
            await EnsureSystemSettingAsync("ChatbotEnableAppointmentGuidance", "true", "bool", "Chatbot", "Enable appointment guided assistance.");
            await EnsureSystemSettingAsync("ChatbotEnableBillingGuidance", "true", "bool", "Chatbot", "Enable billing/payment guided assistance.");
            await EnsureSystemSettingAsync("ChatbotEnableMultilingual", "false", "bool", "Chatbot", "Enable multilingual strategy in chatbot responses.");
            await EnsureSystemSettingAsync("ChatbotSupportedLanguages", "en,es,fr,ar", "string", "Chatbot", "Supported language codes for chatbot usage.");
            await EnsureSystemSettingAsync("ChatbotDefaultLanguage", "en", "string", "Chatbot", "Default chatbot language.");
            await EnsureSystemSettingAsync("ChatbotModel", "gpt-4o-mini", "string", "Chatbot", "Configured provider model for chatbot responses.");
            await EnsureSystemSettingAsync("ChatbotTemperature", "0.2", "decimal", "Chatbot", "Configured model temperature for chatbot responses.");
            await EnsureSystemSettingAsync("ChatbotMaxTokens", "350", "int", "Chatbot", "Configured max token target for chatbot responses.");
            await EnsureSystemSettingAsync("ChatbotHourlyUsageLimit", "100", "int", "Chatbot", "Per-user per-hour chatbot request cap.");
            await EnsureSystemSettingAsync("ChatbotUnresolvedThreshold", "0.45", "decimal", "Chatbot", "Confidence threshold under which escalation is suggested.");
            await EnsureSystemSettingAsync("ChatbotSupportContact", "support@hospital.com", "string", "Chatbot", "Support contact for chatbot handoff.");
            await EnsureSystemSettingAsync("ChatbotAppointmentGuidance.en", "To book: open Appointment module, choose doctor, date, and confirm slot.", "string", "Chatbot", "English appointment guidance template.");
            await EnsureSystemSettingAsync("ChatbotBillingGuidance.en", "To pay bills: open Billing module, review invoice, choose method, and submit payment.", "string", "Chatbot", "English billing guidance template.");
        }

        private async Task SeedLicenseDefaultsAsync()
        {
            await EnsureSystemSettingAsync(
                "LicenseReminderSubject",
                "MedyxHMS license expires in {DaysRemaining} days",
                "string",
                "Licensing",
                "Reminder email subject template for license expiry notifications.");

            await EnsureSystemSettingAsync(
                "LicenseReminderBody",
                "This is a reminder that your MedyxHMS license will expire soon.\n\nExpiry Date: {ExpiryDate}\nDays Remaining: {DaysRemaining}\n\nPlease arrange payment and contact a SuperAdmin user to complete the renewal.\nSuperAdmin Contact: {SuperAdminContact}\nBilling Contact: {BillingContact}\n\nHospital/App: {HospitalName}",
                "string",
                "Licensing",
                "Reminder email body template for license expiry notifications.");

            await EnsureSystemSettingAsync(
                "LicenseSuperAdminContact",
                "superadmin@hospital.com",
                "string",
                "Licensing",
                "Default SuperAdmin contact guidance displayed on license workflows.");

            await EnsureSystemSettingAsync(
                "LicenseBillingContact",
                "superadmin@hospital.com",
                "string",
                "Licensing",
                "Billing contact guidance displayed on license reminders and expired screens.");

            await EnsureSystemSettingAsync(
                "LicenseExpectedProductName",
                "MedyxHMS",
                "string",
                "Licensing",
                "Expected ProductName in signed license payload.");

            await EnsureSystemSettingAsync(
                "LicenseTenantId",
                "UNCONFIGURED",
                "string",
                "Licensing",
                "Expected TenantId in signed license payload.");

            await EnsureSystemSettingAsync(
                "LicensePublicKeyModulusHex",
                string.Empty,
                "string",
                "Licensing",
                "Vendor RSA public modulus (hex) used to verify license signatures.");

            await EnsureSystemSettingAsync(
                "LicensePublicKeyExponentHex",
                string.Empty,
                "string",
                "Licensing",
                "Vendor RSA public exponent (hex) used to verify license signatures.");

            await EnsureSystemSettingAsync(
                "LicenseVerificationKey",
                string.Empty,
                "string",
                "Licensing",
                "Derived verification key fingerprint from configured public key used to bind signed licenses.");
        }

        private async Task EnsureModuleTablesAsync()
        {
            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[SystemModules]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SystemModules] (
        [Id]                INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Key]               NVARCHAR(50)  NOT NULL,
        [DisplayName]       NVARCHAR(100) NOT NULL,
        [Description]       NVARCHAR(300) NULL,
        [Icon]              NVARCHAR(100) NULL,
        [IsGloballyEnabled] BIT NOT NULL DEFAULT(1),
        [SortOrder]         INT NOT NULL DEFAULT(0),
        [CreatedAtUtc]      DATETIME2 NOT NULL,
        [UpdatedAtUtc]      DATETIME2 NOT NULL,
        [UpdatedByUserId]   NVARCHAR(450) NULL
    );
    CREATE UNIQUE INDEX [UX_SystemModules_Key] ON [dbo].[SystemModules]([Key]);
END");

            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[UserModuleAccesses]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserModuleAccesses] (
        [Id]              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId]          NVARCHAR(450) NOT NULL,
        [ModuleId]        INT NOT NULL,
        [IsEnabled]       BIT NOT NULL DEFAULT(1),
        [CreatedAtUtc]    DATETIME2 NOT NULL,
        [UpdatedAtUtc]    DATETIME2 NOT NULL,
        [UpdatedByUserId] NVARCHAR(450) NULL,
        CONSTRAINT [FK_UserModuleAccesses_AspNetUsers_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserModuleAccesses_SystemModules_ModuleId]
            FOREIGN KEY ([ModuleId]) REFERENCES [dbo].[SystemModules]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [UX_UserModuleAccesses_UserId_ModuleId]
        ON [dbo].[UserModuleAccesses]([UserId], [ModuleId]);
END");
        }

        private async Task EnsureAccountApprovalTableAsync()
        {
            await _context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[AccountApprovalRequests]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AccountApprovalRequests] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RequestedUserId] NVARCHAR(450) NOT NULL,
        [RequestedRole] NVARCHAR(100) NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [RequestedAtUtc] DATETIME2 NOT NULL,
        [Notes] NVARCHAR(1000) NULL,
        [ApprovedByUserId] NVARCHAR(450) NULL,
        [ApprovedAtUtc] DATETIME2 NULL,
        CONSTRAINT [FK_AccountApprovalRequests_AspNetUsers_RequestedUserId]
            FOREIGN KEY ([RequestedUserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [UX_AccountApprovalRequests_RequestedUserId]
        ON [dbo].[AccountApprovalRequests]([RequestedUserId]);

    CREATE INDEX [IX_AccountApprovalRequests_Status_RequestedAtUtc]
        ON [dbo].[AccountApprovalRequests]([Status], [RequestedAtUtc]);
END");
        }

        private static readonly (string Key, string DisplayName, string? Description, string? Icon, int SortOrder)[] DefaultModules =
        {
            ("Dashboard",          "Dashboard",                  "Main dashboard",                        "fas fa-tachometer-alt",     1),
            ("Patient",            "Patient Management",         "Patient registration and records",      "fas fa-user-injured",       2),
            ("Appointment",        "Appointments",               "Appointment scheduling",                "fas fa-calendar-check",     3),
            ("OPD",                "Outpatient Department",      "OPD visits and consultations",          "fas fa-stethoscope",        4),
            ("IPD",                "Inpatient Department",       "IPD admissions and wards",              "fas fa-bed",               5),
            ("Billing",            "Billing",                    "Invoices and payments",                 "fas fa-file-invoice-dollar", 6),
            ("Prescription",       "Pharmacy & Prescription",    "Pharmacy and prescriptions",            "fas fa-pills",             7),
            ("Lab",                "Laboratory",                 "Lab tests and results",                 "fas fa-flask",             8),
            ("Radiology",          "Radiology",                  "Radiology tests and reports",           "fas fa-x-ray",             9),
            ("BloodBank",          "Blood Bank",                 "Blood inventory and issues",            "fas fa-tint",              10),
            ("OperationTheatre",   "Operation Theatre",          "OT scheduling and records",             "fas fa-hospital",          11),
            ("FrontOffice",        "Front Office",               "Visitor and complaint management",      "fas fa-concierge-bell",    12),
            ("Attendance",         "Attendance",                 "Staff attendance tracking",             "fas fa-clipboard-check",   13),
            ("Leave",              "Leave Management",           "Leave requests and balances",           "fas fa-calendar-minus",    14),
            ("Payroll",            "Payroll",                    "Staff payroll processing",              "fas fa-money-check-alt",   15),
            ("Certificate",        "Certificates & ID Cards",    "Certificate and ID card issuance",      "fas fa-id-card",           16),
            ("Referral",           "Referrals",                  "Patient referral management",           "fas fa-share-alt",         17),
            ("Report",             "Reports",                    "System reports and analytics",          "fas fa-chart-bar",         18),
            ("PatientPortal",      "Patient Portal",             "Patient self-service portal",           "fas fa-user-circle",       19),
            ("Ambulance",          "Ambulance Management",       "Ambulance dispatch and tracking",       "fas fa-ambulance",         20),
            ("Chatbot",            "Chatbot",                    "AI chatbot assistant",                  "fas fa-robot",             21),
            ("CMS",                "CMS / Public Website",       "Public website and CMS management",     "fas fa-globe",             22),
            ("License",            "License Management",         "System license management",             "fas fa-key",               23),
        };

        private async Task SeedSystemModulesAsync()
        {
            var now = DateTime.UtcNow;
            foreach (var (key, displayName, description, icon, sortOrder) in DefaultModules)
            {
                if (!await _context.SystemModules.AnyAsync(m => m.Key == key))
                {
                    _context.SystemModules.Add(new MedyxHMS.Models.SystemModule
                    {
                        Key = key,
                        DisplayName = displayName,
                        Description = description,
                        Icon = icon,
                        IsGloballyEnabled = true,
                        SortOrder = sortOrder,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task EnsureSystemSettingAsync(string key, string value, string type, string category, string description)
        {
            if (await _context.Settings.AnyAsync(setting => setting.Key == key))
                return;

            _context.Settings.Add(new Setting
            {
                Key = key,
                Value = value,
                Type = type,
                Category = category,
                Description = description,
                IsSystem = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedBy = "System"
            });

            await _context.SaveChangesAsync();
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
                "Accountant",
                "Receptionist",
                "Pharmacist",
                "LabTechnician",
                "Radiologist",
                "Patient"
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
                    "ViewPatients", "AddPatients", "EditPatients",
                    "ViewAppointments",
                    "ViewOPDVisits", "AddOPDVisits", "ViewIPDAdmissions", "AddIPDAdmissions",
                    "ViewMedicines", "DispenseMedicines"
                },
                "Staff" => new[] {
                    "ViewPatients", "AddPatients", "EditPatients",
                    "ViewAppointments", "AddAppointments", "EditAppointments",
                    "ViewBills", "AddBills"
                },
                "Accountant" => new[] {
                    "ViewPatients", "AddPatients", "EditPatients",
                    "ViewBills", "AddBills", "EditBills", "ProcessPayments",
                    "ViewReports"
                },
                "Receptionist" => new[] {
                    "ViewPatients", "AddPatients", "EditPatients",
                    "ViewAppointments", "AddAppointments", "EditAppointments",
                    "ViewBills", "AddBills"
                },
                "Pharmacist" => new[] {
                    "ViewPatients", "AddPatients", "EditPatients",
                    "ViewMedicines", "AddMedicines", "DispenseMedicines"
                },
                "LabTechnician" => new[] {
                    "ViewPatients", "AddPatients", "EditPatients",
                    "ViewLabTests", "AddLabTests"
                },
                "Radiologist" => new[] {
                    "ViewPatients", "AddPatients", "EditPatients",
                    "ViewRadiologyTests", "AddRadiologyTests"
                },
                "Patient" => Array.Empty<string>(),
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
                if (!result.Succeeded)
                {
                    return;
                }
            }

            await EnsureStaffAndSuperAdminRoleAsync(superAdminUser, superAdminEmployeeId);
        }

        private async Task EnsureStaffAndSuperAdminRoleAsync(ApplicationUser superAdminUser, string superAdminEmployeeId)
        {
            var staff = await _context.Staff.FirstOrDefaultAsync(s => s.Id == superAdminUser.Id);
            if (staff == null)
            {
                staff = new Staff
                {
                    Id = superAdminUser.Id,
                    EmployeeId = superAdminEmployeeId,
                    FirstName = superAdminUser.FirstName ?? "Super",
                    LastName = superAdminUser.LastName ?? "Admin",
                    Department = "Administration",
                    Designation = "SuperAdmin",
                    DateOfJoining = DateTime.UtcNow,
                    Salary = 0,
                    Email = superAdminUser.Email ?? "superadmin@hospital.com",
                    Phone = string.Empty,
                    Address = string.Empty,
                    About = "System generated SuperAdmin staff profile",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    User = superAdminUser
                };
                _context.Staff.Add(staff);
                await _context.SaveChangesAsync();
            }

            var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
            if (superAdminRole == null)
            {
                return;
            }

            var hasStaffRole = await _context.StaffRoles.AnyAsync(sr => sr.StaffId == superAdminUser.Id && sr.RoleId == superAdminRole.Id);
            if (hasStaffRole)
            {
                return;
            }

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