// Purpose: Application entry point that configures DI, middleware pipeline, and endpoint routing for Medyx HMS.
using MedyxHMS.Data;
using MedyxHMS.Extensions;
using MedyxHMS.Models;
using MedyxHMS.Services;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Centralized license gate for MVC actions. Middleware also enforces license for non-MVC paths.
    options.Filters.Add<MedyxHMS.Services.Filters.LicenseExpiryFilter>();
})
.AddMvcOptions(options =>
{
    // The project enables nullable annotations (see .csproj) but most DTO properties are
    // declared as non-nullable `string` without an explicit [Required] attribute, relying on
    // [StringLength]/etc. alone to mean "optional". Without this, ASP.NET Core's default
    // model-binding behavior treats every non-nullable reference-type property as implicitly
    // required, silently blocking submission of forms (e.g. Patient/Create) on fields the UI
    // and DTOs themselves document as optional. Explicit [Required] attributes are unaffected.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    // Labels/validation messages read "Date of birth", "Doctor" instead of "DateOfBirth", "DoctorId".
    options.ModelMetadataDetailsProviders.Add(new MedyxHMS.Extensions.HumanizedDisplayNameProvider());
});
builder.Services.AddTransient<MedyxHMS.Services.Filters.LicenseExpiryFilter>();
builder.Services.AddHttpClient();

// Configure Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            // Explicitly keep EF Core's default (single SQL query per LINQ query). Setting it removes the
            // "no QuerySplittingBehavior configured" warning without changing how any query runs.
            sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery))
        // Informational only: the connection string enables MARS, which disables savepoints.
        .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.SqlServerEventId.SavepointsDisabledBecauseOfMARS)));

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Security hardening: sign-in expires after a period of inactivity (Identity's default was 14 days).
// The value is replaced at start-up and on save by Security & Backups (Security:SessionTimeoutMinutes).
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Role-based policies
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin", "SuperAdmin"));
    options.AddPolicy("RequireDoctorRole", policy => policy.RequireRole("Doctor"));
    options.AddPolicy("RequireStaffRole", policy => policy.RequireRole("Staff", "Nurse", "Admin", "SuperAdmin"));

    // Permission-based policies (custom requirement handlers would be needed for full implementation)
    options.AddPolicy("CanViewPatients", policy => policy.RequireClaim("Permission", "ViewPatients"));
    options.AddPolicy("CanAddPatients", policy => policy.RequireClaim("Permission", "AddPatients"));
    options.AddPolicy("CanEditPatients", policy => policy.RequireClaim("Permission", "EditPatients"));
    options.AddPolicy("CanDeletePatients", policy => policy.RequireClaim("Permission", "DeletePatients"));
});

// Add Permission-based Authorization
builder.Services.AddPermissionAuthorization();

