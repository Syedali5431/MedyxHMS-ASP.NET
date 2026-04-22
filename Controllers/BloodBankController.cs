using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// Purpose: Contains application code for BloodBankController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Staff,Doctor,Nurse")]
    public class BloodBankController : Controller
    {
        private readonly IBloodBankService _bloodBankService;
        private readonly IPatientService _patientService;
        private readonly IAuditService _auditService;

        public BloodBankController(IBloodBankService bloodBankService, IPatientService patientService, IAuditService auditService)
        {
            _bloodBankService = bloodBankService;
            _patientService = patientService;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Issues = await _bloodBankService.GetBloodIssuesAsync();
            var inventory = await _bloodBankService.GetBloodInventoryAsync();
            return View(inventory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInventory(string bloodGroup, int unitsAvailable, int minimumLevel = 5)
        {
            try
            {
                await _bloodBankService.UpsertInventoryAsync(bloodGroup, unitsAvailable, minimumLevel);
                TempData["SuccessMessage"] = $"Inventory updated for {bloodGroup}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Issue()
        {
            ViewBag.Patients = await _patientService.GetAllPatientsAsync();
            ViewBag.Inventory = await _bloodBankService.GetBloodInventoryAsync();
            return View(new BloodIssue());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Issue(BloodIssue model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Patients = await _patientService.GetAllPatientsAsync();
                ViewBag.Inventory = await _bloodBankService.GetBloodInventoryAsync();
                return View(model);
            }

            try
            {
                var created = await _bloodBankService.IssueBloodAsync(model);
                await _auditService.LogActivityAsync(
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    "CREATE",
                    "BloodIssue",
                    created.Id.ToString(),
                    null,
                    $"Patient: {created.PatientId}, Group: {created.BloodGroup}, Units: {created.UnitsIssued}");

                TempData["SuccessMessage"] = "Blood issued and billing entry created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.Patients = await _patientService.GetAllPatientsAsync();
                ViewBag.Inventory = await _bloodBankService.GetBloodInventoryAsync();
                return View(model);
            }
        }
    }
}
