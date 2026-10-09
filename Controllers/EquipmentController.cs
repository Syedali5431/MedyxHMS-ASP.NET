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
    /// <summary>
    /// Equipment register: assets of the active hospital with preventive-maintenance and calibration schedules,
    /// due/overdue alerts and service history. All staff can view the register and report a fault;
    /// Admin/SuperAdmin manage assets and record services.
    /// </summary>
    [Authorize(Roles = QualityService.StaffRoles)]
    public class EquipmentController : Controller
    {
        public static readonly string[] Categories =
            { "Patient monitoring", "Life support", "Diagnostic imaging", "Laboratory", "Sterilisation", "Surgical / OT", "Infusion", "Rehabilitation", "IT / communication", "Facility / utility", "Other" };
        public static readonly string[] Statuses = { "In service", "Under maintenance", "Out of service", "Condemned" };
        public static readonly string[] RiskClasses = { "Low", "Medium", "High" };
        public static readonly string[] ServiceTypes = { "Preventive maintenance", "Calibration", "Repair", "Inspection" };
        public static readonly string[] Results = { "Pass", "Adjusted", "Repaired", "Fail" };
        public const int DueSoonDays = 30;

        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public EquipmentController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        private bool IsManager => User.IsInRole("SuperAdmin") || User.IsInRole("Admin");

        public async Task<IActionResult> Index(string? filter, string? category, string? status, string? search)
        {
            var today = DateTime.Today;
            var soon = today.AddDays(DueSoonDays);
            var query = _context.Equipment.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(e => e.Category == category);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(e => e.Status == status);
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(e => e.Name.Contains(search) || e.AssetTag.Contains(search) || e.SerialNumber.Contains(search) || e.Location.Contains(search));

            var active = query.Where(e => e.Status != "Condemned");
            query = filter switch
            {
                "overdue" => active.Where(e => (e.NextMaintenanceDue != null && e.NextMaintenanceDue < today) || (e.NextCalibrationDue != null && e.NextCalibrationDue < today)),
                "due" => active.Where(e => (e.NextMaintenanceDue != null && e.NextMaintenanceDue >= today && e.NextMaintenanceDue <= soon) || (e.NextCalibrationDue != null && e.NextCalibrationDue >= today && e.NextCalibrationDue <= soon)),
                "out" => query.Where(e => e.Status == "Out of service" || e.Status == "Under maintenance"),
                _ => query
            };

            var all = _context.Equipment.AsNoTracking().Where(e => e.Status != "Condemned");
            ViewBag.Total = await all.CountAsync();
            ViewBag.Overdue = await all.CountAsync(e => (e.NextMaintenanceDue != null && e.NextMaintenanceDue < today) || (e.NextCalibrationDue != null && e.NextCalibrationDue < today));
            ViewBag.DueSoon = await all.CountAsync(e => (e.NextMaintenanceDue != null && e.NextMaintenanceDue >= today && e.NextMaintenanceDue <= soon) || (e.NextCalibrationDue != null && e.NextCalibrationDue >= today && e.NextCalibrationDue <= soon));
            ViewBag.OutOfService = await all.CountAsync(e => e.Status == "Out of service" || e.Status == "Under maintenance");
            ViewBag.Filter = filter;
            ViewBag.Category = category;
            ViewBag.Status = status;
            ViewBag.Search = search;
            ViewBag.IsManager = IsManager;
            return View(await query.OrderBy(e => e.Name).Take(1000).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var equipment = await _context.Equipment.AsNoTracking().Include(e => e.ServiceRecords).FirstOrDefaultAsync(e => e.Id == id);
            if (equipment == null) return NotFound();
            ViewBag.IsManager = IsManager;
            return View(equipment);
        }

        [Authorize(Roles = QualityService.ManagerRoles)]
        public IActionResult Create()
        {
            return View("Form", new Equipment { MaintenanceIntervalDays = 180, CalibrationIntervalDays = 365, PurchaseDate = DateTime.Today });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = QualityService.ManagerRoles)]
        public async Task<IActionResult> Create([Bind(EditableFields)] Equipment input)
        {
            await ValidateAsync(input);
            if (!ModelState.IsValid) return View("Form", input);

            input.AssetTag = string.IsNullOrWhiteSpace(input.AssetTag) ? await NextAssetTagAsync() : input.AssetTag.Trim().ToUpperInvariant();
            Schedule(input);
            input.CreatedAt = DateTime.Now;
            input.CreatedBy = User.Identity?.Name ?? string.Empty;
            _context.Equipment.Add(input);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "CREATE", "Equipment", input.Id.ToString(), null, $"{input.AssetTag} {input.Name} ({input.Category}, {input.Location})");
            TempData["SuccessMessage"] = $"{input.AssetTag} – {input.Name} added to the register.";
            return RedirectToAction(nameof(Details), new { id = input.Id });
        }

        [Authorize(Roles = QualityService.ManagerRoles)]
        public async Task<IActionResult> Edit(int id)
        {
            var equipment = await _context.Equipment.FirstOrDefaultAsync(e => e.Id == id);
            if (equipment == null) return NotFound();
            return View("Form", equipment);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = QualityService.ManagerRoles)]
        public async Task<IActionResult> Edit(int id, [Bind(EditableFields)] Equipment input)
        {
            var equipment = await _context.Equipment.FirstOrDefaultAsync(e => e.Id == id);
            if (equipment == null) return NotFound();

            input.Id = id;
            await ValidateAsync(input);
            if (!ModelState.IsValid) return View("Form", input);

            var old = $"{equipment.AssetTag} {equipment.Status} PM {equipment.MaintenanceIntervalDays}d cal {equipment.CalibrationIntervalDays}d";
            equipment.AssetTag = string.IsNullOrWhiteSpace(input.AssetTag) ? equipment.AssetTag : input.AssetTag.Trim().ToUpperInvariant();
            equipment.Name = input.Name.Trim();
            equipment.Category = input.Category;
            equipment.Manufacturer = input.Manufacturer?.Trim() ?? string.Empty;
            equipment.Model = input.Model?.Trim() ?? string.Empty;
            equipment.SerialNumber = input.SerialNumber?.Trim() ?? string.Empty;
            equipment.Location = input.Location?.Trim() ?? string.Empty;
            equipment.Supplier = input.Supplier?.Trim() ?? string.Empty;
            equipment.PurchaseDate = input.PurchaseDate;
            equipment.WarrantyExpiry = input.WarrantyExpiry;
            equipment.RiskClass = input.RiskClass;
            equipment.Status = input.Status;
            equipment.MaintenanceIntervalDays = input.MaintenanceIntervalDays;
            equipment.CalibrationIntervalDays = input.CalibrationIntervalDays;
            equipment.LastMaintenanceDate = input.LastMaintenanceDate;
            equipment.LastCalibrationDate = input.LastCalibrationDate;
            equipment.Notes = input.Notes?.Trim() ?? string.Empty;
            Schedule(equipment);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "UPDATE", "Equipment", id.ToString(), old, $"{equipment.AssetTag} {equipment.Status} PM {equipment.MaintenanceIntervalDays}d cal {equipment.CalibrationIntervalDays}d");
            TempData["SuccessMessage"] = "Equipment updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Record maintenance, calibration, repair or inspection; updates the schedule and status.</summary>
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = QualityService.ManagerRoles)]
        public async Task<IActionResult> LogService(int id, string serviceType, DateTime? serviceDate, string? performedBy, string result, decimal? cost, string? certificateNumber, string? notes)
        {
            var equipment = await _context.Equipment.FirstOrDefaultAsync(e => e.Id == id);
            if (equipment == null) return NotFound();

            var errors = new List<string>();
            if (!ServiceTypes.Contains(serviceType)) errors.Add("Choose the type of service.");
            if (!Results.Contains(result)) errors.Add("Choose the result.");
            if (!serviceDate.HasValue || serviceDate.Value.Date > DateTime.Today) errors.Add("Enter the service date (not in the future).");
            if (string.IsNullOrWhiteSpace(performedBy)) errors.Add("Enter who performed the service.");
            if (cost is < 0) errors.Add("Cost cannot be negative.");
            if (equipment.Status == "Condemned") errors.Add("This equipment is condemned.");
            if (errors.Count > 0)
            {
                TempData["ErrorMessage"] = string.Join(" ", errors);
                return RedirectToAction(nameof(Details), new { id });
            }

            var date = serviceDate!.Value.Date;
            var record = new EquipmentServiceRecord
            {
                EquipmentId = id,
                ServiceType = serviceType,
                ServiceDate = date,
                PerformedBy = performedBy!.Trim(),
                Result = result,
                Cost = cost ?? 0,
                CertificateNumber = certificateNumber?.Trim() ?? string.Empty,
                Notes = notes?.Trim() ?? string.Empty,
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? string.Empty
            };
            _context.EquipmentServiceRecords.Add(record);

            var passed = result != "Fail";
            if (passed && serviceType == "Preventive maintenance" && (equipment.LastMaintenanceDate == null || date >= equipment.LastMaintenanceDate))
            {
                equipment.LastMaintenanceDate = date;
            }

            if (passed && serviceType == "Calibration" && (equipment.LastCalibrationDate == null || date >= equipment.LastCalibrationDate))
            {
                equipment.LastCalibrationDate = date;
            }

            // A failed service takes the device out of use; a passed service or repair returns it to service.
            equipment.Status = passed ? (equipment.Status == "Condemned" ? "Condemned" : "In service") : "Out of service";
            Schedule(equipment);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "SERVICE", "Equipment", id.ToString(), null, $"{equipment.AssetTag}: {serviceType} {date:yyyy-MM-dd} – {result} by {record.PerformedBy}");
            TempData["SuccessMessage"] = passed
                ? $"{serviceType} recorded. Status: {equipment.Status}."
                : $"{serviceType} recorded as failed – {equipment.AssetTag} is now out of service.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Any staff member can report a fault; the device is marked out of service until it is repaired.</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportFault(int id, string? description)
        {
            var equipment = await _context.Equipment.FirstOrDefaultAsync(e => e.Id == id);
            if (equipment == null) return NotFound();
            if (string.IsNullOrWhiteSpace(description))
            {
                TempData["ErrorMessage"] = "Describe the fault.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.EquipmentServiceRecords.Add(new EquipmentServiceRecord
            {
                EquipmentId = id,
                ServiceType = "Fault report",
                ServiceDate = DateTime.Today,
                PerformedBy = User.Identity?.Name ?? string.Empty,
                Result = "Reported",
                Notes = description.Trim(),
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? string.Empty
            });
            if (equipment.Status == "In service") equipment.Status = "Out of service";
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "FAULT", "Equipment", id.ToString(), null, $"{equipment.AssetTag}: {description}");
            TempData["SuccessMessage"] = $"Fault reported. {equipment.AssetTag} is marked out of service until it is repaired.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private const string EditableFields = "AssetTag,Name,Category,Manufacturer,Model,SerialNumber,Location,Supplier,PurchaseDate,WarrantyExpiry,RiskClass,Status,MaintenanceIntervalDays,CalibrationIntervalDays,LastMaintenanceDate,LastCalibrationDate,Notes";

        /// <summary>Next due = last service (or commissioning date, or today) + interval; no interval = no schedule.</summary>
        public static void Schedule(Equipment e)
        {
            var start = e.PurchaseDate?.Date ?? DateTime.Today;
            e.NextMaintenanceDue = e.MaintenanceIntervalDays > 0 ? (e.LastMaintenanceDate?.Date ?? start).AddDays(e.MaintenanceIntervalDays) : null;
            e.NextCalibrationDue = e.CalibrationIntervalDays > 0 ? (e.LastCalibrationDate?.Date ?? start).AddDays(e.CalibrationIntervalDays) : null;
        }

        private async Task ValidateAsync(Equipment model)
        {
            ModelState.Remove(nameof(Equipment.ServiceRecords));
            if (!Categories.Contains(model.Category)) ModelState.AddModelError(nameof(model.Category), "Choose a category.");
            if (!Statuses.Contains(model.Status)) ModelState.AddModelError(nameof(model.Status), "Choose a status.");
            if (!RiskClasses.Contains(model.RiskClass)) ModelState.AddModelError(nameof(model.RiskClass), "Choose a risk class.");
            var today = DateTime.Today;
            if (model.PurchaseDate > today) ModelState.AddModelError(nameof(model.PurchaseDate), "Cannot be in the future.");
            if (model.LastMaintenanceDate > today) ModelState.AddModelError(nameof(model.LastMaintenanceDate), "Cannot be in the future.");
            if (model.LastCalibrationDate > today) ModelState.AddModelError(nameof(model.LastCalibrationDate), "Cannot be in the future.");
            if (!string.IsNullOrWhiteSpace(model.AssetTag))
            {
                var tag = model.AssetTag.Trim().ToUpperInvariant();
                // Asset tags are unique across all hospitals of the group.
                if (await _context.Equipment.IgnoreQueryFilters().AnyAsync(e => e.AssetTag == tag && e.Id != model.Id))
                {
                    ModelState.AddModelError(nameof(model.AssetTag), "This asset tag is already used.");
                }
            }
        }

        private async Task<string> NextAssetTagAsync()
        {
            var tags = await _context.Equipment.IgnoreQueryFilters().Where(e => e.AssetTag.StartsWith("EQ-")).Select(e => e.AssetTag).ToListAsync();
            var max = tags.Select(t => int.TryParse(t[3..], out var n) ? n : 0).DefaultIfEmpty(0).Max();
            return $"EQ-{max + 1:D4}";
        }
    }
}
