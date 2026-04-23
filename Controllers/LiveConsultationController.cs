using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MedyxHMS.Controllers
{
    [Authorize]
    public class LiveConsultationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public LiveConsultationController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // ── List ──────────────────────────────────────────────────

        public async Task<IActionResult> Index()
        {
            var sessions = await _context.LiveConsultationSessions
                .Include(s => s.Patient)
                .OrderByDescending(s => s.ScheduledAt)
                .ToListAsync();
            return View(sessions);
        }

        // ── Schedule ──────────────────────────────────────────────

        public async Task<IActionResult> Schedule()
        {
            ViewBag.Patients = await _context.Patients.OrderBy(p => p.FirstName).ToListAsync();
            ViewBag.Doctors = await _context.Users.OrderBy(u => u.UserName)
                .Select(u => new { u.Id, u.UserName }).ToListAsync();
            return View(new LiveConsultationSession
            {
                ScheduledAt = DateTime.Now.AddHours(1),
                DurationMinutes = 30,
                Platform = "Zoom"
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Schedule(LiveConsultationSession model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Patients = await _context.Patients.OrderBy(p => p.FirstName).ToListAsync();
                ViewBag.Doctors = await _context.Users.OrderBy(u => u.UserName)
                    .Select(u => new { u.Id, u.UserName }).ToListAsync();
                return View(model);
            }

            // Validate meeting link scheme for security (prevent javascript: URLs)
            if (!string.IsNullOrWhiteSpace(model.MeetingLink) &&
                !model.MeetingLink.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.MeetingLink), "Meeting link must start with https://");
                ViewBag.Patients = await _context.Patients.ToListAsync();
                ViewBag.Doctors = await _context.Users.Select(u => new { u.Id, u.UserName }).ToListAsync();
                return View(model);
            }

            model.CreatedDate = DateTime.UtcNow;
            _context.LiveConsultationSessions.Add(model);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier),
                "CREATE", "LiveConsultation", model.Id.ToString(), null,
                $"Doctor: {model.DoctorName}, Patient: {model.PatientName}, At: {model.ScheduledAt:g}");
            TempData["SuccessMessage"] = "Consultation scheduled.";
            return RedirectToAction(nameof(Index));
        }

        // ── Details / Join ────────────────────────────────────────

        public async Task<IActionResult> Details(int id)
        {
            var session = await _context.LiveConsultationSessions
                .Include(s => s.Patient)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (session == null) return NotFound();
            return View(session);
        }

        // ── Update status ─────────────────────────────────────────

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var session = await _context.LiveConsultationSessions.FindAsync(id);
            if (session == null) return NotFound();

            session.Status = status;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Session marked as {status}.";
            return RedirectToAction(nameof(Index));
        }

        // ── Cancel ────────────────────────────────────────────────

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var session = await _context.LiveConsultationSessions.FindAsync(id);
            if (session == null) return NotFound();

            session.Status = "Cancelled";
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Consultation cancelled.";
            return RedirectToAction(nameof(Index));
        }
    }
}