// Configure Application Services
builder.Services.AddScoped<ISettingService, SettingService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<INavAccessService, NavAccessService>();
// Multi-hospital: the active hospital of the current request (filled by HospitalContextMiddleware).
builder.Services.AddScoped<IHospitalContext, HospitalContext>();
// Thermal receipt printing (80 mm / 58 mm): printer settings and receipt headers.
builder.Services.AddScoped<IReceiptPrintService, ReceiptPrintService>();
builder.Services.AddScoped<IDischargeService, DischargeService>();
builder.Services.AddScoped<INetworkPrintService, NetworkPrintService>();
// Security hardening: policy (two-step login for admins, sign-in timeout, audit retention) and database backups.
builder.Services.AddScoped<ISecurityPolicyService, SecurityPolicyService>();
builder.Services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();
builder.Services.AddHostedService<AuditRetentionHostedService>();
// Quality management (incidents + CAPA, controlled documents, internal audits, training records).
builder.Services.AddScoped<QualityService>();
// Lab traceability (accession numbers, chain of custody, electronic sign-off).
builder.Services.AddScoped<LabTraceabilityService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IPatientPortalService, PatientPortalService>();
builder.Services.AddScoped<IEmailNotificationProvider, SmtpEmailNotificationProvider>();
builder.Services.AddScoped<TwilioSmsNotificationProvider>();
builder.Services.AddScoped<AfricaTalkingSmsNotificationProvider>();
builder.Services.AddScoped<ISmsNotificationProvider, SmsNotificationProviderRouter>();
builder.Services.AddScoped<IPublicBookingNotificationService, PublicBookingNotificationService>();
builder.Services.AddScoped<INotificationDeliveryAuditService, NotificationDeliveryAuditService>();
builder.Services.AddScoped<ISystemNotificationService, SystemNotificationService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IReportEngine, ReportEngine>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<ILicenseFileService, LicenseFileService>();
builder.Services.AddScoped<IConcurrentSessionService, ConcurrentSessionService>();
builder.Services.AddScoped<IModuleService, ModuleService>();
builder.Services.AddScoped<IChatbotModerationService, ChatbotModerationService>();
builder.Services.AddScoped<IChatbotPiiRedactionService, ChatbotPiiRedactionService>();
builder.Services.AddScoped<IChatbotPromptBuilder, ChatbotPromptBuilder>();
builder.Services.AddScoped<IChatbotKnowledgeService, ChatbotKnowledgeService>();
builder.Services.AddScoped<IChatbotService, OpenAiChatbotService>();
builder.Services.AddScoped<IChatbotConsentService, ChatbotConsentService>();
builder.Services.AddScoped<IChatbotDataCleanupService, ChatbotDataCleanupService>();
builder.Services.AddScoped<ISmtpHealthService, SmtpHealthService>();
builder.Services.AddScoped<IProfileImageService, ProfileImageService>();
builder.Services.AddScoped<IMFAService, MFAService>();

// Performance & Caching Services (STEP 5.3 - Fast & Efficient System)
// Using MemoryCache for distributed caching support
// For production, configure Redis by installing: dotnet add package StackExchange.Redis
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddScoped<ICacheService, CacheService>();

// Clinical Module Services (STEP 3.1)
builder.Services.AddScoped<IOPDService, OPDService>();
builder.Services.AddScoped<IIPDService, IPDService>();
builder.Services.AddScoped<IMedicalRecordService, MedicalRecordService>();
builder.Services.AddScoped<PatientDocumentService>();
builder.Services.AddScoped<IWardService, WardService>();
builder.Services.AddScoped<IBedService, BedService>();
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();

// Diagnostic Module Services (STEP 3.2)
builder.Services.AddScoped<ILabService, LabService>();
builder.Services.AddScoped<IRadiologyService, RadiologyService>();

// Specialized Module Services (STEP 3.3)
builder.Services.AddScoped<IBloodBankService, BloodBankService>();
builder.Services.AddScoped<IOperationTheatreService, OperationTheatreService>();
builder.Services.AddScoped<IReferralService, ReferralService>();

// Administrative Module Services (STEP 4.1)
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<IFrontOfficeService, FrontOfficeService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IReportTemplateService, ReportTemplateService>();
builder.Services.AddScoped<IReportCatalogVisibilityService, ReportCatalogVisibilityService>();

builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<DemoDataSeeder>();
builder.Services.AddScoped<Year2026SampleDataSeeder>();

// Add HttpContext accessor for audit logging
builder.Services.AddHttpContextAccessor();

// Configure Session
builder.Services.AddSession(options =>
{
    // Longer than the longest sign-in timeout choice (60 min) so session data never expires before the sign-in.
    options.IdleTimeout = TimeSpan.FromMinutes(65);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Security:Cors:AllowedOrigins")
            .Get<string[]>()
            ?? Array.Empty<string>();

        var allowedMethods = builder.Configuration
            .GetSection("Security:Cors:AllowedMethods")
            .Get<string[]>()
            ?? new[] { "GET", "POST", "OPTIONS" };

        var allowedHeaders = builder.Configuration
            .GetSection("Security:Cors:AllowedHeaders")
            .Get<string[]>()
            ?? new[] { "Content-Type", "X-Requested-With", "RequestVerificationToken" };

        var allowCredentials = builder.Configuration.GetValue<bool>("Security:Cors:AllowCredentials");

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .WithMethods(allowedMethods)
                  .WithHeaders(allowedHeaders)
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(10));

            if (allowCredentials)
            {
                policy.AllowCredentials();
            }

            return;
        }

        // If no origins are configured, block cross-origin calls by default.
        policy.SetIsOriginAllowed(_ => false)
              .WithMethods(allowedMethods)
              .WithHeaders(allowedHeaders)
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// Configure Health Checks
builder.Services.AddHealthChecks();

