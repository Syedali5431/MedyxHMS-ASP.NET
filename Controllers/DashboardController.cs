using MedyxHMS.Controllers;
using MedyxHMS.Extensions;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedyxHMS.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ISettingService _settingService;
        private readonly IPatientService _patientService;
        private readonly IAppointmentService _appointmentService;
        private readonly IBillingService _billingService;
        private readonly MedyxHMS.Services.Interfaces.IAuthorizationService _authorizationService;

        public DashboardController(
            ISettingService settingService,
            IPatientService patientService,
            IAppointmentService appointmentService,
            IBillingService billingService,
            MedyxHMS.Services.Interfaces.IAuthorizationService authorizationService)
        {
            _settingService = settingService;
            _patientService = patientService;
            _appointmentService = appointmentService;
            _billingService = billingService;
            _authorizationService = authorizationService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get user permissions
            var permissions = await _authorizationService.GetUserPermissionsAsync(userId);
            var roles = await _authorizationService.GetUserRolesAsync(userId);

            // Build dashboard view model based on permissions
            var dashboardViewModel = new DashboardViewModel
            {
                HospitalSettings = await _settingService.GetHospitalSettingsAsync(),
                FeatureToggles = await _settingService.GetFeatureTogglesAsync(),
                UserPermissions = permissions.ToList(),
                UserRoles = roles.ToList()
            };

            try
            {
                // Load statistics based on permissions
                if (permissions.Contains("ViewPatients"))
                {
                    dashboardViewModel.TotalPatients = (await _patientService.GetAllPatientsAsync()).Count();
                }

                if (permissions.Contains("ViewAppointments"))
                {
                    dashboardViewModel.TodayAppointments = (await _appointmentService.GetAppointmentsByDateAsync(DateTime.Today)).Count();
                    dashboardViewModel.RecentAppointments = (await _appointmentService.GetAllAppointmentsAsync())
                        .OrderByDescending(a => a.CreatedDate)
                        .Take(5)
                        .ToList();
                }

                if (permissions.Contains("ViewBills"))
                {
                    // Get billing summary for current month
                    var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    dashboardViewModel.MonthlyRevenue = await _billingService.GetTotalRevenueAsync(startOfMonth, DateTime.Today);

                    // Get pending bills
                    var allBills = await _billingService.GetAllBillsAsync();
                    dashboardViewModel.PendingBills = allBills.Count(b => b.Status == "Unpaid" || b.Status == "Partially Paid");
                }
            }
            catch (Exception ex)
            {
                // Log error and show basic dashboard
                ViewBag.Error = "Some dashboard data could not be loaded. Please check database connection.";
            }

            return View(dashboardViewModel);
        }

        [PermissionAuthorize("ManageUsers")]
        public IActionResult Admin()
        {
            return View();
        }

        [PermissionAuthorize("ViewPatients")]
        public IActionResult Patient()
        {
            return View();
        }

        [PermissionAuthorize("ViewAppointments")]
        public IActionResult Appointment()
        {
            return View();
        }

        [PermissionAuthorize("ViewBills")]
        public IActionResult Billing()
        {
            return View();
        }
    }

    public class DashboardViewModel
    {
        public HospitalSettings HospitalSettings { get; set; }
        public FeatureToggles FeatureToggles { get; set; }
        public int TotalPatients { get; set; }
        public int TodayAppointments { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int PendingBills { get; set; }
        public IEnumerable<Appointment> RecentAppointments { get; set; }
        public List<string> UserPermissions { get; set; } = new List<string>();
        public List<string> UserRoles { get; set; } = new List<string>();
    }
}