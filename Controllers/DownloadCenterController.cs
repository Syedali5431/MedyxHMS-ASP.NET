using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MedyxHMS.Controllers
{
    [Authorize]
    public class DownloadCenterController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IAuditService _audit;

        public DownloadCenterController(ApplicationDbContext context, IWebHostEnvironment env, IAuditService audit)
        {
            _context = context;
            _env = env;
            _audit = audit;
        }

        // ── List ──────────────────────────────────────────────────

        public async Task<IActionResult> Index(string? category)
        {
            var isAdminOrSuper = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

            var query = _context.DownloadFiles
                .Where(f => f.IsActive && (f.IsPublic || isAdminOrSuper))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(f => f.Category == category);

            ViewBag.Category = category;
            var files = await query.OrderByDescending(f => f.UploadedAt).ToListAsync();
            return View(files);
        }

        // ── Upload (Admin+) ───────────────────────────────────────

        [Authorize(Roles = "Admin,SuperAdmin")]
        public IActionResult Upload()
        {
            return View(new DownloadFile());
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Upload(DownloadFile model, IFormFile file)
        {
            if (file == null || file.Length == 0)
                ModelState.AddModelError("file", "Please select a file to upload.");

            if (!ModelState.IsValid) return View(model);

            // Sanitize and store the file
            var allowedTypes = new[] { ".pdf", ".docx", ".xlsx", ".doc", ".xls", ".txt", ".png", ".jpg" };
            var ext = Path.GetExtension(file!.FileName).ToLowerInvariant();
            if (!allowedTypes.Contains(ext))
            {
                ModelState.AddModelError("file", "File type not allowed.");
                return View(model);
            }

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "downloads");
            Directory.CreateDirectory(uploadsDir);

            var safeFileName = Guid.NewGuid().ToString("N") + ext;
            var fullPath = Path.Combine(uploadsDir, safeFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            model.FileName = Path.GetFileName(file.FileName);
            model.FilePath = $"/uploads/downloads/{safeFileName}";
            model.FileType = ext.TrimStart('.');
            model.FileSizeBytes = file.Length;
            model.UploadedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            model.UploadedAt = DateTime.UtcNow;

            _context.DownloadFiles.Add(model);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(model.UploadedByUserId, "UPLOAD", "DownloadFile", model.Id.ToString(), null, model.Title);
            TempData["SuccessMessage"] = "File uploaded.";
            return RedirectToAction(nameof(Index));
        }

        // ── Download (increment counter + serve) ──────────────────

        public async Task<IActionResult> Download(int id)
        {
            var isAdminOrSuper = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
            var file = await _context.DownloadFiles
                .FirstOrDefaultAsync(f => f.Id == id && f.IsActive && (f.IsPublic || isAdminOrSuper));
            if (file == null) return NotFound();

            file.DownloadCount++;
            await _context.SaveChangesAsync();

            var fullPath = Path.Combine(_env.WebRootPath, file.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            var mimeType = file.FileType switch
            {
                "pdf"  => "application/pdf",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _      => "application/octet-stream"
            };

            return PhysicalFile(fullPath, mimeType, file.FileName);
        }

        // ── Delete (Admin+) ───────────────────────────────────────

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var file = await _context.DownloadFiles.FindAsync(id);
            if (file == null) return NotFound();

            file.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "File removed from download center.";
            return RedirectToAction(nameof(Index));
        }
    }
}
