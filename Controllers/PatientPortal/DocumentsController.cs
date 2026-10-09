using System.Security.Claims;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

// Purpose: Patient portal "My Documents": documents the hospital shared with the patient and documents the
// patient uploaded (e.g. earlier reports from other providers). Patients only ever see their own documents.
namespace MedyxHMS.Controllers.PatientPortal
{
    [Area("PatientPortal")]
    [Authorize(Roles = "Patient")]
    [Route("PatientPortal/[controller]/[action]")]
    public class DocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPatientPortalService _patientPortalService;
        private readonly PatientDocumentService _documents;
        private readonly IAuditService _audit;

        public DocumentsController(ApplicationDbContext context, IPatientPortalService patientPortalService, PatientDocumentService documents, IAuditService audit)
        {
            _context = context;
            _patientPortalService = patientPortalService;
            _documents = documents;
            _audit = audit;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue) return LocalRedirect("/PatientPortal/Account/Login");

            var documents = await _context.PatientDocuments.AsNoTracking()
                .Where(d => d.PatientId == patientId.Value && !d.IsDeleted && (d.SharedWithPatient || d.UploadedByPatient))
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
            return View(documents);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(PatientDocumentService.MaxBytes + 1024 * 1024)]
        public async Task<IActionResult> Upload(string? category, string? title, string? description, DateTime? documentDate, IFormFile? file)
        {
            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue) return LocalRedirect("/PatientPortal/Account/Login");

            string? error = !PatientDocumentService.PatientCategories.Contains(category) ? "Choose the document category."
                : string.IsNullOrWhiteSpace(title) ? "Enter a title for the document."
                : title.Trim().Length > 200 ? "The title can have at most 200 characters."
                : documentDate.HasValue && documentDate.Value.Date > DateTime.Today ? "The document date cannot be in the future." : null;
            PatientDocument? document = null;
            if (error == null)
            {
                (document, error) = await _documents.SaveAsync(file);
            }
            if (error != null || document == null)
            {
                TempData["ErrorMessage"] = error ?? "The document could not be saved.";
                return RedirectToAction(nameof(Index));
            }

            var details = (description ?? string.Empty).Trim();
            document.PatientId = patientId.Value;
            document.Category = category!;
            document.Title = title!.Trim();
            document.Description = details.Length > 1000 ? details[..1000] : details;
            document.DocumentDate = documentDate?.Date;
            document.UploadedByPatient = true;
            document.SharedWithPatient = true;
            document.UploadedByUserId = UserId;
            document.UploadedBy = User.Identity?.Name ?? "Patient";
            document.UploadedAt = DateTime.Now;
            _context.PatientDocuments.Add(document);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "UPLOAD", "PatientDocument", document.Id.ToString(), null,
                $"Patient upload: {document.Category} \"{document.Title}\" ({document.OriginalFileName}, {document.SizeBytes} bytes, sha256 {document.Sha256})");
            TempData["SuccessMessage"] = $"\"{document.Title}\" uploaded. Your care team can now see it in your records.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Download(int id)
        {
            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue) return LocalRedirect("/PatientPortal/Account/Login");

            var document = await _context.PatientDocuments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id && d.PatientId == patientId.Value && !d.IsDeleted && (d.SharedWithPatient || d.UploadedByPatient));
            if (document == null) return NotFound();
            var path = _documents.GetPath(document.StoredFileName);
            if (path == null) return NotFound();

            await _audit.LogActivityAsync(UserId, "DOWNLOAD", "PatientDocument", id.ToString(), null, $"Patient download: \"{document.Title}\"");
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            if (PatientDocumentService.ShowInline(document.ContentType))
            {
                var disposition = new ContentDispositionHeaderValue("inline");
                disposition.SetHttpFileName(document.OriginalFileName);
                Response.Headers[HeaderNames.ContentDisposition] = disposition.ToString();
                return PhysicalFile(path, document.ContentType);
            }
            return PhysicalFile(path, document.ContentType, document.OriginalFileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue) return LocalRedirect("/PatientPortal/Account/Login");

            // Patients can remove only what they uploaded themselves; it stays on file for the audit trail.
            var document = await _context.PatientDocuments.FirstOrDefaultAsync(d => d.Id == id && d.PatientId == patientId.Value && d.UploadedByPatient && !d.IsDeleted);
            if (document == null) return NotFound();
            document.IsDeleted = true;
            document.DeletedAt = DateTime.Now;
            document.DeletedBy = User.Identity?.Name ?? "Patient";
            document.DeleteReason = "Removed by the patient";
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "REMOVE", "PatientDocument", id.ToString(), $"\"{document.Title}\"", document.DeleteReason);
            TempData["SuccessMessage"] = $"\"{document.Title}\" removed.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<int?> ResolveCurrentPatientIdAsync()
        {
            if (string.IsNullOrWhiteSpace(UserId)) return null;
            var patient = await _patientPortalService.GetPatientByIdAsync(UserId);
            return patient?.Id;
        }
    }
}
