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
    public class BirthDeathController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public BirthDeathController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // ── Birth Records ─────────────────────────────────────────

        public async Task<IActionResult> Index()
        {
            var births = await _context.BirthRecords
                .Include(b => b.Patient)
                .OrderByDescending(b => b.DateOfBirth)
                .ToListAsync();
            return View(births);
        }

        public IActionResult CreateBirth()
        {
            return View(new BirthRecord { DateOfBirth = DateTime.Today });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBirth(BirthRecord model)
        {
            if (!ModelState.IsValid) return View(model);

            if (string.IsNullOrWhiteSpace(model.CertificateNumber))
                model.CertificateNumber = $"BR-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            _context.BirthRecords.Add(model);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier),
                "CREATE", "BirthRecord", model.Id.ToString(), null,
                $"Baby: {model.BabyName}, Mother: {model.MotherName}");
            TempData["SuccessMessage"] = "Birth record saved.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> BirthDetails(int id)
        {
            var record = await _context.BirthRecords.Include(b => b.Patient).FirstOrDefaultAsync(b => b.Id == id);
            if (record == null) return NotFound();
            return View(record);
        }

        // ── Death Records ─────────────────────────────────────────

        public async Task<IActionResult> Deaths()
        {
            var deaths = await _context.DeathRecords
                .Include(d => d.Patient)
                .OrderByDescending(d => d.DateOfDeath)
                .ToListAsync();
            return View(deaths);
        }

        public IActionResult CreateDeath()
        {
            return View(new DeathRecord { DateOfDeath = DateTime.Today });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDeath(DeathRecord model)
        {
            if (!ModelState.IsValid) return View(model);

            if (string.IsNullOrWhiteSpace(model.CertificateNumber))
                model.CertificateNumber = $"DR-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            _context.DeathRecords.Add(model);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier),
                "CREATE", "DeathRecord", model.Id.ToString(), null,
                $"Patient: {model.PatientName}, Cause: {model.CauseOfDeath}");
            TempData["SuccessMessage"] = "Death record saved.";
            return RedirectToAction(nameof(Deaths));
        }

        public async Task<IActionResult> DeathDetails(int id)
        {
            var record = await _context.DeathRecords.Include(d => d.Patient).FirstOrDefaultAsync(d => d.Id == id);
            if (record == null) return NotFound();
            return View(record);
        }
    }
}
