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
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// Configure Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IPatientPortalService, PatientPortalService>();
builder.Services.AddScoped<IEmailNotificationProvider, SmtpEmailNotificationProvider>();
builder.Services.AddScoped<ISmsNotificationProvider, TwilioSmsNotificationProvider>();
builder.Services.AddScoped<IPublicBookingNotificationService, PublicBookingNotificationService>();
builder.Services.AddScoped<INotificationDeliveryAuditService, NotificationDeliveryAuditService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<IChatbotModerationService, ChatbotModerationService>();
builder.Services.AddScoped<IChatbotPromptBuilder, ChatbotPromptBuilder>();
builder.Services.AddScoped<IChatbotService, OpenAiChatbotService>();
builder.Services.AddScoped<ISmtpHealthService, SmtpHealthService>();
builder.Services.AddHostedService<LicenseReminderHostedService>();

// Clinical Module Services (STEP 3.1)
builder.Services.AddScoped<IOPDService, OPDService>();
builder.Services.AddScoped<IIPDService, IPDService>();
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

builder.Services.AddScoped<DatabaseInitializer>();

// Add HttpContext accessor for audit logging
builder.Services.AddHttpContextAccessor();

// Configure Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
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

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var initializer = services.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

// Enable CORS
app.UseCors("AllowAll");

// Enable Session
app.UseSession();

app.UseAuthentication();
app.UseMiddleware<LicenseEnforcementMiddleware>();
app.UseAuthorization();

// Enable Response Caching
app.UseResponseCaching();

// Health Check endpoint
app.MapHealthChecks("/health");

// Map controller routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
