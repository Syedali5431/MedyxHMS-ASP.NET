using System.Security.Claims;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Controllers
{
    /// <summary>
    /// Document control: policies/SOPs/forms with numbered versions. A version is drafted, submitted, and approved
    /// by a different administrator; the approved version becomes effective and the previous one is superseded.
    /// All staff can read effective documents.
    /// </summary>
    [Authorize(Roles = QualityService.StaffRoles)]
    public class QualityDocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly QualityService _quality;
        private readonly IAuditService _audit;

        public QualityDocumentsController(ApplicationDbContext context, QualityService quality, IAuditService audit)
        {
            _context = context;
            _quality = quality;
            _audit = audit;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        private bool IsManager => User.IsInRole("SuperAdmin") || User.IsInRole("Admin");

        private async Task<string> CurrentUserNameAsync()
        {
            var u = await _context.Users.AsNoTracking().Where(x => x.Id == UserId).Select(x => new { x.FirstName, x.LastName, x.UserName }).FirstOrDefaultAsync();
            var name = u == null ? string.Empty : $"{u.FirstName} {u.LastName}".Trim();
            return string.IsNullOrWhiteSpace(name) ? (u?.UserName ?? User.Identity?.Name ?? string.Empty) : name;
        }

        public async Task<IActionResult> Index(string? search, string? category)
        {
            var query = _context.ControlledDocuments.AsNoTracking().Include(d => d.Versions).AsQueryable();
            if (!IsManager)
            {
                query = query.Where(d => d.Status == "Effective");
            }

            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(d => d.Title.Contains(search) || d.DocumentNumber.Contains(search));
            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(d => d.Category == category);

            ViewBag.IsManager = IsManager;
            ViewBag.Search = search;
            ViewBag.Category = category;
            return View(await query.OrderBy(d => d.DocumentNumber).ToListAsync());
        }

        [Authorize(Roles = QualityService.ManagerRoles)]
        public IActionResult Create()
        {
            return View(new ControlledDocumentCreateViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = QualityService.ManagerRoles)]
        [RequestSizeLimit(QualityService.MaxDocumentBytes + 1024 * 1024)]
        public async Task<IActionResult> Create(ControlledDocumentCreateViewModel model)
        {
            var doc = model.Document;
            doc.DocumentNumber = (doc.DocumentNumber ?? string.Empty).Trim().ToUpperInvariant();
            if (!QualityService.DocumentCategories.Contains(doc.Category)) ModelState.AddModelError("Document.Category", "Choose a category.");
            if (await _context.ControlledDocuments.AnyAsync(d => d.DocumentNumber == doc.DocumentNumber)) ModelState.AddModelError("Document.DocumentNumber", "This document number is already used.");
            ModelState.Remove("Document.Versions");
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (stored, error) = await _quality.SaveDocumentFileAsync(model.File);
            if (error != null)
            {
                ModelState.AddModelError(nameof(model.File), error);
                return View(model);
            }

            var name = await CurrentUserNameAsync();
            var document = new ControlledDocument
            {
                DocumentNumber = doc.DocumentNumber,
                Title = doc.Title.Trim(),
                Category = doc.Category,
                Department = doc.Department?.Trim() ?? string.Empty,
                ReviewIntervalMonths = doc.ReviewIntervalMonths,
                Status = "Draft",
                CreatedAt = DateTime.Now,
                CreatedBy = name
            };
            document.Versions.Add(NewVersion(1, model.ChangeSummary, stored!, model.File!, name));
            _context.ControlledDocuments.Add(document);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "CREATE", "ControlledDocument", document.Id.ToString(), null, $"{document.DocumentNumber} {document.Title} v1 (draft)");

            TempData["SuccessMessage"] = $"Document {document.DocumentNumber} created as a draft. Submit version 1 for approval when ready.";
            return RedirectToAction(nameof(Details), new { id = document.Id });
        }

        public async Task<IActionResult> Details(int id)
        {
            var document = await _context.ControlledDocuments.AsNoTracking().Include(d => d.Versions).FirstOrDefaultAsync(d => d.Id == id);
            if (document == null) return NotFound();
            if (!IsManager && document.Status != "Effective") return NotFound();

            ViewBag.IsManager = IsManager;
            ViewBag.UserId = UserId;
            return View(document);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = QualityService.ManagerRoles)]
        [RequestSizeLimit(QualityService.MaxDocumentBytes + 1024 * 1024)]
        public async Task<IActionResult> UploadVersion(int id, string? changeSummary, IFormFile? file)
        {
            var document = await _context.ControlledDocuments.Include(d => d.Versions).FirstOrDefaultAsync(d => d.Id == id);
            if (document == null) return NotFound();
            if (document.Status == "Obsolete" || document.Versions.Any(v => v.Status == "Draft" || v.Status == "Pending approval"))
            {
                TempData["ErrorMessage"] = document.Status == "Obsolete" ? "The document is obsolete." : "Finish the open draft first (submit, approve or reject it).";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(changeSummary))
            {
                TempData["ErrorMessage"] = "Describe what changed in this version.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var (stored, error) = await _quality.SaveDocumentFileAsync(file);
            if (error != null)
            {
                TempData["ErrorMessage"] = error;
                return RedirectToAction(nameof(Details), new { id });
            }

            var number = document.Versions.Max(v => v.VersionNumber) + 1;
            document.Versions.Add(NewVersion(number, changeSummary, stored!, file!, await CurrentUserNameAsync()));
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "CREATE", "DocumentVersion", document.Id.ToString(), null, $"{document.DocumentNumber} v{number} drafted: {changeSummary}");
            TempData["SuccessMessage"] = $"Version {number} uploaded as a draft.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = QualityService.ManagerRoles)]
        public async Task<IActionResult> Submit(int versionId)
        {
            var version = await _context.DocumentVersions.Include(v => v.Document).FirstOrDefaultAsync(v => v.Id == versionId);
            if (version == null) return NotFound();
            if (version.Status != "Draft" && version.Status != "Rejected")
            {
                TempData["ErrorMessage"] = "Only a draft or rejected version can be submitted.";
                return RedirectToAction(nameof(Details), new { id = version.DocumentId });
            }

            version.Status = "Pending approval";
            version.SubmittedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "SUBMIT", "DocumentVersion", version.Id.ToString(), null, $"{version.Document.DocumentNumber} v{version.VersionNumber} submitted for approval");
            TempData["SuccessMessage"] = $"Version {version.VersionNumber} submitted for approval. Another administrator must approve it.";
            return RedirectToAction(nameof(Details), new { id = version.DocumentId });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = QualityService.ManagerRoles)]
        public async Task<IActionResult> Decide(int versionId, bool approve, string? comments)
        {
            var version = await _context.DocumentVersions.Include(v => v.Document).ThenInclude(d => d.Versions).FirstOrDefaultAsync(v => v.Id == versionId);
            if (version == null) return NotFound();
            var back = RedirectToAction(nameof(Details), new { id = version.DocumentId });

            if (version.Status != "Pending approval")
            {
                TempData["ErrorMessage"] = "This version is not waiting for approval.";
                return back;
            }

            // Four-eyes principle: the author cannot approve or reject their own version.
            if (version.CreatedByUserId == UserId)
            {
                TempData["ErrorMessage"] = "You uploaded this version, so another administrator must approve it.";
                return back;
            }

            if (!approve && string.IsNullOrWhiteSpace(comments))
            {
                TempData["ErrorMessage"] = "Give a reason when rejecting.";
                return back;
            }

            var name = await CurrentUserNameAsync();
            version.ApprovedByUserId = UserId;
            version.ApprovedBy = name;
            version.ApprovedAt = DateTime.Now;
            version.ApprovalComments = comments?.Trim() ?? string.Empty;

            if (approve)
            {
                foreach (var previous in version.Document.Versions.Where(v => v.Id != version.Id && v.Status == "Approved"))
                {
                    previous.Status = "Superseded";
                }

                version.Status = "Approved";
                version.EffectiveDate = DateTime.Today;
                version.Document.Status = "Effective";
                version.Document.NextReviewDate = DateTime.Today.AddMonths(version.Document.ReviewIntervalMonths);
            }
            else
            {
                version.Status = "Rejected";
            }

            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, approve ? "APPROVE" : "REJECT", "DocumentVersion", version.Id.ToString(), null,
                $"{version.Document.DocumentNumber} v{version.VersionNumber} {(approve ? "approved – effective" : "rejected")}: {version.ApprovalComments}");
            TempData["SuccessMessage"] = approve ? $"Version {version.VersionNumber} approved and now effective." : $"Version {version.VersionNumber} rejected.";
            return back;
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = QualityService.ManagerRoles)]
        public async Task<IActionResult> MarkObsolete(int id, string? reason)
        {
            var document = await _context.ControlledDocuments.FirstOrDefaultAsync(d => d.Id == id);
            if (document == null) return NotFound();
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Give a reason for withdrawing the document.";
                return RedirectToAction(nameof(Details), new { id });
            }

            document.Status = "Obsolete";
            document.NextReviewDate = null;
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "OBSOLETE", "ControlledDocument", id.ToString(), null, $"{document.DocumentNumber} withdrawn: {reason}");
            TempData["SuccessMessage"] = $"{document.DocumentNumber} marked obsolete (kept for the record).";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Staff can download the effective version; administrators can download any version.</summary>
        public async Task<IActionResult> Download(int versionId)
        {
            var version = await _context.DocumentVersions.AsNoTracking().Include(v => v.Document).FirstOrDefaultAsync(v => v.Id == versionId);
            if (version == null) return NotFound();
            if (!IsManager && !(version.Status == "Approved" && version.Document.Status == "Effective")) return NotFound();

            var path = _quality.GetDocumentFilePath(version.StoredFileName);
            if (path == null) return NotFound();

            if (!new FileExtensionContentTypeProvider().TryGetContentType(version.OriginalFileName, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            var downloadName = $"{version.Document.DocumentNumber}_v{version.VersionNumber}{Path.GetExtension(version.OriginalFileName)}";
            return PhysicalFile(path, contentType, downloadName);
        }

        private DocumentVersion NewVersion(int number, string? summary, string stored, IFormFile file, string author) => new()
        {
            VersionNumber = number,
            ChangeSummary = string.IsNullOrWhiteSpace(summary) ? (number == 1 ? "First issue" : "Revision") : summary.Trim(),
            StoredFileName = stored,
            OriginalFileName = Path.GetFileName(file.FileName),
            FileSize = file.Length,
            Status = "Draft",
            CreatedByUserId = UserId,
            CreatedBy = author,
            CreatedAt = DateTime.Now
        };
    }
}
