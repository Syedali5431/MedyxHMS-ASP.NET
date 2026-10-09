using System.Security.Claims;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Controllers
{
    /// <summary>Internal audit programme: plan audits, record findings, raise CAPA from findings, complete with a summary.</summary>
    [Authorize(Roles = QualityService.ManagerRoles)]
    public class InternalAuditController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly QualityService _quality;
        private readonly IAuditService _audit;

        public InternalAuditController(ApplicationDbContext context, QualityService quality, IAuditService audit)
        {
            _context = context;
            _quality = quality;
            _audit = audit;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        public async Task<IActionResult> Index(string? status)
        {
            var query = _context.InternalAudits.AsNoTracking().Include(a => a.Findings).AsQueryable();
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);
            ViewBag.Status = status;
            return View(await query.OrderByDescending(a => a.PlannedDate).ToListAsync());
        }

        public IActionResult Create()
        {
            return View(new InternalAudit());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Scope,Standard,PlannedDate,LeadAuditor")] InternalAudit model)
        {
            if (!QualityService.AuditStandards.Contains(model.Standard)) ModelState.AddModelError(nameof(model.Standard), "Choose a standard.");
            ModelState.Remove(nameof(InternalAudit.Findings));
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.AuditNumber = await _quality.NextAuditNumberAsync();
            model.Status = "Planned";
            model.CreatedAt = DateTime.Now;
            model.CreatedBy = User.Identity?.Name ?? string.Empty;
            _context.InternalAudits.Add(model);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "CREATE", "InternalAudit", model.Id.ToString(), null, $"{model.AuditNumber} {model.Title} planned {model.PlannedDate:yyyy-MM-dd}");
            TempData["SuccessMessage"] = $"Audit {model.AuditNumber} planned.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        public async Task<IActionResult> Details(int id)
        {
            var audit = await _context.InternalAudits.AsNoTracking().Include(a => a.Findings).FirstOrDefaultAsync(a => a.Id == id);
            if (audit == null) return NotFound();

            var findingIds = audit.Findings.Select(f => f.Id).ToList();
            ViewBag.Capa = await _context.CapaActions.AsNoTracking().Where(c => c.AuditFindingId != null && findingIds.Contains(c.AuditFindingId.Value)).ToListAsync();
            ViewBag.Staff = await _quality.GetStaffOptionsAsync();
            return View(audit);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int id)
        {
            var audit = await _context.InternalAudits.FirstOrDefaultAsync(a => a.Id == id);
            if (audit == null) return NotFound();
            if (audit.Status == "Planned")
            {
                audit.Status = "In progress";
                await _context.SaveChangesAsync();
                await _audit.LogActivityAsync(UserId, "START", "InternalAudit", id.ToString(), "Planned", "In progress");
                TempData["SuccessMessage"] = "Audit started – record the findings below.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFinding(int id, string findingType, string? clause, string description)
        {
            var audit = await _context.InternalAudits.FirstOrDefaultAsync(a => a.Id == id);
            if (audit == null) return NotFound();
            if (audit.Status != "In progress")
            {
                TempData["ErrorMessage"] = "Start the audit before recording findings (completed audits cannot be changed).";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!QualityService.FindingTypes.Contains(findingType) || string.IsNullOrWhiteSpace(description))
            {
                TempData["ErrorMessage"] = "Choose the finding type and describe the finding.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var finding = new AuditFinding
            {
                AuditId = id,
                FindingType = findingType,
                Clause = (clause ?? string.Empty).Trim(),
                Description = description.Trim(),
                // Observations and improvement opportunities need no corrective action.
                Status = findingType.Contains("non-conformity") ? "Open" : "Closed",
                CreatedAt = DateTime.Now
            };
            _context.AuditFindings.Add(finding);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "CREATE", "AuditFinding", finding.Id.ToString(), null, $"{audit.AuditNumber}: {findingType} {finding.Clause} – {finding.Description}");
            TempData["SuccessMessage"] = finding.Status == "Open" ? "Non-conformity recorded – raise a corrective action for it." : "Finding recorded.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? summary)
        {
            var audit = await _context.InternalAudits.FirstOrDefaultAsync(a => a.Id == id);
            if (audit == null) return NotFound();
            if (audit.Status != "In progress" || string.IsNullOrWhiteSpace(summary))
            {
                TempData["ErrorMessage"] = audit.Status != "In progress" ? "Only an audit in progress can be completed." : "Write the audit summary / conclusion.";
                return RedirectToAction(nameof(Details), new { id });
            }

            audit.Status = "Completed";
            audit.CompletedDate = DateTime.Today;
            audit.Summary = summary.Trim();
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "COMPLETE", "InternalAudit", id.ToString(), null, $"{audit.AuditNumber}: {audit.Summary}");
            TempData["SuccessMessage"] = $"Audit {audit.AuditNumber} completed. Open non-conformities stay tracked through their actions.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
