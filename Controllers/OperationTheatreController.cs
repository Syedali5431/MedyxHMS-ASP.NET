using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// Purpose: Contains application code for OperationTheatreController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Staff,Doctor,Nurse")]
    public class OperationTheatreController : Controller
    {
        private readonly IOperationTheatreService _operationTheatreService;
        private readonly IPatientService _patientService;
        private readonly IAuditService _auditService;

        public OperationTheatreController(IOperationTheatreService operationTheatreService, IPatientService patientService, IAuditService auditService)
        {
            _operationTheatreService = operationTheatreService;
            _patientService = patientService;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var schedules = await _operationTheatreService.GetSchedulesAsync();
            return View(schedules);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Patients = await _patientService.GetAllPatientsAsync();
            return View(new OTSchedule { ScheduledDate = DateTime.Now.AddHours(1), Status = "Scheduled" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OTSchedule model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Patients = await _patientService.GetAllPatientsAsync();
                return View(model);
            }

            try
            {
                var created = await _operationTheatreService.CreateScheduleAsync(model);
                await _auditService.LogActivityAsync(
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    "CREATE",
                    "OTSchedule",
                    created.Id.ToString(),
                    null,
                    $"Patient: {created.PatientId}, Procedure: {created.ProcedureName}, Date: {created.ScheduledDate:O}");

                TempData["SuccessMessage"] = "OT booking created and billing entry generated.";
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
            var updated = await _operationTheatreService.UpdateStatusAsync(id, status);
            TempData[updated ? "SuccessMessage" : "ErrorMessage"] = updated
                ? "OT schedule status updated."
                : "OT schedule not found.";
            return RedirectToAction(nameof(Index));
        }
    }
}
