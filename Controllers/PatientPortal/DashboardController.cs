using MedyxHMS.DTOs;
using MedyxHMS.Extensions;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// Purpose: Contains application code for DashboardController and its related runtime behavior.
namespace MedyxHMS.Controllers.PatientPortal
{
    [Area("PatientPortal")]
    [Authorize(Roles = "Patient")]
    [Route("PatientPortal/[controller]/[action]")]
    public class DashboardController : Controller
    {
        private readonly IPatientPortalService _patientPortalService;
        private readonly IExportService _exportService;

        public DashboardController(IPatientPortalService patientPortalService, IExportService exportService)
        {
            _patientPortalService = patientPortalService;
            _exportService = exportService;
        }

        // GET: /PatientPortal/Dashboard and /PatientPortal/Dashboard/Index
        [HttpGet("/PatientPortal/Dashboard")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return LocalRedirect("/PatientPortal/Account/Login");
            }

            try
            {
                var patient = await _patientPortalService.GetPatientByIdAsync(userId);
                if (patient == null)
                {
                    return NotFound();
                }

                var patientRecordId = patient.Id.ToString();

                var dashboardData = await _patientPortalService.GetPatientDashboardDataAsync(patientRecordId);

                var viewModel = new PatientPortalDashboardViewModel
                {
                    Dashboard = new PatientPortalDashboardDto
                    {
                        Patient = new PatientPortalDto
                        {
                            Id = patient.Id.ToString(),
                            PatientId = patient.PatientId,
                            FirstName = patient.FirstName,
                            LastName = patient.LastName,
                            Email = patient.Email,
                            Phone = patient.Phone,
                            DateOfBirth = patient.DateOfBirth,
                            Gender = patient.Gender,
                            BloodGroup = patient.BloodGroup,
                            Address = patient.Address,
                            IsActive = patient.IsActive,
                            CreatedDate = patient.CreatedDate,
                            LastLoginDate = patient.User?.LastLoginDate,
                            ProfileImagePath = patient.ProfileImagePath
                        },
                        UpcomingAppointments = (int)dashboardData["UpcomingAppointments"],
                        PendingBills = (int)dashboardData["PendingBills"],
                        TotalOutstandingAmount = (decimal)dashboardData["OutstandingAmount"],
                        RecentTestResults = 0 // Will be populated from database
                    },
                    WelcomeMessage = $"Welcome back, {patient.FirstName}!"
                };

                // Get recent appointments
                var appointments = await _patientPortalService.GetPatientAppointmentsAsync(patientRecordId, "upcoming");
                viewModel.Dashboard.RecentAppointments = appointments.Take(5).Select(a => new PatientPortalAppointmentDto
                {
                    Id = a.Id.ToString(),
                    AppointmentId = a.AppointmentId,
                    AppointmentDate = a.AppointmentDate,
                    DoctorName = $"{a.Staff?.FirstName} {a.Staff?.LastName}",
                    Department = a.Staff?.Department ?? string.Empty,
                    Status = a.Status,
                    Symptoms = a.Symptoms
                }).ToList();

                // Get recent bills
                var bills = await _patientPortalService.GetPatientBillsAsync(patientRecordId);
                viewModel.Dashboard.RecentBills = bills.Take(5).Select(b => new PatientPortalBillDto
                {
                    Id = b.Id.ToString(),
                    BillNumber = b.BillNumber,
                    BillDate = b.BillDate,
                    TotalAmount = b.TotalAmount,
                    PaidAmount = b.PaidAmount,
                    Status = b.Status
                }).ToList();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading dashboard: {ex.Message}";
                return View(new PatientPortalDashboardViewModel());
            }
        }

