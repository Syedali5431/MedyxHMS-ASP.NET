using System.Security.Claims;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

// Purpose: A patient's file for clinical and front-office staff: medical records (from OPD visits and IPD admissions)
// and documents (upload, view/download, share with the patient, remove with reason). Every access is audited.
namespace MedyxHMS.Controllers
{
    [Authorize(Roles = ViewRoles)]
    public class PatientRecordsController : Controller
    {
        public const string ViewRoles = "SuperAdmin,Admin,Doctor,Nurse,Receptionist,LabTechnician,Radiologist,Pathologist,Staff";

        private readonly ApplicationDbContext _context;
        private readonly IMedicalRecordService _records;
        private readonly PatientDocumentService _documents;
        private readonly IAuditService _audit;
        private readonly ISystemNotificationService _notifications;

        public PatientRecordsController(ApplicationDbContext context, IMedicalRecordService records, PatientDocumentService documents, IAuditService audit, ISystemNotificationService notifications)
        {
            _context = context;
            _records = records;
            _documents = documents;
            _audit = audit;
            _notifications = notifications;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        private string UserName => User.Identity?.Name ?? string.Empty;
        private bool IsManager => User.IsInRole("SuperAdmin") || User.IsInRole("Admin");
        private bool CanManage(PatientDocument d) => IsManager || d.UploadedByUserId == UserId;
        private bool CanShare(PatientDocument d) => !d.UploadedByPatient && (IsManager || User.IsInRole("Doctor") || d.UploadedByUserId == UserId);

        [HttpGet]
        public async Task<IActionResult> Index(int id, string? type)
        {
            var patient = await _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (patient == null) return NotFound();

            await _records.EnsureRecordsAsync(id);
            var records = _context.MedicalRecords.AsNoTracking().Where(m => m.PatientId == id);
            if (type is "OPD" or "IPD") records = records.Where(m => m.RecordType == type);
            ViewBag.Records = await records.OrderByDescending(m => m.RecordDate).ToListAsync();
            ViewBag.Documents = await _context.PatientDocuments.AsNoTracking().Where(d => d.PatientId == id && !d.IsDeleted).OrderByDescending(d => d.UploadedAt).ToListAsync();
            ViewBag.RemovedCount = await _context.PatientDocuments.CountAsync(d => d.PatientId == id && d.IsDeleted);
            ViewBag.Type = type;
            ViewBag.UserId = UserId;
            ViewBag.IsManager = IsManager;
            ViewBag.IsDoctor = User.IsInRole("Doctor");
            await _audit.LogActivityAsync(UserId, "VIEW", "PatientRecords", id.ToString(), null, $"{patient.PatientId} records and documents opened");
            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(PatientDocumentService.MaxBytes + 1024 * 1024)]
        public async Task<IActionResult> Upload(int id, string? category, string? title, string? description, DateTime? documentDate, bool sharedWithPatient, IFormFile? file)
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == id);
            if (patient == null) return NotFound();

            string? error = !PatientDocumentService.StaffCategories.Contains(category) ? "Choose the document category."
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
                return Redirect(Url.Action(nameof(Index), new { id }) + "#documents");
            }

            document.PatientId = id;
            document.Category = category!;
            document.Title = title!.Trim();
            var details = (description ?? string.Empty).Trim();
            document.Description = details.Length > 1000 ? details[..1000] : details;
            document.DocumentDate = documentDate?.Date;
            document.SharedWithPatient = sharedWithPatient;
            document.UploadedByUserId = UserId;
            document.UploadedBy = UserName;
            document.UploadedAt = DateTime.Now;
            _context.PatientDocuments.Add(document);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "UPLOAD", "PatientDocument", document.Id.ToString(), null,
                $"{patient.PatientId}: {document.Category} \"{document.Title}\" ({document.OriginalFileName}, {document.SizeBytes} bytes, sha256 {document.Sha256}){(document.SharedWithPatient ? ", shared with patient" : "")}");
            if (document.SharedWithPatient) await NotifyPatientAsync(patient, document);
            TempData["SuccessMessage"] = $"Document \"{document.Title}\" added to the patient's file.";
            return Redirect(Url.Action(nameof(Index), new { id }) + "#documents");
        }

        [HttpGet]
        public async Task<IActionResult> Document(int documentId)
        {
            var document = await _context.PatientDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);
            if (document == null) return NotFound();
            var path = _documents.GetPath(document.StoredFileName);
            if (path == null) return NotFound();

            await _audit.LogActivityAsync(UserId, "DOWNLOAD", "PatientDocument", documentId.ToString(), null, $"Patient {document.PatientId}: \"{document.Title}\"");
            return FileResult(path, document);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Share(int documentId)
        {
            var document = await _context.PatientDocuments.Include(d => d.Patient).FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);
            if (document == null) return NotFound();
            if (!CanShare(document))
            {
                TempData["ErrorMessage"] = "Only the person who uploaded the document, a doctor or an administrator can change whether the patient sees it.";
                return Redirect(Url.Action(nameof(Index), new { id = document.PatientId }) + "#documents");
            }

            document.SharedWithPatient = !document.SharedWithPatient;
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, document.SharedWithPatient ? "SHARE" : "UNSHARE", "PatientDocument", documentId.ToString(), null, $"\"{document.Title}\"");
            if (document.SharedWithPatient) await NotifyPatientAsync(document.Patient, document);
            TempData["SuccessMessage"] = document.SharedWithPatient ? "The patient can now see this document in the portal." : "The document is no longer shown to the patient.";
            return Redirect(Url.Action(nameof(Index), new { id = document.PatientId }) + "#documents");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int documentId, string? reason)
        {
            var document = await _context.PatientDocuments.FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);
            if (document == null) return NotFound();
            string? error = !CanManage(document) ? "Only the person who uploaded the document or an administrator can remove it."
                : string.IsNullOrWhiteSpace(reason) ? "Give the reason for removing the document." : null;
            if (error != null)
            {
                TempData["ErrorMessage"] = error;
                return Redirect(Url.Action(nameof(Index), new { id = document.PatientId }) + "#documents");
            }

            // Removed documents stay on file (hidden) with who removed them and why.
            document.IsDeleted = true;
            document.DeletedAt = DateTime.Now;
            document.DeletedBy = UserName;
            document.DeleteReason = reason!.Trim().Length > 300 ? reason.Trim()[..300] : reason.Trim();
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "REMOVE", "PatientDocument", documentId.ToString(), $"\"{document.Title}\"", document.DeleteReason);
            TempData["SuccessMessage"] = $"Document \"{document.Title}\" removed from the file.";
            return Redirect(Url.Action(nameof(Index), new { id = document.PatientId }) + "#documents");
        }

        private IActionResult FileResult(string path, PatientDocument document)
        {
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

        private async Task NotifyPatientAsync(Patient patient, PatientDocument document)
        {
            if (string.IsNullOrEmpty(patient.UserId)) return;
            await _notifications.CreateForUserAsync(patient.UserId, "New document in your records",
                $"\"{document.Title}\" ({document.Category}) has been added to your records.", "PatientDocument", nameof(PatientDocument), document.Id.ToString(), patient.Id);
        }
    }
}
