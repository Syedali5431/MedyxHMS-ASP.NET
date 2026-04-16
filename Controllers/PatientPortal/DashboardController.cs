using MedyxHMS.DTOs;
using MedyxHMS.Extensions;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedyxHMS.Controllers.PatientPortal
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IPatientPortalService _patientPortalService;

        public DashboardController(IPatientPortalService patientPortalService)
        {
            _patientPortalService = patientPortalService;
        }

        // GET: /PatientPortal/Dashboard/Index
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var patient = await _patientPortalService.GetPatientByIdAsync(userId);
                if (patient == null)
                {
                    return NotFound();
                }

                var dashboardData = await _patientPortalService.GetPatientDashboardDataAsync(userId);

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
                var appointments = await _patientPortalService.GetPatientAppointmentsAsync(userId, "upcoming");
                viewModel.Dashboard.RecentAppointments = appointments.Take(5).Select(a => new PatientPortalAppointmentDto
                {
                    Id = a.Id.ToString(),
                    AppointmentId = a.AppointmentId,
                    AppointmentDate = a.AppointmentDate,
                    DoctorName = $"{a.Staff?.FirstName} {a.Staff?.LastName}",
                    Department = a.Staff?.Department,
                    Status = a.Status,
                    Symptoms = a.Symptoms
                }).ToList();

                // Get recent bills
                var bills = await _patientPortalService.GetPatientBillsAsync(userId);
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
                return RedirectToAction("Login", "Account");
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
                return RedirectToAction("Login", "Account");
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
                return RedirectToAction("Login", "Account");
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
                return RedirectToAction("Login", "Account");
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
                        return RedirectToAction("Dashboard", "Index");
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
    }
}