// Configure Response Caching
builder.Services.AddResponseCaching();

// Configure API Behavior
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

var app = builder.Build();

// The Medyx logo for generated documents (PDF, Excel, receipts).
MedyxHMS.Services.Implementations.Branding.Initialize(app.Environment.WebRootPath);

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var initializer = services.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();

    // Seed demo/dummy data for development and testing only (Seeding:DemoData).
    // The seeder's schema patch still runs in every environment.
    var demoSeeder = services.GetRequiredService<DemoDataSeeder>();
    await demoSeeder.SeedAsync(builder.Configuration.GetValue<bool>("Seeding:DemoData"));

    // Sample records for January–October 2026 in every module (once; Seeding:Year2026SampleData or Seeding:DemoData).
    if (builder.Configuration.GetValue<bool>("Seeding:Year2026SampleData") || builder.Configuration.GetValue<bool>("Seeding:DemoData"))
    {
        await services.GetRequiredService<Year2026SampleDataSeeder>().SeedAsync();
    }

    // Multi-hospital: records added without a hospital (e.g. by seed scripts) belong to the default hospital.
    await initializer.BackfillHospitalAssignmentsAsync();

    // Licence: when the active licence is missing or not signed with a trusted vendor key, load MedyxHMS.lic
    // from the application folder (a licence re-issued by the vendor, e.g. after a key change).
    try
    {
        var imported = await services.GetRequiredService<ILicenseFileService>().ImportFromApplicationFolderIfNeededAsync();
        if (imported != null)
            services.GetRequiredService<ILogger<Program>>().LogInformation("Licence {LicenseId} imported from MedyxHMS.lic at start-up", imported.LicenseGuid);
    }
    catch (Exception ex)
    {
        services.GetRequiredService<ILogger<Program>>().LogWarning(ex, "MedyxHMS.lic in the application folder could not be imported at start-up");
    }

    // Patient records: every OPD visit and IPD admission has its medical record (also for data added by seeds or imports).
    try
    {
        await services.GetRequiredService<IMedicalRecordService>().EnsureRecordsAsync();
    }
    catch (Exception ex)
    {
        services.GetRequiredService<ILogger<Program>>().LogError(ex, "Could not create missing medical records at start-up");
    }

    // Security hardening: apply the configured sign-in timeout.
    var securityPolicy = services.GetRequiredService<ISecurityPolicyService>();
    securityPolicy.ApplySessionTimeout((await securityPolicy.GetPolicyAsync()).SessionTimeoutMinutes);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Friendly pages for empty 4xx/5xx responses (e.g. 404, 429) on browser page requests.
// API and AJAX/JSON calls are left alone so their callers still get the bare status code.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api")
               && context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase),
    branch => branch.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}"));

// Enhanced Security Middleware (STEP 5.5 - Enhanced Security)
app.UseEnhancedSecurity();

app.UseStaticFiles();

app.UseRouting();

// Enable CORS
app.UseCors("AllowConfiguredOrigins");

// Enable Response Caching (must be before authentication/authorization)
app.UseResponseCaching();

// Enable Session
app.UseSession();

// Authentication must run before license/module checks so policies can inspect user roles/claims.
app.UseAuthentication();
// Multi-hospital: select the hospital this staff request works in (used by query filters and the switcher).
app.UseMiddleware<HospitalContextMiddleware>();
// Money is shown in the hospital's currency (Print Settings → currency symbol) on every page.
app.UseMiddleware<HospitalCurrencyMiddleware>();
// Security hardening: admins must set up two-step login before using the system (Security & Backups policy).
app.UseMiddleware<AdminMfaEnforcementMiddleware>();
// License expiration is enforced before per-module entitlement checks.
app.UseMiddleware<LicenseEnforcementMiddleware>();
// Entitlement check validates both admin toggles and license module availability.
app.UseMiddleware<ModuleEntitlementMiddleware>();
app.UseAuthorization();

// Health Check endpoint
app.MapHealthChecks("/health");

// Map controller routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

