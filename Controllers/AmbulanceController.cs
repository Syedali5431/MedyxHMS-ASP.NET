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
    public class AmbulanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public AmbulanceController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // ── Vehicles ──────────────────────────────────────────────

        public async Task<IActionResult> Index()
        {
            var vehicles = await _context.AmbulanceVehicles
                .OrderBy(v => v.VehicleNumber)
                .ToListAsync();
            return View(vehicles);
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        public IActionResult Create()
        {
            return View(new AmbulanceVehicle());
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Create(AmbulanceVehicle vehicle)
        {
            if (!ModelState.IsValid) return View(vehicle);

            _context.AmbulanceVehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier),
                "CREATE", "AmbulanceVehicle", vehicle.Id.ToString(), null, vehicle.VehicleNumber);
            TempData["SuccessMessage"] = "Vehicle added.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Edit(int id)
        {
            var vehicle = await _context.AmbulanceVehicles.FindAsync(id);
            if (vehicle == null) return NotFound();
            return View(vehicle);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Edit(int id, AmbulanceVehicle vehicle)
        {
            if (id != vehicle.Id) return BadRequest();
            if (!ModelState.IsValid) return View(vehicle);

            _context.AmbulanceVehicles.Update(vehicle);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Vehicle updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── Dispatches ────────────────────────────────────────────

        public async Task<IActionResult> Dispatches(DateTime? from = null, DateTime? to = null, int? vehicleId = null)
        {
            var query = _context.AmbulanceDispatches
                .Include(d => d.AmbulanceVehicle)
                .Include(d => d.Patient)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(d => d.DispatchTime >= from.Value);
            if (to.HasValue)
                query = query.Where(d => d.DispatchTime <= to.Value.AddDays(1).AddTicks(-1));
            if (vehicleId.HasValue)
                query = query.Where(d => d.AmbulanceVehicleId == vehicleId.Value);

            ViewBag.FromDate = from?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to?.ToString("yyyy-MM-dd");
            ViewBag.VehicleId = vehicleId;
            ViewBag.Vehicles = await _context.AmbulanceVehicles.OrderBy(v => v.VehicleNumber).ToListAsync();

            var dispatches = await query
                .OrderByDescending(d => d.DispatchTime)
                .ToListAsync();
            return View(dispatches);
        }

        public async Task<IActionResult> Dispatch()
        {
            ViewBag.Vehicles = await _context.AmbulanceVehicles
                .Where(v => v.Status == "Available")
                .OrderBy(v => v.VehicleNumber)
                .ToListAsync();
            ViewBag.Patients = await _context.Patients
                .OrderBy(p => p.FirstName)
                .ToListAsync();
            return View(new AmbulanceDispatch { DispatchTime = DateTime.Now });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Dispatch(AmbulanceDispatch model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Vehicles = await _context.AmbulanceVehicles
                    .Where(v => v.Status == "Available").ToListAsync();
                ViewBag.Patients = await _context.Patients.ToListAsync();
                return View(model);
            }

            model.Status = "Dispatched";
            _context.AmbulanceDispatches.Add(model);

            var vehicle = await _context.AmbulanceVehicles.FindAsync(model.AmbulanceVehicleId);
            if (vehicle != null) vehicle.Status = "Dispatched";

            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier),
                "DISPATCH", "AmbulanceDispatch", model.Id.ToString(), null,
                $"Vehicle {model.AmbulanceVehicleId} → {model.PickupAddress}");
            TempData["SuccessMessage"] = "Ambulance dispatched.";
            return RedirectToAction(nameof(Dispatches));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReturned(int id)
        {
            var dispatch = await _context.AmbulanceDispatches
                .Include(d => d.AmbulanceVehicle)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (dispatch == null) return NotFound();

            dispatch.Status = "Returned";
            dispatch.ReturnTime = DateTime.UtcNow;
            if (dispatch.AmbulanceVehicle != null)
                dispatch.AmbulanceVehicle.Status = "Available";

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Ambulance marked as returned.";
            return RedirectToAction(nameof(Dispatches));
        }
    }
}
