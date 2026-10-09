using System.Security.Claims;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

// Purpose: Discharge Patient (doctors, nurses, administrators) and the Discharge Reports of all patients:
// create on discharge, complete, view, print and download as PDF.
namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Doctor,Nurse")]
    public class DischargeController : Controller
    {
        /// <summary>Who may discharge patients and see every patient's discharge report.</summary>
        public const string AllowedRoles = "Admin,SuperAdmin,Doctor,Nurse";

        private readonly IDischargeService _discharge;
        private readonly IExportService _export;
        private readonly IAuditService _audit;
        private readonly UserManager<ApplicationUser> _users;

        public DischargeController(IDischargeService discharge, IExportService export, IAuditService audit, UserManager<ApplicationUser> users)
        {
            _discharge = discharge;
            _export = export;
            _audit = audit;
            _users = users;
        }

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private async Task<string> UserDisplayNameAsync()
        {
            var user = await _users.GetUserAsync(User);
            var name = user == null ? null : $"{user.FirstName} {user.LastName}".Trim();
            return string.IsNullOrWhiteSpace(name) ? User.Identity?.Name ?? "Unknown" : name;
        }

        // GET /Discharge – discharge reports of all patients (choose a patient or search by name) and patients still admitted.
        public async Task<IActionResult> Index(int? patientId, string? search, DateTime? from, DateTime? to, string? status)
        {
            var vm = new DischargeReportListViewModel
            {
                PatientId = patientId,
                Search = search,
                From = from,
                To = to,
                Status = status,
                Reports = await _discharge.ListAsync(patientId, search, from, to, status),
                Admitted = await _discharge.AdmittedAsync(),
                PatientOptions = await _discharge.PatientOptionsAsync(patientId)
            };
            return View(vm);
        }

        // GET /Discharge/Patient?admissionId=5 – the Discharge Patient form.
        [HttpGet]
        public async Task<IActionResult> Patient(int admissionId)
        {
            var existing = await _discharge.GetForAdmissionAsync(admissionId);
            if (existing != null) return RedirectToAction(nameof(Details), new { id = existing.Id });

            var form = await _discharge.NewFormAsync(admissionId);
            if (form == null) return NotFound();
            return View(form);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Patient(DischargeFormViewModel form)
        {
            if (!DischargeSummary.Conditions.Contains(form.ConditionAtDischarge))
            {
                ModelState.AddModelError(nameof(form.ConditionAtDischarge), "Choose the condition at discharge.");
            }
            if (form.DischargeDate > DateTime.Now.AddMinutes(5))
            {
                ModelState.AddModelError(nameof(form.DischargeDate), "The discharge cannot be in the future.");
            }
            var admitted = (await _discharge.NewFormAsync(form.AdmissionId))?.AdmissionDate;
            if (admitted.HasValue && form.DischargeDate < admitted.Value)
            {
                ModelState.AddModelError(nameof(form.DischargeDate), $"The discharge cannot be before the admission ({admitted.Value:dd-MMM-yyyy hh:mm tt}).");
            }
            if (!ModelState.IsValid)
            {
                var shown = await _discharge.NewFormAsync(form.AdmissionId);
                if (shown == null) return NotFound();
                form.PatientName = shown.PatientName;
                form.PatientCode = shown.PatientCode;
                form.WardBed = shown.WardBed;
                form.AdmissionDate = shown.AdmissionDate;
                return View(form);
            }

            try
            {
                var summary = await _discharge.DischargeAsync(form, UserId, await UserDisplayNameAsync());
                await _audit.LogActivityAsync(UserId, "DISCHARGE", "IPDAdmission", form.AdmissionId.ToString(), "Status: Admitted",
                    $"Status: Discharged, discharge report {summary.ReportNumber} ({summary.Status})");
                TempData["SuccessMessage"] = form.Complete
                    ? $"The patient has been discharged and the discharge report {summary.ReportNumber} is complete."
                    : $"The patient has been discharged. Discharge report {summary.ReportNumber} is saved as a draft – complete it when the details are final.";
                return RedirectToAction(nameof(Details), new { id = summary.Id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Details", "IPD", new { id = form.AdmissionId });
            }
        }

        // GET /Discharge/Details/5 – the report as it prints, with Download PDF, Print and Complete.
        public async Task<IActionResult> Details(int id)
        {
            var summary = await _discharge.GetAsync(id);
            if (summary == null) return NotFound();
            ViewData["ReportDocument"] = _discharge.BuildDocument(summary);
            return View(summary);
        }

        // GET /Discharge/ForAdmission/5 – the report of an admission (created for older discharges without one).
        public async Task<IActionResult> ForAdmission(int admissionId)
        {
            var summary = await _discharge.EnsureForAdmissionAsync(admissionId, UserId, await UserDisplayNameAsync());
            if (summary == null)
            {
                TempData["ErrorMessage"] = "This patient has not been discharged yet.";
                return RedirectToAction("Details", "IPD", new { id = admissionId });
            }
            return RedirectToAction(nameof(Details), new { id = summary.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var form = await _discharge.EditFormAsync(id);
            if (form == null) return NotFound();
            var summary = await _discharge.GetAsync(id);
            if (summary!.IsCompleted)
            {
                TempData["InfoMessage"] = "This report is completed. An administrator can reopen it for corrections.";
                return RedirectToAction(nameof(Details), new { id });
            }
            return View(form);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DischargeFormViewModel form)
        {
            if (!DischargeSummary.Conditions.Contains(form.ConditionAtDischarge))
            {
                ModelState.AddModelError(nameof(form.ConditionAtDischarge), "Choose the condition at discharge.");
            }
            if (!form.SummaryId.HasValue) return NotFound();
            if (!ModelState.IsValid)
            {
                var shown = await _discharge.EditFormAsync(form.SummaryId.Value);
                if (shown == null) return NotFound();
                form.PatientName = shown.PatientName;
                form.PatientCode = shown.PatientCode;
                form.WardBed = shown.WardBed;
                form.AdmissionDate = shown.AdmissionDate;
                form.ReportNumber = shown.ReportNumber;
                return View(form);
            }

            var summary = await _discharge.UpdateAsync(form, await UserDisplayNameAsync());
            if (summary == null)
            {
                TempData["ErrorMessage"] = "The report could not be saved (it may have been completed in the meantime).";
                return RedirectToAction(nameof(Details), new { id = form.SummaryId });
            }
            await _audit.LogActivityAsync(UserId, form.Complete ? "COMPLETE" : "UPDATE", "DischargeSummary", summary.Id.ToString(), null,
                $"{summary.ReportNumber}: {summary.Status}");
            TempData["SuccessMessage"] = form.Complete ? $"Discharge report {summary.ReportNumber} is complete." : "Draft saved.";
            return RedirectToAction(nameof(Details), new { id = summary.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Reopen(int id)
        {
            if (await _discharge.ReopenAsync(id))
            {
                await _audit.LogActivityAsync(UserId, "REOPEN", "DischargeSummary", id.ToString(), "Completed", "Draft");
                TempData["SuccessMessage"] = "The report is a draft again and can be corrected.";
            }
            return RedirectToAction(nameof(Edit), new { id });
        }

        // GET /Discharge/Download/5 – PDF with the hospital letterhead and logo.
        public async Task<IActionResult> Download(int id)
        {
            var summary = await _discharge.GetAsync(id);
            if (summary == null) return NotFound();
            var pdf = _export.BuildReportPdf(_discharge.BuildDocument(summary));
            await _audit.LogActivityAsync(UserId, "DOWNLOAD", "DischargeSummary", id.ToString(), null, summary.ReportNumber);
            return File(pdf, "application/pdf", $"Discharge_Summary_{summary.ReportNumber}_{summary.Patient?.FirstName}_{summary.Patient?.LastName}.pdf".Replace(' ', '_'));
        }
    }
}
