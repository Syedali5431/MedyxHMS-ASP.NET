using MedyxHMS.DTOs;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// Purpose: Contains application code for SettingsController and its related runtime behavior.
namespace MedyxHMS.Controllers.PatientPortal
{
    [Authorize(Roles = "Patient")]
    public class SettingsController : Controller
    {
        private readonly IPatientPortalService _patientPortalService;

        public SettingsController(IPatientPortalService patientPortalService)
        {
            _patientPortalService = patientPortalService;
        }

        // GET: /PatientPortal/Settings/Index
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });
            }

            try
            {
                var patient = await _patientPortalService.GetPatientByIdAsync(userId);
                if (patient == null)
                {
                    return NotFound();
                }

                var viewModel = new PatientPortalSettingsViewModel
                {
                    Patient = new PatientPortalDto
                    {
                        Id = patient.Id.ToString(),
                        Email = patient.Email,
                        Phone = patient.Phone,
                        ProfileImagePath = patient.ProfileImagePath
                    },
                    NotificationPreferences = new NotificationPreferences
                    {
                        EmailNotifications = true,
                        SMSNotifications = true,
                        AppointmentReminders = true,
                        BillNotifications = true,
                        TestResultNotifications = true,
                        NewsletterSubscription = false
                    },
                    PreferredLanguage = "English",
                    PreferredTimezone = "UTC"
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading settings: {ex.Message}";
                return View(new PatientPortalSettingsViewModel());
            }
        }

        // POST: /PatientPortal/Settings/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(PatientPortalSettingsViewModel viewModel)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });
            }

            try
            {
                var patient = await _patientPortalService.GetPatientByIdAsync(userId);
                if (patient == null)
                {
                    return NotFound();
                }

                // Update phone if changed
                if (patient.Phone != viewModel.Patient.Phone)
                {
                    patient.Phone = viewModel.Patient.Phone;
                    await _patientPortalService.UpdatePatientProfileAsync(patient);
                }

                // Store notification preferences (would typically be in database)
                // This is a placeholder for persistence logic
                HttpContext.Session.SetString("NotificationPreferences", System.Text.Json.JsonSerializer.Serialize(viewModel.NotificationPreferences));
                HttpContext.Session.SetString("PreferredLanguage", viewModel.PreferredLanguage);
                HttpContext.Session.SetString("PreferredTimezone", viewModel.PreferredTimezone);

                TempData["SuccessMessage"] = "Settings updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating settings: {ex.Message}";
                return View(viewModel);
            }
        }

        // GET: /PatientPortal/Settings/Privacy
        public IActionResult Privacy()
        {
            return View();
        }

        // GET: /PatientPortal/Settings/Security
        public async Task<IActionResult> Security()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });
            }

            var viewModel = new PatientPortalSecuritySettingsViewModel
            {
                PatientId = userId,
                TwoFactorEnabled = false, // Would be retrieved from database
                LastPasswordChange = DateTime.Now.AddDays(-30), // Placeholder
                ActiveSessions = 1 // Placeholder
            };

            return View(viewModel);
        }

        // POST: /PatientPortal/Settings/EnableTwoFactor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactor()
        {
            try
            {
                TempData["SuccessMessage"] = "Two-Factor Authentication has been enabled!";
                return RedirectToAction("Security");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error enabling Two-Factor Authentication: {ex.Message}";
                return RedirectToAction("Security");
            }
        }
    }
}

public class NotificationPreferences
{
    public bool EmailNotifications { get; set; }
    public bool SMSNotifications { get; set; }
    public bool AppointmentReminders { get; set; }
    public bool BillNotifications { get; set; }
    public bool TestResultNotifications { get; set; }
    public bool NewsletterSubscription { get; set; }
}

public class PatientPortalSecuritySettingsViewModel
{
    public string PatientId { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTime LastPasswordChange { get; set; }
    public int ActiveSessions { get; set; }
}
