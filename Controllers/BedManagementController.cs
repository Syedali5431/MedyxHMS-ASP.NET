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

            var old = bed.Status;
            bed.Status = status;
            bed.LastUpdated = DateTime.UtcNow;
            // If moving to Available, clear any stale patient reference
            if (status == "Available") bed.PatientId = null;
            await _context.SaveChangesAsync();

            await _audit.LogActivityAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                "STATUS_CHANGE", "Bed", bedId.ToString(), old, status);

            TempData["SuccessMessage"] = $"Bed status updated to {status}.";
            return RedirectToAction(nameof(Index));
        }

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

            var old = bed.Status;
            bed.Status = request.Status;
            bed.LastUpdated = DateTime.UtcNow;
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
                    CreatedDate = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
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
            if (!ModelState.IsValid)
            {
                ViewBag.Wards = await _context.Wards.Where(w => w.IsActive).OrderBy(w => w.Name).ToListAsync();
                return View(model);
            }

            model.LastUpdated = DateTime.UtcNow;
            if (model.BedType == "ICU") model.RequiresAdminApproval = true;
            if (model.BedType == "Isolation") model.IsIsolation = true;
            _context.Beds.Update(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Bed {model.BedNumber} updated.";
            return RedirectToAction(nameof(Index));
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
