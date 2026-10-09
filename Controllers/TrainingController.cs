using System.Security.Claims;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Controllers
{
    /// <summary>Staff training and certification records with expiry tracking. Staff see their own records.</summary>
    [Authorize(Roles = QualityService.StaffRoles)]
    public class TrainingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly QualityService _quality;
        private readonly IAuditService _audit;

        public TrainingController(ApplicationDbContext context, QualityService quality, IAuditService audit)
        {
            _context = context;
            _quality = quality;
            _audit = audit;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        private bool IsManager => User.IsInRole("SuperAdmin") || User.IsInRole("Admin");

        public async Task<IActionResult> Index(string? filter, string? staffId)
        {
            var query = _context.TrainingRecords.AsNoTracking().AsQueryable();
            if (!IsManager)
            {
                query = query.Where(t => t.StaffId == UserId);
            }
            else if (!string.IsNullOrWhiteSpace(staffId))
            {
                query = query.Where(t => t.StaffId == staffId);
            }

            var today = DateTime.Today;
            query = filter switch
            {
                "expiring" => query.Where(t => t.ExpiryDate != null && t.ExpiryDate >= today && t.ExpiryDate <= today.AddDays(30)),
                "expired" => query.Where(t => t.ExpiryDate != null && t.ExpiryDate < today),
                _ => query
            };

            return View(new TrainingIndexViewModel
            {
                Records = await query.OrderBy(t => t.StaffName).ThenByDescending(t => t.CompletedDate).Take(1000).ToListAsync(),
                IsManager = IsManager,
                Filter = filter,
                StaffId = staffId,
                Staff = IsManager ? await _quality.GetStaffOptionsAsync() : new List<StaffOption>()
            });
        }

        [Authorize(Roles = QualityService.ManagerRoles)]
        public async Task<IActionResult> Create(string? staffId)
        {
            ViewBag.Staff = await _quality.GetStaffOptionsAsync();
            return View("Form", new TrainingRecord { StaffId = staffId ?? string.Empty, CompletedDate = DateTime.Today });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = QualityService.ManagerRoles)]
        public async Task<IActionResult> Create([Bind("StaffId,CourseTitle,Category,Provider,CompletedDate,ExpiryDate,CertificateNumber,Result,Notes")] TrainingRecord model)
        {
            var staff = await ValidateAsync(model);
            if (!ModelState.IsValid)
            {
                ViewBag.Staff = await _quality.GetStaffOptionsAsync();
                return View("Form", model);
            }

            model.StaffName = staff!.Name;
            model.StaffDepartment = staff.Department;
            model.CreatedAt = DateTime.Now;
            model.CreatedBy = User.Identity?.Name ?? string.Empty;
            _context.TrainingRecords.Add(model);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "CREATE", "TrainingRecord", model.Id.ToString(), null, $"{model.StaffName}: {model.CourseTitle} ({model.CompletedDate:yyyy-MM-dd})");
            TempData["SuccessMessage"] = $"Training record added for {model.StaffName}.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = QualityService.ManagerRoles)]
        public async Task<IActionResult> Edit(int id)
        {
            var record = await _context.TrainingRecords.FindAsync(id);
            if (record == null) return NotFound();
            ViewBag.Staff = await _quality.GetStaffOptionsAsync();
            return View("Form", record);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = QualityService.ManagerRoles)]
        public async Task<IActionResult> Edit(int id, [Bind("StaffId,CourseTitle,Category,Provider,CompletedDate,ExpiryDate,CertificateNumber,Result,Notes")] TrainingRecord model)
        {
            var record = await _context.TrainingRecords.FindAsync(id);
            if (record == null) return NotFound();

            model.Id = id;
            var staff = await ValidateAsync(model, record);
            if (!ModelState.IsValid)
            {
                ViewBag.Staff = await _quality.GetStaffOptionsAsync();
                return View("Form", model);
            }

            var old = $"{record.CourseTitle} {record.CompletedDate:yyyy-MM-dd} expiry {record.ExpiryDate:yyyy-MM-dd}";
            if (record.StaffId != model.StaffId && staff != null)
            {
                record.StaffId = staff.Id;
                record.StaffName = staff.Name;
                record.StaffDepartment = staff.Department;
            }

            record.CourseTitle = model.CourseTitle.Trim();
            record.Category = model.Category;
            record.Provider = model.Provider?.Trim() ?? string.Empty;
            record.CompletedDate = model.CompletedDate;
            record.ExpiryDate = model.ExpiryDate;
            record.CertificateNumber = model.CertificateNumber?.Trim() ?? string.Empty;
            record.Result = model.Result?.Trim() ?? string.Empty;
            record.Notes = model.Notes?.Trim() ?? string.Empty;
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "UPDATE", "TrainingRecord", id.ToString(), old, $"{record.CourseTitle} {record.CompletedDate:yyyy-MM-dd} expiry {record.ExpiryDate:yyyy-MM-dd}");
            TempData["SuccessMessage"] = "Training record updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = QualityService.ManagerRoles)]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _context.TrainingRecords.FindAsync(id);
            if (record == null) return NotFound();
            _context.TrainingRecords.Remove(record);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "DELETE", "TrainingRecord", id.ToString(), $"{record.StaffName}: {record.CourseTitle} {record.CompletedDate:yyyy-MM-dd}", null);
            TempData["SuccessMessage"] = "Training record deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<StaffOption?> ValidateAsync(TrainingRecord model, TrainingRecord? existing = null)
        {
            var staff = (await _quality.GetStaffOptionsAsync()).FirstOrDefault(s => s.Id == model.StaffId);
            // A record of a staff member who has since left can still be edited.
            if (staff == null && !(existing != null && existing.StaffId == model.StaffId))
            {
                ModelState.AddModelError(nameof(model.StaffId), "Choose a staff member.");
            }

            if (!QualityService.TrainingCategories.Contains(model.Category)) ModelState.AddModelError(nameof(model.Category), "Choose a category.");
            if (model.CompletedDate.Date > DateTime.Today) ModelState.AddModelError(nameof(model.CompletedDate), "The completion date cannot be in the future.");
            if (model.ExpiryDate.HasValue && model.ExpiryDate.Value.Date <= model.CompletedDate.Date) ModelState.AddModelError(nameof(model.ExpiryDate), "The expiry date must be after the completion date.");
            return staff;
        }
    }
}