        // GET: /PatientPortal/Dashboard/Profile
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return LocalRedirect("/PatientPortal/Account/Login");
            }

            var patient = await _patientPortalService.GetPatientByIdAsync(userId);
            if (patient == null)
            {
                return NotFound();
            }

            var viewModel = new PatientPortalProfileViewModel
            {
                Patient = new PatientPortalDto
                {
                    Id = patient.Id.ToString(),
                    PatientId = patient.PatientId,
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    Email = patient.Email,
                    Phone = patient.Phone,
                    DateOfBirth = patient.DateOfBirth,
                    Gender = patient.Gender,
                    BloodGroup = patient.BloodGroup,
                    Address = patient.Address,
                    GuardianName = patient.GuardianName,
                    GuardianPhone = patient.GuardianPhone,
                    MaritalStatus = patient.MaritalStatus,
                    Occupation = patient.Occupation,
                    EmergencyContactName = patient.EmergencyContactName,
                    EmergencyContactPhone = patient.EmergencyContactPhone,
                    EmergencyContactRelation = patient.EmergencyContactRelation,
                    IsActive = patient.IsActive,
                    CreatedDate = patient.CreatedDate,
                    LastLoginDate = patient.User?.LastLoginDate,
                    ProfileImagePath = patient.ProfileImagePath
                }
            };

            return View(viewModel);
        }

        // POST: /PatientPortal/Dashboard/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(PatientPortalProfileViewModel viewModel)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return LocalRedirect("/PatientPortal/Account/Login");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var patient = await _patientPortalService.GetPatientByIdAsync(userId);
                    if (patient == null)
                    {
                        return NotFound();
                    }

                    patient.FirstName = viewModel.UpdateProfile.FirstName;
                    patient.LastName = viewModel.UpdateProfile.LastName;
                    patient.Phone = viewModel.UpdateProfile.Phone;
                    patient.DateOfBirth = viewModel.UpdateProfile.DateOfBirth;
                    patient.Gender = viewModel.UpdateProfile.Gender;
                    patient.BloodGroup = viewModel.UpdateProfile.BloodGroup;
                    patient.Address = viewModel.UpdateProfile.Address;
                    patient.GuardianName = viewModel.UpdateProfile.GuardianName;
                    patient.GuardianPhone = viewModel.UpdateProfile.GuardianPhone;
                    patient.MaritalStatus = viewModel.UpdateProfile.MaritalStatus;
                    patient.Occupation = viewModel.UpdateProfile.Occupation;
                    patient.EmergencyContactName = viewModel.UpdateProfile.EmergencyContactName;
                    patient.EmergencyContactPhone = viewModel.UpdateProfile.EmergencyContactPhone;
                    patient.EmergencyContactRelation = viewModel.UpdateProfile.EmergencyContactRelation;

                    await _patientPortalService.UpdatePatientProfileAsync(patient);

                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction("Profile");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error updating profile: {ex.Message}";
                }
            }

            return View("Profile", viewModel);
        }

        // GET: /PatientPortal/Dashboard/ChangePassword
        public IActionResult ChangePassword()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return LocalRedirect("/PatientPortal/Account/Login");
            }

            var viewModel = new PatientPortalPasswordChangeViewModel
            {
                PatientId = userId
            };

            return View(viewModel);
        }

        // POST: /PatientPortal/Dashboard/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(PatientPortalPasswordChangeViewModel viewModel)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return LocalRedirect("/PatientPortal/Account/Login");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _patientPortalService.ChangePatientPasswordAsync(
                        userId,
                        viewModel.PasswordChange.CurrentPassword,
                        viewModel.PasswordChange.NewPassword);

                    if (result)
                    {
                        TempData["SuccessMessage"] = "Password changed successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Current password is incorrect");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Error changing password: {ex.Message}");
                }
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Export(string section = "appointments", string format = "csv")
        {
            format = (format ?? "csv").Trim().ToLowerInvariant();
            section = (section ?? "appointments").Trim().ToLowerInvariant();

            if (format != "csv" && format != "pdf" && format != "excel")
                return BadRequest("Only CSV, Excel, and PDF exports are supported.");

            if (section != "appointments" && section != "bills")
                return BadRequest("Invalid dashboard section.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return LocalRedirect("/PatientPortal/Account/Login");

            var patient = await _patientPortalService.GetPatientByIdAsync(userId);
            if (patient == null)
                return NotFound();

            var patientRecordId = patient.Id.ToString();

            string[] headers;
            List<IReadOnlyList<string>> rows;
            string title;
            string filePrefix;

            if (section == "appointments")
            {
                var appointments = await _patientPortalService.GetPatientAppointmentsAsync(patientRecordId, "upcoming");
                headers = new[] { "Date", "Time", "Doctor", "Department", "Status" };
                rows = appointments.Take(5).Select(a => (IReadOnlyList<string>)new[]
                {
                    a.AppointmentDate.ToString("yyyy-MM-dd"),
                    a.AppointmentDate.ToString("HH:mm"),
                    (a.Staff != null ? ($"{a.Staff.FirstName} {a.Staff.LastName}").Trim() : string.Empty),
                    a.Staff?.Department ?? string.Empty,
                    a.Status ?? string.Empty
                }).ToList();
                title = "Patient Dashboard - Upcoming Appointments";
                filePrefix = "dashboard_appointments";
            }
            else
            {
                var bills = await _patientPortalService.GetPatientBillsAsync(patientRecordId);
                headers = new[] { "Bill #", "Date", "Total", "Paid", "Status" };
                rows = bills.Take(5).Select(b => (IReadOnlyList<string>)new[]
                {
                    b.BillNumber ?? string.Empty,
                    b.BillDate.ToString("yyyy-MM-dd"),
                    b.TotalAmount.ToString("0.00"),
                    b.PaidAmount.ToString("0.00"),
                    b.Status ?? string.Empty
                }).ToList();
                title = "Patient Dashboard - Recent Bills";
                filePrefix = "dashboard_bills";
            }

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            if (format == "excel")
            {
                var bytes = _exportService.BuildExcel(title, headers, rows);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{filePrefix}_{stamp}.xlsx");
            }

            if (format == "csv")
            {
                var bytes = _exportService.BuildCsv(title, headers, rows);
                return File(bytes, "text/csv", $"{filePrefix}_{stamp}.csv");
            }

            var pdfBytes = _exportService.BuildPdfTable(title, headers, rows);
            return File(pdfBytes, "application/pdf", $"{filePrefix}_{stamp}.pdf");
        }
    }
}
