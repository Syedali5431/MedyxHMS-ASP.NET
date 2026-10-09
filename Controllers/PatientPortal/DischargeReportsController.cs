using System.Security.Claims;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Purpose: Patient portal "Discharge Reports": the patient's own discharge reports. Completed reports can be viewed
// and downloaded as PDF; reports the care team is still completing are listed as "being prepared".
namespace MedyxHMS.Controllers.PatientPortal
{
    [Area("PatientPortal")]
    [Authorize(Roles = "Patient")]
    [Route("PatientPortal/[controller]/[action]")]
    public class DischargeReportsController : Controller
    {
        private readonly IPatientPortalService _patientPortalService;
        private readonly IDischargeService _discharge;
        private readonly IExportService _export;
        private readonly IAuditService _audit;

        public DischargeReportsController(IPatientPortalService patientPortalService, IDischargeService discharge, IExportService export, IAuditService audit)
        {
            _patientPortalService = patientPortalService;
            _discharge = discharge;
            _export = export;
            _audit = audit;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue) return LocalRedirect("/PatientPortal/Account/Login");
            return View(await _discharge.ListForPatientAsync(patientId.Value));
        }

        // GET /PatientPortal/DischargeReports/Download/5?inline=true – own, completed reports only.
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Download(int id, bool inline = false)
        {
            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue) return LocalRedirect("/PatientPortal/Account/Login");

            var summary = await _discharge.GetAsync(id);
            if (summary == null || summary.PatientId != patientId.Value || !summary.IsCompleted) return NotFound();

            var pdf = _export.BuildReportPdf(_discharge.BuildDocument(summary));
            await _audit.LogActivityAsync(UserId, "DOWNLOAD", "DischargeSummary", id.ToString(), null, $"{summary.ReportNumber} (patient portal)");
            var fileName = $"Discharge_Summary_{summary.ReportNumber}.pdf";
            if (inline)
            {
                Response.Headers.ContentDisposition = $"inline; filename=\"{fileName}\"";
                return File(pdf, "application/pdf");
            }
            return File(pdf, "application/pdf", fileName);
        }

        private async Task<int?> ResolveCurrentPatientIdAsync()
        {
            if (string.IsNullOrWhiteSpace(UserId)) return null;
            var patient = await _patientPortalService.GetPatientByIdAsync(UserId);
            return patient?.Id;
        }
    }
}
