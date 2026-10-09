using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MedyxHMS.Controllers
{
    [Authorize(Roles = BedManagementViewRoles)]
    public class BedManagementController : Controller
    {
        private const string BedManagementViewRoles = "SuperAdmin,Admin,Doctor,Nurse,Pharmacist,Accountant,Receptionist,LabTechnician,Radiologist,Staff";
        private const string BedManagementManageRoles = "SuperAdmin,Admin,Nurse";

        private readonly ApplicationDbContext _context;
        private readonly IBedService _bedService;
        private readonly IAuditService _audit;

        public BedManagementController(
            ApplicationDbContext context,
            IBedService bedService,
            IAuditService audit)
        {
            _context = context;
            _bedService = bedService;
            _audit = audit;
        }

        // ── GET: /BedManagement and /bed-management ─────────────
        [HttpGet("/BedManagement")]
        [HttpGet("/BedManagement/Index")]
        [HttpGet("/bed-management")]
        public async Task<IActionResult> Index()
        {
            var beds = await _context.Beds
                .Include(b => b.Ward)
                .Include(b => b.Patient)
                .Where(b => b.IsActive)
                .OrderBy(b => b.Block)
                .ThenBy(b => b.Floor)
                .ThenBy(b => b.Ward.Name)
                .ThenBy(b => b.RoomNumber)
                .ThenBy(b => b.BedNumber)
                .ToListAsync();

            var summary = await _bedService.GetBedManagementSummaryAsync();
            ViewBag.Summary = summary;

            var wards = await _context.Wards
                .Where(w => w.IsActive)
                .OrderBy(w => w.Name)
                .ToListAsync();
            ViewBag.Wards = wards;

            // Distinct location values for filter dropdowns (populated client-side from JSON too,
            // but pre-populating here keeps the dropdowns stable even before JS runs)
            ViewBag.Blocks = await _context.Beds
                .Where(b => b.IsActive && b.Block != null && b.Block != "")
                .Select(b => b.Block).Distinct().OrderBy(x => x).ToListAsync();
            ViewBag.Floors = await _context.Beds
                .Where(b => b.IsActive && b.Floor != null && b.Floor != "")
                .Select(b => b.Floor).Distinct().OrderBy(x => x).ToListAsync();
            ViewBag.Rooms = await _context.Beds
                .Where(b => b.IsActive && b.RoomNumber != null && b.RoomNumber != "")
                .Select(b => b.RoomNumber).Distinct().OrderBy(x => x).ToListAsync();

            // Beds held by an IPD admission: their status follows the admission (discharge / transfer).
            var admitted = await _context.IPDAdmissions.AsNoTracking()
                .Where(a => a.Status == "Admitted" && a.BedId != null)
                .Select(a => new { BedId = a.BedId!.Value, a.Id })
                .ToListAsync();
            ViewBag.Admissions = admitted.GroupBy(a => a.BedId).ToDictionary(g => g.Key, g => g.Max(a => a.Id));

            var availablePatients = await _context.Patients
                .Where(p => p.IsActive)
                .OrderBy(p => p.FirstName)
                .Select(p => new { p.Id, Name = p.FirstName + " " + p.LastName, p.PatientId })
                .ToListAsync();
            ViewBag.Patients = availablePatients;

            return View(beds);
        }

        // ── API: POST /BedManagement/Assign ─────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = BedManagementManageRoles)]
        public async Task<IActionResult> Assign(int bedId, int patientId)
        {
            // Determine the most-privileged role for ICU check
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            var effectiveRole = roles.Contains("SuperAdmin") ? "SuperAdmin"
                             : roles.Contains("Admin") ? "Admin"
                             : roles.FirstOrDefault() ?? string.Empty;

            var (ok, error) = await _bedService.AssignBedAsync(bedId, patientId, effectiveRole);
            if (!ok)
            {
                TempData["ErrorMessage"] = error;
                return RedirectToAction(nameof(Index));
            }

            var bed = await _context.Beds.Include(b => b.Ward).FirstOrDefaultAsync(b => b.Id == bedId);
            await _audit.LogActivityAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                "ASSIGN", "Bed", bedId.ToString(), null,
                $"Bed {bed?.BedNumber} ({bed?.Ward?.Name}) assigned to patient {patientId}");

            TempData["SuccessMessage"] = $"Bed {bed?.BedNumber} assigned successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ── API: POST /BedManagement/Release ────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = BedManagementManageRoles)]
        public async Task<IActionResult> Release(int bedId)
        {
            if (await ActiveAdmissionMessageAsync(bedId) is { } admittedMessage)
            {
                TempData["ErrorMessage"] = admittedMessage;
                return RedirectToAction(nameof(Index));
            }

            var (ok, error) = await _bedService.ReleaseBedAsync(bedId);
            if (!ok)
            {
                TempData["ErrorMessage"] = error;
                return RedirectToAction(nameof(Index));
            }

            var bed = await _context.Beds.Include(b => b.Ward).FirstOrDefaultAsync(b => b.Id == bedId);
            await _audit.LogActivityAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                "RELEASE", "Bed", bedId.ToString(), null,
                $"Bed {bed?.BedNumber} ({bed?.Ward?.Name}) released \u2192 Cleaning");

            TempData["SuccessMessage"] = $"Bed {bed?.BedNumber} released and set to Cleaning.";
            return RedirectToAction(nameof(Index));
        }

        // ── API: POST /BedManagement/Transfer ───────────────────
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = BedManagementManageRoles)]
        public async Task<IActionResult> Transfer(int fromBedId, int toBedId)
        {
            var (ok, error) = await _bedService.TransferBedAsync(fromBedId, toBedId);
            if (!ok)
            {
                TempData["ErrorMessage"] = error;
                return RedirectToAction(nameof(Index));
            }

            await _audit.LogActivityAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                "TRANSFER", "Bed", fromBedId.ToString(), null,
                $"Patient transferred from bed {fromBedId} to bed {toBedId}");

            TempData["SuccessMessage"] = "Patient transferred successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ── API: POST /BedManagement/SetStatus ──────────────────
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = BedManagementManageRoles)]
        public async Task<IActionResult> SetStatus(int bedId, string status)
        {
            // Validate allowed statuses
            var allowed = new[] { "Available", "Cleaning", "Maintenance", "Blocked" };
            if (!allowed.Contains(status))
            {
                TempData["ErrorMessage"] = "Invalid status value.";
                return RedirectToAction(nameof(Index));
            }

            var bed = await _context.Beds.FindAsync(bedId);
            if (bed == null) return NotFound();

            if (bed.Status == "Occupied" && status != "Available")
            {
                TempData["ErrorMessage"] = "Cannot change status of an occupied bed. Release the bed first.";
                return RedirectToAction(nameof(Index));
            }
            if (bed.Status == "Occupied" && await ActiveAdmissionMessageAsync(bedId) is { } admittedMessage)
            {
                TempData["ErrorMessage"] = admittedMessage;
                return RedirectToAction(nameof(Index));
            }

            var old = bed.Status;
            bed.Status = status;
            bed.LastUpdated = DateTime.Now;
            // If moving to Available, clear any stale patient reference
            if (status == "Available") bed.PatientId = null;
            await _context.SaveChangesAsync();

            await _audit.LogActivityAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                "STATUS_CHANGE", "Bed", bedId.ToString(), old, status);

            TempData["SuccessMessage"] = $"Bed status updated to {status}.";
            return RedirectToAction(nameof(Index));
        }

        // ── POST /BedManagement/ChangeStatus (right-click menu on a bed) ──
        // Statuses: Available, Occupied (needs a patient), Cleaning, Maintenance, Blocked ("Maint./Blocked").
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = BedManagementManageRoles)]
        public async Task<IActionResult> ChangeStatus(int bedId, string status, int? patientId)
        {
            var allowed = new[] { "Available", "Occupied", "Cleaning", "Maintenance", "Blocked" };
            status = allowed.FirstOrDefault(s => string.Equals(s, (status ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            if (status.Length == 0)
                return BadRequest(new { success = false, error = "Choose Available, Occupied, Cleaning or Maint./Blocked." });

            var bed = await _context.Beds.Include(b => b.Ward).Include(b => b.Patient).FirstOrDefaultAsync(b => b.Id == bedId && b.IsActive);
            if (bed == null)
                return NotFound(new { success = false, error = "Bed not found." });

            var old = bed.Status;
            if (string.Equals(old, status, StringComparison.OrdinalIgnoreCase))
                return Ok(BedStatusResult(bed, $"Bed {bed.BedNumber} is already {StatusLabel(status)}."));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (status == "Occupied")
            {
                if (patientId is null or <= 0)
                    return BadRequest(new { success = false, error = "Choose the patient who occupies the bed." });
                if (!await _context.Patients.AnyAsync(p => p.Id == patientId && p.IsActive))
                    return BadRequest(new { success = false, error = "Patient not found." });

                // A bed that is being cleaned or repaired can be taken straight into use from this menu.
                if (old != "Available")
                {
                    bed.Status = "Available";
                }

                var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
                var effectiveRole = roles.Contains("SuperAdmin") ? "SuperAdmin" : roles.Contains("Admin") ? "Admin" : roles.FirstOrDefault() ?? string.Empty;
                var (ok, error) = await _bedService.AssignBedAsync(bedId, patientId.Value, effectiveRole);
                if (!ok)
                {
                    bed.Status = old;
                    return BadRequest(new { success = false, error });
                }

                await _context.Entry(bed).Reference(b => b.Patient).LoadAsync();
                await _audit.LogActivityAsync(userId, "STATUS_CHANGE", "Bed", bedId.ToString(), old,
                    $"Occupied – patient {bed.Patient?.PatientId} {bed.Patient?.FirstName} {bed.Patient?.LastName}".Trim());
                return Ok(BedStatusResult(bed, $"Bed {bed.BedNumber} marked Occupied ({bed.Patient?.FirstName} {bed.Patient?.LastName})."));
            }

            if (old == "Occupied")
            {
                // An admitted patient keeps the bed until discharge or transfer, so the admission stays correct.
                var admitted = await ActiveAdmissionMessageAsync(bedId);
                if (admitted != null)
                    return BadRequest(new { success = false, error = admitted });

                bed.PatientId = null;
            }

            bed.Status = status;
            bed.LastUpdated = DateTime.Now;
            if (status == "Available")
                bed.PatientId = null;
            await _context.SaveChangesAsync();

            await _audit.LogActivityAsync(userId, "STATUS_CHANGE", "Bed", bedId.ToString(), old, status);
            return Ok(BedStatusResult(bed, $"Bed {bed.BedNumber} marked {StatusLabel(status)}."));
        }

        /// <summary>Message when an IPD admission still holds the bed (null when none).</summary>
        private async Task<string?> ActiveAdmissionMessageAsync(int bedId)
        {
            var admission = await _context.IPDAdmissions.AsNoTracking()
                .Where(a => a.BedId == bedId && a.Status == "Admitted")
                .Select(a => new { a.Id, a.Patient.FirstName, a.Patient.LastName })
                .FirstOrDefaultAsync();
            return admission == null
                ? null
                : $"{admission.FirstName} {admission.LastName} is admitted in this bed (IPD admission #{admission.Id}). Discharge the patient or move them to another bed first.";
        }

        private static string StatusLabel(string status) => status is "Maintenance" or "Blocked" ? $"Maint./Blocked ({status})" : status;

        private static object BedStatusResult(Bed bed, string message) => new
        {
            success = true,
            message,
            bed = new
            {
                id = bed.Id,
                status = bed.Status,
                patientId = bed.PatientId,
                patientName = bed.Patient != null && bed.PatientId != null ? (bed.Patient.FirstName + " " + bed.Patient.LastName) : null
            }
        };

        // ── API: GET /api/beds ─────────────────────────────────
        [HttpGet("/api/beds")]
        public async Task<IActionResult> GetBedsApi()
        {
            var beds = await _context.Beds
                .Include(b => b.Ward)
                .Include(b => b.Patient)
                .Where(b => b.IsActive)
                .OrderBy(b => b.Block)
                .ThenBy(b => b.Floor)
                .ThenBy(b => b.Ward.Name)
                .ThenBy(b => b.RoomNumber)
                .ThenBy(b => b.BedNumber)
                .Select(b => new
                {
                    b.Id,
                    b.BedNumber,
                    b.Block,
                    b.Floor,
                    b.RoomNumber,
                    Ward = b.Ward.Name,
                    b.WardId,
                    b.BedType,
                    b.Status,
                    b.PatientId,
                    PatientName = b.Patient != null ? (b.Patient.FirstName + " " + b.Patient.LastName) : null,
                    b.IsIsolation,
                    b.RequiresAdminApproval,
                    b.LastUpdated
                })
                .ToListAsync();

            return Ok(beds);
        }

        // ── API: POST /api/beds/assign ─────────────────────────
        [HttpPost("/api/beds/assign")]
        [Authorize(Roles = BedManagementManageRoles)]
        public async Task<IActionResult> AssignBedApi([FromBody] AssignBedRequest request)
        {
            if (request.BedId <= 0 || request.PatientId <= 0)
                return BadRequest(new { success = false, error = "Invalid request payload." });

            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            var effectiveRole = roles.Contains("SuperAdmin") ? "SuperAdmin"
                             : roles.Contains("Admin") ? "Admin"
                             : roles.FirstOrDefault() ?? string.Empty;

            var (ok, error) = await _bedService.AssignBedAsync(request.BedId, request.PatientId, effectiveRole);
            if (!ok)
                return BadRequest(new { success = false, error });

            await _audit.LogActivityAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                "ASSIGN", "Bed", request.BedId.ToString(), null,
                $"API bed assignment to patient {request.PatientId}");

            return Ok(new { success = true });
        }

        // ── API: POST /api/beds/release ────────────────────────
        [HttpPost("/api/beds/release")]
        [Authorize(Roles = BedManagementManageRoles)]
        public async Task<IActionResult> ReleaseBedApi([FromBody] ReleaseBedRequest request)
        {
            if (request.BedId <= 0)
                return BadRequest(new { success = false, error = "Invalid request payload." });

            var (ok, error) = await _bedService.ReleaseBedAsync(request.BedId);
            if (!ok)
                return BadRequest(new { success = false, error });

            await _audit.LogActivityAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                "RELEASE", "Bed", request.BedId.ToString(), null,
                "API bed release");

            return Ok(new { success = true });
        }

        // ── API: POST /api/beds/transfer ───────────────────────
        [HttpPost("/api/beds/transfer")]
        [Authorize(Roles = BedManagementManageRoles)]
        public async Task<IActionResult> TransferBedApi([FromBody] TransferBedRequest request)
        {
            if (request.FromBedId <= 0 || request.ToBedId <= 0)
                return BadRequest(new { success = false, error = "Invalid request payload." });

            var (ok, error) = await _bedService.TransferBedAsync(request.FromBedId, request.ToBedId);
            if (!ok)
                return BadRequest(new { success = false, error });

            await _audit.LogActivityAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                "TRANSFER", "Bed", request.FromBedId.ToString(), null,
                $"API bed transfer {request.FromBedId} -> {request.ToBedId}");

            return Ok(new { success = true });
        }

        // ── API: POST /api/beds/status ─────────────────────────
        [HttpPost("/api/beds/status")]
        [Authorize(Roles = BedManagementManageRoles)]
        public async Task<IActionResult> UpdateBedStatusApi([FromBody] UpdateBedStatusRequest request)
        {
            var allowed = new[] { "Available", "Cleaning", "Maintenance", "Blocked" };
            if (request.BedId <= 0 || string.IsNullOrWhiteSpace(request.Status) || !allowed.Contains(request.Status))
                return BadRequest(new { success = false, error = "Invalid request payload." });

            var bed = await _context.Beds.FindAsync(request.BedId);
            if (bed == null)
                return NotFound(new { success = false, error = "Bed not found." });

            if (bed.Status == "Occupied" && request.Status != "Available")
                return BadRequest(new { success = false, error = "Cannot change status of an occupied bed. Release the bed first." });
            if (bed.Status == "Occupied" && await ActiveAdmissionMessageAsync(request.BedId) is { } admittedMessage)
                return BadRequest(new { success = false, error = admittedMessage });

            var old = bed.Status;
            bed.Status = request.Status;
            bed.LastUpdated = DateTime.Now;
            if (request.Status == "Available")
                bed.PatientId = null;

            await _context.SaveChangesAsync();

            await _audit.LogActivityAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                "STATUS_CHANGE", "Bed", request.BedId.ToString(), old, request.Status);

            return Ok(new { success = true });
        }

        // ── Admin-only: Create new bed(s) ──────────────────────
        [Authorize(Roles = BedManagementManageRoles)]
        public async Task<IActionResult> Create()
        {
            ViewBag.Wards = await _context.Wards.Where(w => w.IsActive).OrderBy(w => w.Name).ToListAsync();
            return View(new Bed());
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = BedManagementManageRoles)]
        public async Task<IActionResult> Create(Bed model, int numberOfBeds = 1)
        {
            // Remove BedNumber from validation — it is auto-generated for bulk creates
            ModelState.Remove(nameof(Bed.BedNumber));

            // Normalize location fields to keep room grouping and sequencing consistent.
            model.Block = (model.Block ?? string.Empty).Trim();
            model.Floor = (model.Floor ?? string.Empty).Trim();
            model.RoomNumber = (model.RoomNumber ?? string.Empty).Trim();

            // Wards are per hospital: only wards of the active hospital can be chosen.
            if (!await _context.Wards.AnyAsync(w => w.Id == model.WardId && w.IsActive))
            {
                ModelState.AddModelError(nameof(Bed.WardId), "Please choose a ward.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Wards = await _context.Wards.Where(w => w.IsActive).OrderBy(w => w.Name).ToListAsync();
                return View(model);
            }

            numberOfBeds = Math.Clamp(numberOfBeds, 1, 50);

            // Determine how many beds already exist in this room so we continue numbering
            var existingCount = await _context.Beds
                .CountAsync(b => b.WardId == model.WardId
                              && b.Block == model.Block
                              && b.Floor == model.Floor
                              && b.RoomNumber == model.RoomNumber);

            var created = new List<Bed>();
            for (int i = 1; i <= numberOfBeds; i++)
            {
                var bed = new Bed
                {
                    WardId = model.WardId,
                    Block = model.Block,
                    Floor = model.Floor,
                    RoomNumber = model.RoomNumber,
                    BedNumber = string.IsNullOrWhiteSpace(model.RoomNumber)
                        ? $"B{(existingCount + i):D2}"
                        : $"{model.RoomNumber}-B{(existingCount + i):D2}",
                    BedType = model.BedType,
                    DailyCharges = model.DailyCharges,
                    IsActive = model.IsActive,
                    Status = "Available",
                    CreatedDate = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    RequiresAdminApproval = model.BedType == "ICU",
                    IsIsolation = model.BedType == "Isolation"
                };
                _context.Beds.Add(bed);
                created.Add(bed);
            }

            await _context.SaveChangesAsync();

            await _audit.LogActivityAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                "CREATE", "Bed", string.Join(",", created.Select(b => b.Id.ToString())), null,
                $"{numberOfBeds} bed(s) added — Block:{model.Block} Floor:{model.Floor} Ward:{model.WardId} Room:{model.RoomNumber}");

            TempData["SuccessMessage"] = numberOfBeds == 1
                ? $"Bed {created[0].BedNumber} created successfully."
                : $"{numberOfBeds} beds created in Room {model.RoomNumber} (Block {model.Block}, Floor {model.Floor}).";

            return RedirectToAction(nameof(Index));
        }

        // ── Admin-only: Edit bed ────────────────────────────────
        [Authorize(Roles = BedManagementManageRoles)]
        public async Task<IActionResult> Edit(int id)
        {
            var bed = await _context.Beds.FindAsync(id);
            if (bed == null) return NotFound();
            ViewBag.Wards = await _context.Wards.Where(w => w.IsActive).OrderBy(w => w.Name).ToListAsync();
            return View(bed);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = BedManagementManageRoles)]
        public async Task<IActionResult> Edit(int id, Bed model)
        {
            if (id != model.Id) return BadRequest();
            if (!await _context.Beds.AnyAsync(b => b.Id == id)) return NotFound();
            if (!await _context.Wards.AnyAsync(w => w.Id == model.WardId))
            {
                ModelState.AddModelError(nameof(Bed.WardId), "Please choose a ward.");
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Wards = await _context.Wards.Where(w => w.IsActive).OrderBy(w => w.Name).ToListAsync();
                return View(model);
            }

            model.LastUpdated = DateTime.Now;
            if (model.BedType == "ICU") model.RequiresAdminApproval = true;
            if (model.BedType == "Isolation") model.IsIsolation = true;
            _context.Beds.Update(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Bed {model.BedNumber} updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── Wards (per hospital) ────────────────────────────────
        [Authorize(Roles = BedManagementManageRoles)]
        public async Task<IActionResult> Wards()
        {
            var wards = await _context.Wards.OrderBy(w => w.Name).ToListAsync();
            var beds = await _context.Beds
                .Where(b => b.IsActive)
                .GroupBy(b => b.WardId)
                .Select(g => new { WardId = g.Key, Total = g.Count(), Occupied = g.Count(b => b.Status == "Occupied") })
                .ToListAsync();
            ViewBag.BedCounts = beds.ToDictionary(b => b.WardId, b => (b.Total, b.Occupied));
            return View(wards);
        }

        [Authorize(Roles = BedManagementManageRoles)]
        public IActionResult CreateWard()
        {
            return View("WardForm", new Ward { IsActive = true });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = BedManagementManageRoles)]
        public async Task<IActionResult> CreateWard(Ward model)
        {
            model.Name = (model.Name ?? string.Empty).Trim();
            model.Description = model.Description?.Trim() ?? string.Empty;
            await ValidateWardAsync(model);
            if (!ModelState.IsValid)
            {
                return View("WardForm", model);
            }

            var ward = new Ward
            {
                Name = model.Name,
                Description = model.Description,
                IsActive = model.IsActive,
                TotalBeds = 0,
                OccupiedBeds = 0,
                CreatedDate = DateTime.Now
            };
            _context.Wards.Add(ward);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "CREATE", "Ward", ward.Id.ToString(), null, ward.Name);

            TempData["SuccessMessage"] = $"Ward \"{ward.Name}\" created. Add beds to it with Add Beds.";
            return RedirectToAction(nameof(Wards));
        }

        [Authorize(Roles = BedManagementManageRoles)]
        public async Task<IActionResult> EditWard(int id)
        {
            var ward = await _context.Wards.FirstOrDefaultAsync(w => w.Id == id);
            if (ward == null) return NotFound();
            return View("WardForm", ward);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = BedManagementManageRoles)]
        public async Task<IActionResult> EditWard(int id, Ward model)
        {
            var ward = await _context.Wards.FirstOrDefaultAsync(w => w.Id == id);
            if (ward == null) return NotFound();

            model.Id = id;
            model.Name = (model.Name ?? string.Empty).Trim();
            model.Description = model.Description?.Trim() ?? string.Empty;
            await ValidateWardAsync(model);
            if (!model.IsActive && ward.IsActive && await _context.Beds.AnyAsync(b => b.WardId == id && b.Status == "Occupied"))
            {
                ModelState.AddModelError(nameof(Ward.IsActive), "This ward has occupied beds. Release or transfer the patients first.");
            }
            if (!ModelState.IsValid)
            {
                return View("WardForm", model);
            }

            var old = $"{ward.Name} (active: {ward.IsActive})";
            ward.Name = model.Name;
            ward.Description = model.Description;
            ward.IsActive = model.IsActive;
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "UPDATE", "Ward", id.ToString(), old, $"{ward.Name} (active: {ward.IsActive})");

            TempData["SuccessMessage"] = $"Ward \"{ward.Name}\" updated.";
            return RedirectToAction(nameof(Wards));
        }

        private async Task ValidateWardAsync(Ward model)
        {
            ModelState.Remove(nameof(Ward.Beds));
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(Ward.Name), "Ward name is required.");
            }
            else if (await _context.Wards.AnyAsync(w => w.Name == model.Name && w.Id != model.Id))
            {
                ModelState.AddModelError(nameof(Ward.Name), "This hospital already has a ward with this name.");
            }
        }

        public sealed class AssignBedRequest
        {
            public int BedId { get; set; }
            public int PatientId { get; set; }
        }

        public sealed class ReleaseBedRequest
        {
            public int BedId { get; set; }
        }

        public sealed class TransferBedRequest
        {
            public int FromBedId { get; set; }
            public int ToBedId { get; set; }
        }

        public sealed class UpdateBedStatusRequest
        {
            public int BedId { get; set; }
            public string Status { get; set; } = string.Empty;
        }
    }
}
