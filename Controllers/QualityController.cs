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
    /// <summary>Quality management: dashboard, incident reporting/investigation and CAPA (corrective and preventive actions).</summary>
    [Authorize(Roles = QualityService.StaffRoles)]
    public class QualityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly QualityService _quality;
        private readonly IAuditService _audit;

        public QualityController(ApplicationDbContext context, QualityService quality, IAuditService audit)
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

        // ── Dashboard ─────────────────────────────────────────────

        [Authorize(Roles = QualityService.ManagerRoles)]
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var model = new QualityDashboardViewModel
            {
                OpenIncidents = await _context.QualityIncidents.CountAsync(i => i.Status != "Closed"),
                CriticalOpenIncidents = await _context.QualityIncidents.CountAsync(i => i.Status != "Closed" && (i.Severity == "High" || i.Severity == "Critical")),
                IncidentsThisMonth = await _context.QualityIncidents.CountAsync(i => i.ReportedAt >= new DateTime(today.Year, today.Month, 1)),
                OpenCapa = await _context.CapaActions.CountAsync(c => c.Status == "Open" || c.Status == "In progress"),
                OverdueCapa = await _context.CapaActions.CountAsync(c => (c.Status == "Open" || c.Status == "In progress") && c.DueDate < today),
                CapaAwaitingVerification = await _context.CapaActions.CountAsync(c => c.Status == "Completed"),
                DocumentsDueForReview = await _context.ControlledDocuments.CountAsync(d => d.Status == "Effective" && d.NextReviewDate != null && d.NextReviewDate <= today.AddDays(30)),
                DocumentsPendingApproval = await _context.DocumentVersions.CountAsync(v => v.Status == "Pending approval"),
                UpcomingAudits = await _context.InternalAudits.CountAsync(a => a.Status != "Completed"),
                OpenFindings = await _context.AuditFindings.CountAsync(f => f.Status != "Closed"),
                TrainingExpiringSoon = await _context.TrainingRecords.CountAsync(t => t.ExpiryDate != null && t.ExpiryDate >= today && t.ExpiryDate <= today.AddDays(30)),
                TrainingExpired = await _context.TrainingRecords.CountAsync(t => t.ExpiryDate != null && t.ExpiryDate < today),
                EquipmentOverdue = await _context.Equipment.CountAsync(e => e.Status != "Condemned" && ((e.NextMaintenanceDue != null && e.NextMaintenanceDue < today) || (e.NextCalibrationDue != null && e.NextCalibrationDue < today))),
                EquipmentDueSoon = await _context.Equipment.CountAsync(e => e.Status != "Condemned" && ((e.NextMaintenanceDue != null && e.NextMaintenanceDue >= today && e.NextMaintenanceDue <= today.AddDays(30)) || (e.NextCalibrationDue != null && e.NextCalibrationDue >= today && e.NextCalibrationDue <= today.AddDays(30)))),
                EquipmentOutOfService = await _context.Equipment.CountAsync(e => e.Status == "Out of service" || e.Status == "Under maintenance"),
                RecentIncidents = await _context.QualityIncidents.AsNoTracking().OrderByDescending(i => i.ReportedAt).Take(8).ToListAsync(),
                OverdueCapaList = await _context.CapaActions.AsNoTracking().Include(c => c.Incident)
                    .Where(c => (c.Status == "Open" || c.Status == "In progress") && c.DueDate < today).OrderBy(c => c.DueDate).Take(8).ToListAsync()
            };
            return View(model);
        }

        // ── Incidents ─────────────────────────────────────────────

        public async Task<IActionResult> Incidents(string? status, string? severity)
        {
            var query = _context.QualityIncidents.AsNoTracking().AsQueryable();
            if (!IsManager)
            {
                // Staff see the incidents they reported; managers see all.
                query = query.Where(i => i.ReportedByUserId == UserId);
            }

            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(i => i.Status == status);
            if (!string.IsNullOrWhiteSpace(severity)) query = query.Where(i => i.Severity == severity);

            ViewBag.Status = status;
            ViewBag.Severity = severity;
            ViewBag.IsManager = IsManager;
            return View(await query.OrderByDescending(i => i.ReportedAt).Take(500).ToListAsync());
        }

        public IActionResult ReportIncident()
        {
            return View(new QualityIncident { OccurredAt = DateTime.Now });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportIncident([Bind("Title,Category,Severity,OccurredAt,Location,Description,ImmediateAction,PatientId")] QualityIncident model)
        {
            if (!QualityService.IncidentCategories.Contains(model.Category)) ModelState.AddModelError(nameof(model.Category), "Choose a category.");
            if (!QualityService.Severities.Contains(model.Severity)) ModelState.AddModelError(nameof(model.Severity), "Choose a severity.");
            if (model.OccurredAt > DateTime.Now.AddMinutes(5)) ModelState.AddModelError(nameof(model.OccurredAt), "The date cannot be in the future.");
            if (model.PatientId.HasValue && !await _context.Patients.AnyAsync(p => p.Id == model.PatientId)) ModelState.AddModelError(nameof(model.PatientId), "Unknown patient.");
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.IncidentNumber = await _quality.NextIncidentNumberAsync();
            model.ReportedAt = DateTime.Now;
            model.ReportedByUserId = UserId;
            model.ReportedByName = await CurrentUserNameAsync();
            model.Status = "Open";
            _context.QualityIncidents.Add(model);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "CREATE", "QualityIncident", model.Id.ToString(), null, $"{model.IncidentNumber} {model.Severity}: {model.Title}");

            TempData["SuccessMessage"] = $"Incident {model.IncidentNumber} reported. Thank you – the quality team will review it.";
            return RedirectToAction(nameof(Incident), new { id = model.Id });
        }

        public async Task<IActionResult> Incident(int id)
        {
            var incident = await _context.QualityIncidents.AsNoTracking()
                .Include(i => i.Patient)
                .Include(i => i.CapaActions)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (incident == null) return NotFound();
            // Managers, the reporter and the owners of its actions may open an incident.
            if (!IsManager && incident.ReportedByUserId != UserId && !incident.CapaActions.Any(c => c.OwnerUserId == UserId)) return Forbid();

            ViewBag.IsManager = IsManager;
            ViewBag.Staff = IsManager ? await _quality.GetStaffOptionsAsync() : new List<StaffOption>();
            ViewBag.UserId = UserId;
            return View(incident);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = QualityService.ManagerRoles)]
        public async Task<IActionResult> UpdateInvestigation(int id, string status, string? rootCause)
        {
            var incident = await _context.QualityIncidents.FirstOrDefaultAsync(i => i.Id == id);
            if (incident == null) return NotFound();
            if (incident.Status == "Closed")
            {
                TempData["ErrorMessage"] = "This incident is closed.";
                return RedirectToAction(nameof(Incident), new { id });
            }

            if (!new[] { "Open", "Under investigation", "CAPA in progress" }.Contains(status))
            {
                TempData["ErrorMessage"] = "Choose a valid status (use Close incident to close it).";
                return RedirectToAction(nameof(Incident), new { id });
            }

            var old = $"{incident.Status}";
            incident.Status = status;
            incident.RootCause = (rootCause ?? string.Empty).Trim();
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "UPDATE", "QualityIncident", id.ToString(), old, $"{status}; root cause: {incident.RootCause}");
            TempData["SuccessMessage"] = "Investigation updated.";
            return RedirectToAction(nameof(Incident), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = QualityService.ManagerRoles)]
        public async Task<IActionResult> AddCapa(int? incidentId, int? findingId, string actionType, string description, string ownerUserId, DateTime? dueDate)
        {
            var back = incidentId.HasValue
                ? RedirectToAction(nameof(Incident), new { id = incidentId })
                : (IActionResult)RedirectToAction("Details", "InternalAudit", new { id = await _context.AuditFindings.Where(f => f.Id == findingId).Select(f => (int?)f.AuditId).FirstOrDefaultAsync() });

            QualityIncident? incident = null;
            AuditFinding? finding = null;
            if (incidentId.HasValue)
            {
                incident = await _context.QualityIncidents.FirstOrDefaultAsync(i => i.Id == incidentId);
                if (incident == null) return NotFound();
            }
            else if (findingId.HasValue)
            {
                finding = await _context.AuditFindings.Include(f => f.Audit).FirstOrDefaultAsync(f => f.Id == findingId);
                if (finding == null) return NotFound();
            }
            else
            {
                return BadRequest();
            }

            var owner = (await _quality.GetStaffOptionsAsync()).FirstOrDefault(s => s.Id == ownerUserId);
            var errors = new List<string>();
            if (actionType != "Corrective" && actionType != "Preventive") errors.Add("Choose Corrective or Preventive.");
            if (string.IsNullOrWhiteSpace(description)) errors.Add("Describe the action.");
            if (owner == null) errors.Add("Choose who is responsible.");
            if (!dueDate.HasValue || dueDate.Value.Date < DateTime.Today) errors.Add("Choose a due date from today onwards.");
            if (incident?.Status == "Closed") errors.Add("The incident is closed.");
            if (errors.Count > 0)
            {
                TempData["ErrorMessage"] = string.Join(" ", errors);
                return back;
            }

            var capa = new CapaAction
            {
                IncidentId = incident?.Id,
                AuditFindingId = finding?.Id,
                HospitalId = incident?.HospitalId ?? finding?.Audit.HospitalId,
                ActionType = actionType,
                Description = description.Trim(),
                OwnerUserId = owner!.Id,
                OwnerName = owner.Name,
                DueDate = dueDate!.Value.Date,
                Status = "Open",
                CreatedAt = DateTime.Now,
                CreatedBy = await CurrentUserNameAsync()
            };
            _context.CapaActions.Add(capa);
            if (incident != null && incident.Status == "Open") incident.Status = "CAPA in progress";
            if (finding != null) finding.Status = "CAPA raised";
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "CREATE", "CapaAction", capa.Id.ToString(), null, $"{capa.ActionType} for {(incident != null ? incident.IncidentNumber : "finding " + finding!.Id)}: {capa.Description} (owner {capa.OwnerName}, due {capa.DueDate:yyyy-MM-dd})");
            TempData["SuccessMessage"] = $"{actionType} action assigned to {owner.Name}.";
            return back;
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = QualityService.ManagerRoles)]
        public async Task<IActionResult> CloseIncident(int id, string? closureNotes)
        {
            var incident = await _context.QualityIncidents.Include(i => i.CapaActions).FirstOrDefaultAsync(i => i.Id == id);
            if (incident == null) return NotFound();

            var unverified = incident.CapaActions.Count(c => c.Status != "Verified");
            if (unverified > 0 || string.IsNullOrWhiteSpace(closureNotes) || string.IsNullOrWhiteSpace(incident.RootCause))
            {
                TempData["ErrorMessage"] = unverified > 0
                    ? $"{unverified} action(s) are not verified yet. Verify all corrective/preventive actions before closing."
                    : string.IsNullOrWhiteSpace(incident.RootCause) ? "Record the investigation / root cause before closing." : "Enter closure notes.";
                return RedirectToAction(nameof(Incident), new { id });
            }

            incident.Status = "Closed";
            incident.ClosedAt = DateTime.Now;
            incident.ClosedBy = await CurrentUserNameAsync();
            incident.ClosureNotes = closureNotes!.Trim();
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "CLOSE", "QualityIncident", id.ToString(), null, $"{incident.IncidentNumber} closed: {incident.ClosureNotes}");
            TempData["SuccessMessage"] = $"Incident {incident.IncidentNumber} closed.";
            return RedirectToAction(nameof(Incident), new { id });
        }

        // ── CAPA ──────────────────────────────────────────────────

        public async Task<IActionResult> Capa(string? filter)
        {
            var query = _context.CapaActions.AsNoTracking().Include(c => c.Incident).Include(c => c.AuditFinding).AsQueryable();
            if (!IsManager)
            {
                query = query.Where(c => c.OwnerUserId == UserId);
            }

            var today = DateTime.Today;
            query = filter switch
            {
                "open" => query.Where(c => c.Status == "Open" || c.Status == "In progress"),
                "overdue" => query.Where(c => (c.Status == "Open" || c.Status == "In progress") && c.DueDate < today),
                "verify" => query.Where(c => c.Status == "Completed"),
                _ => query
            };

            ViewBag.Filter = filter;
            ViewBag.IsManager = IsManager;
            ViewBag.UserId = UserId;
            return View(await query.OrderBy(c => c.Status == "Verified").ThenBy(c => c.DueDate).Take(500).ToListAsync());
        }

        /// <summary>The owner (or a manager) records progress or completion.</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCapa(int id, string status, string? notes, string? returnUrl)
        {
            var capa = await _context.CapaActions.FirstOrDefaultAsync(c => c.Id == id);
            if (capa == null) return NotFound();
            if (!IsManager && capa.OwnerUserId != UserId) return Forbid();

            if (capa.Status == "Verified" || (status != "In progress" && status != "Completed"))
            {
                TempData["ErrorMessage"] = capa.Status == "Verified" ? "This action is already verified." : "Choose In progress or Completed.";
                return RedirectToLocal(returnUrl);
            }

            if (status == "Completed" && string.IsNullOrWhiteSpace(notes))
            {
                TempData["ErrorMessage"] = "Describe what was done before marking the action completed.";
                return RedirectToLocal(returnUrl);
            }

            var old = capa.Status;
            capa.Status = status;
            if (!string.IsNullOrWhiteSpace(notes)) capa.CompletionNotes = notes.Trim();
            capa.CompletedAt = status == "Completed" ? DateTime.Now : null;
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "UPDATE", "CapaAction", id.ToString(), old, $"{status}: {capa.CompletionNotes}");
            TempData["SuccessMessage"] = status == "Completed" ? "Action marked completed – waiting for verification." : "Action updated.";
            return RedirectToLocal(returnUrl);
        }

        /// <summary>A manager confirms the completed action was effective.</summary>
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = QualityService.ManagerRoles)]
        public async Task<IActionResult> VerifyCapa(int id, string? effectivenessNotes, string? returnUrl)
        {
            var capa = await _context.CapaActions.Include(c => c.AuditFinding).FirstOrDefaultAsync(c => c.Id == id);
            if (capa == null) return NotFound();
            if (capa.Status != "Completed")
            {
                TempData["ErrorMessage"] = "Only completed actions can be verified.";
                return RedirectToLocal(returnUrl);
            }

            if (string.IsNullOrWhiteSpace(effectivenessNotes))
            {
                TempData["ErrorMessage"] = "Record how the effectiveness was checked.";
                return RedirectToLocal(returnUrl);
            }

            capa.Status = "Verified";
            capa.VerifiedAt = DateTime.Now;
            capa.VerifiedBy = await CurrentUserNameAsync();
            capa.EffectivenessNotes = effectivenessNotes.Trim();

            // A finding is closed once all of its actions are verified.
            if (capa.AuditFinding != null)
            {
                var open = await _context.CapaActions.CountAsync(c => c.AuditFindingId == capa.AuditFindingId && c.Id != capa.Id && c.Status != "Verified");
                if (open == 0) capa.AuditFinding.Status = "Closed";
            }

            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "VERIFY", "CapaAction", id.ToString(), null, capa.EffectivenessNotes);
            TempData["SuccessMessage"] = "Action verified as effective.";
            return RedirectToLocal(returnUrl);
        }

        private IActionResult RedirectToLocal(string? returnUrl) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Capa));
    }
}
