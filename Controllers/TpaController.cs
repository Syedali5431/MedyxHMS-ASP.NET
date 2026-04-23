using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Staff,Doctor,Receptionist")]
    public class TpaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public TpaController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // ── TPA Providers ─────────────────────────────────────────

        public async Task<IActionResult> Index()
        {
            var providers = await _context.TpaProviders
                .OrderBy(p => p.Name)
                .ToListAsync();
            return View(providers);
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        public IActionResult Create()
        {
            return View(new TpaProvider());
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Create(TpaProvider model)
        {
            if (!ModelState.IsValid) return View(model);

            _context.TpaProviders.Add(model);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier),
                "CREATE", "TpaProvider", model.Id.ToString(), null, model.Name);
            TempData["SuccessMessage"] = "TPA provider added.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Edit(int id)
        {
            var provider = await _context.TpaProviders.FindAsync(id);
            if (provider == null) return NotFound();
            return View(provider);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Edit(int id, TpaProvider model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            _context.TpaProviders.Update(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "TPA provider updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── TPA Claims ────────────────────────────────────────────

        public async Task<IActionResult> Claims()
        {
            var claims = await _context.TpaClaims
                .Include(c => c.TpaProvider)
                .Include(c => c.Patient)
                .OrderByDescending(c => c.ClaimDate)
                .ToListAsync();
            return View(claims);
        }

        public async Task<IActionResult> CreateClaim()
        {
            ViewBag.Providers = await _context.TpaProviders.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            ViewBag.Patients = await _context.Patients.OrderBy(p => p.FirstName).ToListAsync();
            ViewBag.Bills = await _context.Bills.OrderByDescending(b => b.CreatedDate).Take(200).ToListAsync();
            return View(new TpaClaim { ClaimDate = DateTime.Today });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClaim(TpaClaim model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Providers = await _context.TpaProviders.Where(p => p.IsActive).ToListAsync();
                ViewBag.Patients = await _context.Patients.ToListAsync();
                ViewBag.Bills = await _context.Bills.OrderByDescending(b => b.CreatedDate).Take(200).ToListAsync();
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.ClaimNumber))
                model.ClaimNumber = $"TPA-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            _context.TpaClaims.Add(model);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier),
                "CREATE", "TpaClaim", model.Id.ToString(), null,
                $"Claim {model.ClaimNumber}, Patient {model.PatientId}");
            TempData["SuccessMessage"] = "TPA claim created.";
            return RedirectToAction(nameof(Claims));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateClaimStatus(int id, string status, decimal? approvedAmount, decimal? settledAmount)
        {
            var claim = await _context.TpaClaims.FindAsync(id);
            if (claim == null) return NotFound();

            claim.Status = status;
            if (approvedAmount.HasValue) claim.ApprovedAmount = approvedAmount;
            if (settledAmount.HasValue)
            {
                claim.SettledAmount = settledAmount;
                claim.SettlementDate = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Claim status updated.";
            return RedirectToAction(nameof(Claims));
        }
    }
}
