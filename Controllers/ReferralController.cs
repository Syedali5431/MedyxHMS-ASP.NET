using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Staff,Doctor")]
    public class ReferralController : Controller
    {
        private readonly IReferralService _referralService;
        private readonly IPatientService _patientService;
        private readonly IAuditService _auditService;

        public ReferralController(IReferralService referralService, IPatientService patientService, IAuditService auditService)
        {
            _referralService = referralService;
            _patientService = patientService;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var referrals = await _referralService.GetReferralsAsync();
            return View(referrals);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Patients = await _patientService.GetAllPatientsAsync();
            return View(new Referral { ReferralDate = DateTime.Now, Status = "Pending", ReferralType = "External" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Referral model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Patients = await _patientService.GetAllPatientsAsync();
                return View(model);
            }

            try
            {
                var created = await _referralService.CreateReferralAsync(model);
                await _auditService.LogActivityAsync(
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    "CREATE",
                    "Referral",
                    created.Id.ToString(),
                    null,
                    $"Patient: {created.PatientId}, Type: {created.ReferralType}, ReferredTo: {created.ReferredTo}");

                TempData["SuccessMessage"] = "Referral saved successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.Patients = await _patientService.GetAllPatientsAsync();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var updated = await _referralService.UpdateStatusAsync(id, status);
            TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
                ? "Referral status updated."
                : "Referral not found.";
            return RedirectToAction(nameof(Index));
        }
    }
}
