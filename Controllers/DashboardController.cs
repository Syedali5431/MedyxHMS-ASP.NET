using MedyxHMS.Controllers;
using MedyxHMS.Extensions;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// Purpose: Contains application code for DashboardController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize]
    public partial class DashboardController : Controller
    {
        private readonly ISettingService _settingService;
        private readonly IPatientService _patientService;
        private readonly IAppointmentService _appointmentService;
        private readonly IBillingService _billingService;
        private readonly IModuleService _moduleService;
        private readonly ILicenseService _licenseService;
        private readonly MedyxHMS.Services.Interfaces.IAuthorizationService _authorizationService;

        public DashboardController(
            ISettingService settingService,
            IPatientService patientService,
            IAppointmentService appointmentService,
            IBillingService billingService,
            IModuleService moduleService,
            ILicenseService licenseService,
            MedyxHMS.Services.Interfaces.IAuthorizationService authorizationService)
        {
            _settingService = settingService;
            _patientService = patientService;
            _appointmentService = appointmentService;
            _billingService = billingService;
            _moduleService = moduleService;
            _licenseService = licenseService;
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
            var isSuperAdmin = roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase);

            var moduleMap = await _moduleService.GetUserModuleMapAsync(userId, isSuperAdmin);
            var registeredModules = await _moduleService.GetAllModulesAsync();

            // Build dashboard view model based on permissions
            var dashboardViewModel = new DashboardViewModel
            {
                HospitalSettings = await _settingService.GetHospitalSettingsAsync(),
                FeatureToggles = await _settingService.GetFeatureTogglesAsync(),
                UserPermissions = permissions.ToList(),
                UserRoles = roles.ToList(),
                ModuleExplorer = await BuildModuleExplorerAsync(registeredModules, moduleMap, roles.ToList(), isSuperAdmin)
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
            catch (Exception)
            {
                // Log error and show basic dashboard
                ViewBag.Error = "Some dashboard data could not be loaded. Please check database connection.";
            }

            // Build chart data: last 7 days
            try
            {
                var today = DateTime.Today;
                var labels = Enumerable.Range(6, 7).Select(i => today.AddDays(-(6 - i))).ToList();
                var labelStrings = labels.Select(d => d.ToString("ddd dd MMM")).ToArray();

                // Appointments per day
                var allAppts = await _appointmentService.GetAllAppointmentsAsync();
                var apptByDay = allAppts.GroupBy(a => a.AppointmentDate.Date)
                    .ToDictionary(g => g.Key, g => g.Count());
                var apptCounts = labels.Select(d => apptByDay.TryGetValue(d, out var c) ? c : 0).ToArray();

                // Revenue per day
                var allBillsList = await _billingService.GetAllBillsAsync();
                var revenueByDay = allBillsList.GroupBy(b => b.BillDate.Date)
                    .ToDictionary(g => g.Key, g => (double)g.Sum(b => b.TotalAmount));
                var revenueCounts = labels.Select(d => revenueByDay.TryGetValue(d, out var r) ? r : 0.0).ToArray();

                ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(labelStrings);
                ViewBag.ApptChartData = System.Text.Json.JsonSerializer.Serialize(apptCounts);
                ViewBag.RevenueChartData = System.Text.Json.JsonSerializer.Serialize(revenueCounts);
            }
            catch
            {
                ViewBag.ChartLabels = "[]";
                ViewBag.ApptChartData = "[]";
                ViewBag.RevenueChartData = "[]";
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
        public List<DashboardModuleNavGroup> ModuleExplorer { get; set; } = new();
    }

    public class DashboardModuleNavGroup
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "fas fa-folder";
        public List<DashboardModuleNavItem> Items { get; set; } = new();
    }

    public class DashboardModuleNavItem
    {
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = "Index";
        public string? Area { get; set; }
    }

    public partial class DashboardController
    {
        private async Task<List<DashboardModuleNavGroup>> BuildModuleExplorerAsync(
            IReadOnlyList<SystemModule> modules,
            Dictionary<string, bool> moduleMap,
            IReadOnlyCollection<string> roles,
            bool isSuperAdmin)
        {
            var result = new List<DashboardModuleNavGroup>();
            foreach (var module in modules.OrderBy(m => m.SortOrder).ThenBy(m => m.DisplayName))
            {
                if (!moduleMap.TryGetValue(module.Key, out var isEnabled) || !isEnabled)
                    continue;

                if (module.Key.Equals("License", StringComparison.OrdinalIgnoreCase) && !isSuperAdmin)
                    continue;

                var isLicensed = isSuperAdmin || await _licenseService.IsModuleLicensedForCurrentLicenseAsync(module.Key);
                if (!isLicensed && !module.Key.Equals("License", StringComparison.OrdinalIgnoreCase))
                    continue;

                var options = FilterInaccessibleOptions(GetModuleOptions(module.Key), roles, isSuperAdmin);
                if (options.Count == 0)
                    continue;

                result.Add(new DashboardModuleNavGroup
                {
                    Key = module.Key,
                    Title = module.DisplayName,
                    Description = module.Description ?? string.Empty,
                    Icon = string.IsNullOrWhiteSpace(module.Icon) ? "fas fa-folder" : module.Icon,
                    Items = options
                });
            }

            return result;
        }

        private static List<DashboardModuleNavItem> FilterInaccessibleOptions(
            List<DashboardModuleNavItem> options,
            IReadOnlyCollection<string> roles,
            bool isSuperAdmin)
        {
            if (isSuperAdmin)
                return options;

            var isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
            var canManageBeds = isAdmin || roles.Contains("Nurse", StringComparer.OrdinalIgnoreCase);

            return options.Where(item =>
            {
                // Keep staff portal navigation actionable by hiding admin-only operations.
                if (item.Controller.Equals("Payroll", StringComparison.OrdinalIgnoreCase) &&
                    item.Action.Equals("Generate", StringComparison.OrdinalIgnoreCase))
                {
                    return isAdmin;
                }

                if (item.Controller.Equals("Leave", StringComparison.OrdinalIgnoreCase) &&
                    (item.Action.Equals("Types", StringComparison.OrdinalIgnoreCase) ||
                     item.Action.Equals("Balances", StringComparison.OrdinalIgnoreCase)))
                {
                    return isAdmin;
                }

                if (item.Controller.Equals("Certificate", StringComparison.OrdinalIgnoreCase) &&
                    (item.Action.Equals("GenerateCertificate", StringComparison.OrdinalIgnoreCase) ||
                     item.Action.Equals("GenerateIdCard", StringComparison.OrdinalIgnoreCase)))
                {
                    return isAdmin;
                }

                if (item.Controller.Equals("Report", StringComparison.OrdinalIgnoreCase) &&
                    item.Action.Equals("Builder", StringComparison.OrdinalIgnoreCase))
                {
                    return isAdmin;
                }

                if (item.Controller.Equals("ChatbotAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    return isAdmin;
                }

                if (item.Controller.Equals("Messaging", StringComparison.OrdinalIgnoreCase) &&
                    item.Action.Equals("Broadcast", StringComparison.OrdinalIgnoreCase))
                {
                    return isAdmin;
                }

                if (item.Controller.Equals("Inventory", StringComparison.OrdinalIgnoreCase) &&
                    item.Action.Equals("Create", StringComparison.OrdinalIgnoreCase))
                {
                    return isAdmin;
                }

                if (item.Controller.Equals("DownloadCenter", StringComparison.OrdinalIgnoreCase) &&
                    item.Action.Equals("Upload", StringComparison.OrdinalIgnoreCase))
                {
                    return isAdmin;
                }

                if (item.Controller.Equals("BedManagement", StringComparison.OrdinalIgnoreCase) &&
                    (item.Label.Equals("Assign Bed", StringComparison.OrdinalIgnoreCase) ||
                     item.Label.Equals("Update Status", StringComparison.OrdinalIgnoreCase)))
                {
                    return canManageBeds;
                }

                return true;
            }).ToList();
        }

        private static List<DashboardModuleNavItem> GetModuleOptions(string moduleKey)
        {
            var key = (moduleKey ?? string.Empty).Trim();

            return key.ToUpperInvariant() switch
            {
                "DASHBOARD" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Dashboard Overview", Description = "Main KPI overview and recent activity.", Controller = "Dashboard", Action = "Index" }
                },
                "PATIENT" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Patient List", Description = "Search and manage registered patients.", Controller = "Patient", Action = "Index" },
                    new() { Label = "Add Patient", Description = "Register a new patient.", Controller = "Patient", Action = "Create" }
                },
                "APPOINTMENT" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Appointments", Description = "Browse scheduled appointments.", Controller = "Appointment", Action = "Index" },
                    new() { Label = "Appointment Calendar", Description = "Calendar view for appointment planning.", Controller = "Appointment", Action = "Calendar" },
                    new() { Label = "Schedule Appointment", Description = "Create a new appointment.", Controller = "Appointment", Action = "Create" }
                },
                "OPD" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "OPD Visits", Description = "Outpatient records and consultations.", Controller = "OPD", Action = "Index" },
                    new() { Label = "Add OPD Visit", Description = "Create a new OPD visit.", Controller = "OPD", Action = "Create" }
                },
                "IPD" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "IPD Admissions", Description = "Inpatient admission management.", Controller = "IPD", Action = "Index" },
                    new() { Label = "New Admission", Description = "Admit a patient to IPD.", Controller = "IPD", Action = "Create" }
                },
                "BILLING" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Billing Dashboard", Description = "Invoices and payment status.", Controller = "Billing", Action = "Index" },
                    new() { Label = "Create Bill", Description = "Generate a new patient bill.", Controller = "Billing", Action = "Create" }
                },
                "PRESCRIPTION" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Prescriptions", Description = "Manage prescriptions.", Controller = "Prescription", Action = "Index" },
                    new() { Label = "Medicines", Description = "Medicine catalog and stock.", Controller = "Prescription", Action = "Medicines" }
                },
                "LAB" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Lab Tests", Description = "Lab test catalog and orders.", Controller = "Lab", Action = "Index" },
                    new() { Label = "Lab Results", Description = "Track test result workflow.", Controller = "Lab", Action = "Results" }
                },
                "RADIOLOGY" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Radiology Tests", Description = "Radiology catalog and orders.", Controller = "Radiology", Action = "Index" },
                    new() { Label = "Radiology Results", Description = "Manage radiology reports.", Controller = "Radiology", Action = "Results" }
                },
                "BLOODBANK" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Blood Inventory", Description = "Blood stock and issue tracking.", Controller = "BloodBank", Action = "Index" },
                    new() { Label = "Issue Blood", Description = "Issue blood units to patients.", Controller = "BloodBank", Action = "Issue" }
                },
                "OPERATIONTHEATRE" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "OT Schedule", Description = "Operation theatre schedules.", Controller = "OperationTheatre", Action = "Index" },
                    new() { Label = "Add OT Case", Description = "Create operation theatre schedule entry.", Controller = "OperationTheatre", Action = "Create" }
                },
                "FRONTOFFICE" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Front Office Home", Description = "Visitors, complaints, and dispatch.", Controller = "FrontOffice", Action = "Index" },
                    new() { Label = "Visitors", Description = "Manage visitor logs.", Controller = "FrontOffice", Action = "Visitors" },
                    new() { Label = "Complaints", Description = "Manage complaint records.", Controller = "FrontOffice", Action = "Complaints" }
                },
                "ATTENDANCE" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Attendance", Description = "Staff attendance records.", Controller = "Attendance", Action = "Index" },
                    new() { Label = "Check In", Description = "Record check-in.", Controller = "Attendance", Action = "CheckIn" },
                    new() { Label = "Check Out", Description = "Record check-out.", Controller = "Attendance", Action = "CheckOut" }
                },
                "LEAVE" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Leave Requests", Description = "Manage leave workflow.", Controller = "Leave", Action = "Index" },
                    new() { Label = "Leave Types", Description = "Configure leave types.", Controller = "Leave", Action = "Types" },
                    new() { Label = "Leave Balances", Description = "Track leave balances.", Controller = "Leave", Action = "Balances" }
                },
                "PAYROLL" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Payroll Records", Description = "Payroll entries by month.", Controller = "Payroll", Action = "Index" },
                    new() { Label = "Generate Payroll", Description = "Generate payroll for staff.", Controller = "Payroll", Action = "Generate" }
                },
                "CERTIFICATE" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Certificates", Description = "Issue and manage certificates.", Controller = "Certificate", Action = "Index" },
                    new() { Label = "Generate Certificate", Description = "Create printable certificate.", Controller = "Certificate", Action = "GenerateCertificate" },
                    new() { Label = "Generate ID Card", Description = "Create staff ID cards.", Controller = "Certificate", Action = "GenerateIdCard" }
                },
                "REFERRAL" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Referrals", Description = "Manage referral lifecycle.", Controller = "Referral", Action = "Index" },
                    new() { Label = "Create Referral", Description = "Create a patient referral.", Controller = "Referral", Action = "Create" }
                },
                "REPORT" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Reports Dashboard", Description = "Open consolidated report hub.", Controller = "Report", Action = "Index" },
                    new() { Label = "Department Report", Description = "Department-wise analytics.", Controller = "Report", Action = "DepartmentReport" },
                    new() { Label = "Financial Report", Description = "Revenue and finance analysis.", Controller = "Report", Action = "FinancialReport" },
                    new() { Label = "Occupancy Report", Description = "Bed and ward occupancy analytics.", Controller = "Report", Action = "OccupancyReport" },
                    new() { Label = "Staff Report", Description = "Attendance and staffing trends.", Controller = "Report", Action = "StaffReport" },
                    new() { Label = "Payroll Report", Description = "Payroll summary and trends.", Controller = "Report", Action = "PayrollReport" },
                    new() { Label = "Generated Reports", Description = "Review generated report history.", Controller = "Report", Action = "GeneratedReports" },
                    new() { Label = "Report Builder", Description = "Create and edit report templates.", Controller = "Report", Action = "Builder" }
                },
                "PATIENTPORTAL" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Public Site", Description = "Patient-facing website and portal landing.", Controller = "Site", Action = "Index" }
                },
                "AMBULANCE" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Ambulance Vehicles", Description = "Manage ambulance fleet and availability.", Controller = "Ambulance", Action = "Index" },
                    new() { Label = "Dispatch Log", Description = "Track ambulance dispatches and returns.", Controller = "Ambulance", Action = "Dispatches" },
                    new() { Label = "New Dispatch", Description = "Dispatch an available ambulance.", Controller = "Ambulance", Action = "Dispatch" }
                },
                "BIRTHDEATH" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Birth Records", Description = "View and issue birth records.", Controller = "BirthDeath", Action = "Index" },
                    new() { Label = "Death Records", Description = "View and issue death records.", Controller = "BirthDeath", Action = "Deaths" },
                    new() { Label = "New Birth Record", Description = "Create a birth record.", Controller = "BirthDeath", Action = "CreateBirth" },
                    new() { Label = "New Death Record", Description = "Create a death record.", Controller = "BirthDeath", Action = "CreateDeath" }
                },
                "TPA" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "TPA Providers", Description = "Manage third party administrator providers.", Controller = "Tpa", Action = "Index" },
                    new() { Label = "TPA Claims", Description = "Track insurance and TPA claims.", Controller = "Tpa", Action = "Claims" },
                    new() { Label = "New Claim", Description = "Create a new TPA claim.", Controller = "Tpa", Action = "CreateClaim" }
                },
                "MESSAGING" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Inbox", Description = "Review received internal messages.", Controller = "Messaging", Action = "Index" },
                    new() { Label = "Sent Messages", Description = "Review sent messages.", Controller = "Messaging", Action = "Sent" },
                    new() { Label = "Compose Message", Description = "Send an internal message.", Controller = "Messaging", Action = "Compose" },
                    new() { Label = "Broadcast", Description = "Send a message to all staff.", Controller = "Messaging", Action = "Broadcast" }
                },
                "INVENTORY" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Inventory Items", Description = "Browse inventory stock and suppliers.", Controller = "Inventory", Action = "Index" },
                    new() { Label = "Add Item", Description = "Register a new inventory item.", Controller = "Inventory", Action = "Create" },
                    new() { Label = "Transactions", Description = "Review stock movement history.", Controller = "Inventory", Action = "Transactions" },
                    new() { Label = "Low Stock", Description = "See items that need reordering.", Controller = "Inventory", Action = "LowStock" }
                },
                "DOWNLOADCENTER" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Download Center", Description = "Browse shared staff documents.", Controller = "DownloadCenter", Action = "Index" },
                    new() { Label = "Upload File", Description = "Upload a staff document.", Controller = "DownloadCenter", Action = "Upload" }
                },
                "LIVECONSULTATION" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Consultations", Description = "Review scheduled live consultation sessions.", Controller = "LiveConsultation", Action = "Index" },
                    new() { Label = "Schedule Session", Description = "Schedule a new live consultation.", Controller = "LiveConsultation", Action = "Schedule" }
                },
                "BEDMANAGEMENT" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Bed Overview", Description = "View all beds, statuses and occupancy.", Controller = "BedManagement", Action = "Index" },
                    new() { Label = "Assign Bed", Description = "Assign an available bed to a patient.", Controller = "BedManagement", Action = "Index" },
                    new() { Label = "Update Status", Description = "Set bed to Available, Cleaning or Maintenance.", Controller = "BedManagement", Action = "Index" }
                },
                "CHATBOT" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "Chatbot", Description = "Open AI assistant interface.", Controller = "Chatbot", Action = "Index" },
                    new() { Label = "Chatbot Admin", Description = "Configure chatbot settings.", Controller = "ChatbotAdmin", Action = "Settings" }
                },
                "CMS" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "CMS Pages", Description = "Manage public website pages.", Controller = "Cms", Action = "Index" },
                    new() { Label = "CMS Notices", Description = "Manage notices and alerts.", Controller = "Cms", Action = "Notices" },
                    new() { Label = "Public Site Settings", Description = "Configure public website settings.", Controller = "PublicSiteAdmin", Action = "Settings" }
                },
                "LICENSE" => new List<DashboardModuleNavItem>
                {
                    new() { Label = "License Management", Description = "Manage license lifecycle and keys.", Controller = "License", Action = "Index" }
                },
                _ => new List<DashboardModuleNavItem>()
            };
        }
    }
}
