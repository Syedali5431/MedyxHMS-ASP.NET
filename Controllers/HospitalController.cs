using System.Security.Claims;
using MedyxHMS.Data;
using MedyxHMS.Extensions;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MedyxHMS.Controllers
{
    /// <summary>Multi-hospital (branches of one group): hospital list, staff access and the hospital switcher.</summary>
    [Authorize]
    public class HospitalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHospitalContext _hospitalContext;
        private readonly IAuditService _audit;
        private readonly IMemoryCache _cache;

        public HospitalController(ApplicationDbContext context, IHospitalContext hospitalContext, IAuditService audit, IMemoryCache cache)
        {
            _context = context;
            _hospitalContext = hospitalContext;
            _audit = audit;
            _cache = cache;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private bool IsSuperAdmin => User.IsInRole("SuperAdmin");

        /// <summary>A SuperAdmin manages every hospital; an Admin only the hospitals assigned to them.</summary>
        private bool CanManageHospital(int hospitalId) =>
            IsSuperAdmin || _hospitalContext.AccessibleHospitals.Any(h => h.Id == hospitalId);

        private IActionResult NoAccess(string message)
        {
            TempData["ErrorMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        // ── Hospital list ─────────────────────────────────────────

        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Index()
        {
            var hospitals = await _context.Hospitals.AsNoTracking()
                .OrderByDescending(h => h.IsDefault).ThenBy(h => h.Name)
                .ToListAsync();

            // An Admin sees the hospitals assigned to them; a SuperAdmin sees the whole group.
            if (!IsSuperAdmin)
            {
                hospitals = hospitals.Where(h => CanManageHospital(h.Id)).ToList();
            }

            ViewBag.CanAddHospitals = IsSuperAdmin;

            // Counts across the whole group (filters off) so every hospital shows its own totals.
            var staff = await _context.UserHospitalAccesses.GroupBy(a => a.HospitalId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
            var appointments = await _context.Appointments.IgnoreQueryFilters().Where(a => a.HospitalId != null).GroupBy(a => a.HospitalId!.Value).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
            var admissions = await _context.IPDAdmissions.IgnoreQueryFilters().Where(a => a.HospitalId != null && a.Status == "Admitted").GroupBy(a => a.HospitalId!.Value).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
            var bills = await _context.Bills.IgnoreQueryFilters().Where(b => b.HospitalId != null).GroupBy(b => b.HospitalId!.Value).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
            var wards = await _context.Wards.IgnoreQueryFilters().Where(w => w.HospitalId != null).GroupBy(w => w.HospitalId!.Value).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);

            var model = hospitals.Select(h => new HospitalListItemViewModel
            {
                Hospital = h,
                StaffAssigned = staff.GetValueOrDefault(h.Id),
                Appointments = appointments.GetValueOrDefault(h.Id),
                CurrentAdmissions = admissions.GetValueOrDefault(h.Id),
                Bills = bills.GetValueOrDefault(h.Id),
                Wards = wards.GetValueOrDefault(h.Id)
            }).ToList();

            return View(model);
        }

        // Adding hospitals to the group is for SuperAdmins.
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult Create()
        {
            ViewBag.CanChangeStatus = true;
            return View(new Hospital { IsActive = true });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Create(Hospital model)
        {
            ViewBag.CanChangeStatus = true;
            Normalize(model);
            await ValidateHospitalAsync(model, isNew: true);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.CreatedDate = DateTime.Now;
            if (model.IsDefault)
            {
                await ClearDefaultAsync();
            }

            _context.Hospitals.Add(model);
            await _context.SaveChangesAsync();
            HospitalContextMiddleware.InvalidateCache(_cache);
            await _audit.LogActivityAsync(CurrentUserId, "CREATE", "Hospital", model.Id.ToString(), null, $"{model.Code} - {model.Name}");

            TempData["SuccessMessage"] = $"Hospital \"{model.Name}\" added. Use Staff Access to choose who can work there.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var hospital = await _context.Hospitals.FindAsync(id);
            if (hospital == null) return NotFound();
            if (!CanManageHospital(id)) return NoAccess("You can only edit the hospitals assigned to you.");
            ViewBag.CanChangeStatus = IsSuperAdmin;
            return View(hospital);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Edit(int id, Hospital model)
        {
            var hospital = await _context.Hospitals.FindAsync(id);
            if (hospital == null) return NotFound();
            if (!CanManageHospital(id)) return NoAccess("You can only edit the hospitals assigned to you.");
            ViewBag.CanChangeStatus = IsSuperAdmin;

            // Only a SuperAdmin switches hospitals on/off or changes the group's default hospital.
            if (!IsSuperAdmin)
            {
                model.IsActive = hospital.IsActive;
                model.IsDefault = hospital.IsDefault;
            }

            model.Id = id;
            Normalize(model);
            await ValidateHospitalAsync(model, isNew: false, existing: hospital);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var oldValues = $"{hospital.Code} - {hospital.Name} (active: {hospital.IsActive}, default: {hospital.IsDefault})";
            if (model.IsDefault && !hospital.IsDefault)
            {
                await ClearDefaultAsync();
            }

            hospital.Code = model.Code;
            hospital.Name = model.Name;
            hospital.Address = model.Address;
            hospital.City = model.City;
            hospital.Phone = model.Phone;
            hospital.Email = model.Email;
            hospital.LicenseNumber = model.LicenseNumber;
            hospital.IsActive = model.IsActive;
            hospital.IsDefault = model.IsDefault;
            hospital.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            HospitalContextMiddleware.InvalidateCache(_cache);
            await _audit.LogActivityAsync(CurrentUserId, "UPDATE", "Hospital", id.ToString(), oldValues,
                $"{hospital.Code} - {hospital.Name} (active: {hospital.IsActive}, default: {hospital.IsDefault})");

            TempData["SuccessMessage"] = $"Hospital \"{hospital.Name}\" updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── Staff access ──────────────────────────────────────────

        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Access(int id)
        {
            var hospital = await _context.Hospitals.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id);
            if (hospital == null) return NotFound();
            if (!CanManageHospital(id)) return NoAccess("You can only manage staff access for the hospitals assigned to you.");
            return View(await BuildAccessModelAsync(hospital));
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Access(int id, List<string>? assignedUserIds, List<string>? defaultUserIds)
        {
            var hospital = await _context.Hospitals.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id);
            if (hospital == null) return NotFound();
            if (!CanManageHospital(id)) return NoAccess("You can only manage staff access for the hospitals assigned to you.");

            var assigned = (assignedUserIds ?? new List<string>()).ToHashSet();
            var defaults = (defaultUserIds ?? new List<string>()).Where(assigned.Contains).ToHashSet();
            var model = await BuildAccessModelAsync(hospital);
            var isSuperAdmin = User.IsInRole("SuperAdmin");

            var existing = await _context.UserHospitalAccesses.Where(a => a.HospitalId == id).ToListAsync();
            var changedUsers = new List<string>();
            var added = 0;
            var removed = 0;

            foreach (var row in model.Users)
            {
                // Only a SuperAdmin assigns hospitals to administrators (Admin and SuperAdmin accounts).
                if ((row.IsSuperAdmin || row.IsAdmin) && !isSuperAdmin)
                {
                    continue;
                }

                var current = existing.FirstOrDefault(a => a.UserId == row.UserId);
                var wantAccess = assigned.Contains(row.UserId);
                var wantDefault = defaults.Contains(row.UserId);

                if (wantAccess && current == null)
                {
                    current = new UserHospitalAccess { UserId = row.UserId, HospitalId = id, CreatedDate = DateTime.Now };
                    _context.UserHospitalAccesses.Add(current);
                    added++;
                    changedUsers.Add(row.UserId);
                }
                else if (!wantAccess && current != null)
                {
                    _context.UserHospitalAccesses.Remove(current);
                    removed++;
                    changedUsers.Add(row.UserId);
                    continue;
                }

                if (current != null && current.IsDefault != wantDefault)
                {
                    current.IsDefault = wantDefault;
                    if (!changedUsers.Contains(row.UserId)) changedUsers.Add(row.UserId);
                }
            }

            // A user has at most one default hospital.
            if (defaults.Count > 0)
            {
                var otherDefaults = await _context.UserHospitalAccesses
                    .Where(a => a.HospitalId != id && a.IsDefault && defaults.Contains(a.UserId))
                    .ToListAsync();
                otherDefaults.ForEach(a => a.IsDefault = false);
            }

            await _context.SaveChangesAsync();
            HospitalContextMiddleware.InvalidateUsers(_cache, changedUsers);
            if (changedUsers.Count > 0)
            {
                await _audit.LogActivityAsync(CurrentUserId, "UPDATE", "HospitalAccess", id.ToString(), null,
                    $"{hospital.Name}: {added} added, {removed} removed, {changedUsers.Count} user(s) changed");
            }

            TempData["SuccessMessage"] = changedUsers.Count == 0
                ? "No changes to save."
                : $"Staff access for \"{hospital.Name}\" saved ({added} added, {removed} removed).";
            return RedirectToAction(nameof(Access), new { id });
        }

        // ── Hospitals of one user (User Management → Hospitals) ─────

        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> UserAccess(string userId)
        {
            var model = await BuildUserAccessModelAsync(userId);
            if (model == null) return NotFound();
            if (!model.CanEdit)
            {
                TempData["ErrorMessage"] = "Only a SuperAdmin can assign hospitals to administrators.";
                return RedirectToAction("UserManagement", "SystemManagement");
            }

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> UserAccess(string userId, List<int>? hospitalIds, int? defaultHospitalId)
        {
            var model = await BuildUserAccessModelAsync(userId);
            if (model == null) return NotFound();
            if (!model.CanEdit)
            {
                TempData["ErrorMessage"] = "Only a SuperAdmin can assign hospitals to administrators.";
                return RedirectToAction("UserManagement", "SystemManagement");
            }

            var wanted = (hospitalIds ?? new List<int>()).ToHashSet();
            var existing = await _context.UserHospitalAccesses.Where(a => a.UserId == userId).ToListAsync();
            var added = new List<string>();
            var removed = new List<string>();
            foreach (var row in model.Hospitals.Where(h => h.CanEdit))
            {
                var current = existing.FirstOrDefault(a => a.HospitalId == row.HospitalId);
                if (wanted.Contains(row.HospitalId) && current == null)
                {
                    current = new UserHospitalAccess { UserId = userId, HospitalId = row.HospitalId, CreatedDate = DateTime.Now };
                    _context.UserHospitalAccesses.Add(current);
                    existing.Add(current);
                    added.Add(row.Name);
                }
                else if (!wanted.Contains(row.HospitalId) && current != null)
                {
                    _context.UserHospitalAccesses.Remove(current);
                    existing.Remove(current);
                    removed.Add(row.Name);
                }
            }

            // One default hospital, chosen among the assigned ones (the first one when none is ticked).
            var defaultId = defaultHospitalId.HasValue && existing.Any(a => a.HospitalId == defaultHospitalId.Value)
                ? defaultHospitalId.Value
                : existing.FirstOrDefault(a => a.IsDefault)?.HospitalId ?? existing.OrderBy(a => a.HospitalId).FirstOrDefault()?.HospitalId;
            foreach (var a in existing)
            {
                a.IsDefault = a.HospitalId == defaultId;
            }

            await _context.SaveChangesAsync();
            HospitalContextMiddleware.InvalidateUsers(_cache, new[] { userId });
            var defaultName = model.Hospitals.FirstOrDefault(h => h.HospitalId == defaultId)?.Name ?? "-";
            await _audit.LogActivityAsync(CurrentUserId, "UPDATE", "HospitalAccess", userId, null,
                $"{model.UserName}: added [{string.Join(", ", added)}], removed [{string.Join(", ", removed)}], default {defaultName}");

            TempData["SuccessMessage"] = added.Count + removed.Count == 0 && existing.Count > 0
                ? $"Hospitals of {model.FullName} saved (default: {defaultName})."
                : $"Hospitals of {model.FullName} saved: {(existing.Count == 0 ? "none assigned (works in the default hospital)" : $"{existing.Count} hospital(s), default {defaultName}")}.";
            return RedirectToAction(nameof(UserAccess), new { userId });
        }

        private async Task<HospitalUserAccessViewModel?> BuildUserAccessModelAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            var roles = await (from ur in _context.Set<IdentityUserRole<string>>()
                               join r in _context.Set<IdentityRole>() on ur.RoleId equals r.Id
                               where ur.UserId == userId
                               select r.Name).ToListAsync();
            var targetIsAdmin = roles.Contains("Admin") || roles.Contains("SuperAdmin");
            var hospitals = await _context.Hospitals.AsNoTracking().OrderByDescending(h => h.IsDefault).ThenBy(h => h.Name).ToListAsync();
            var access = await _context.UserHospitalAccesses.AsNoTracking().Where(a => a.UserId == userId).ToListAsync();

            return new HospitalUserAccessViewModel
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Roles = string.Join(", ", roles.Where(r => r != null && r != "Patient").OrderBy(r => r)),
                IsSuperAdmin = roles.Contains("SuperAdmin"),
                IsAdmin = roles.Contains("Admin"),
                CanEdit = IsSuperAdmin || !targetIsAdmin,
                Hospitals = hospitals.Select(h => new HospitalUserAccessRow
                {
                    HospitalId = h.Id,
                    Code = h.Code,
                    Name = h.Name,
                    City = h.City,
                    IsActive = h.IsActive,
                    IsGroupDefault = h.IsDefault,
                    HasAccess = access.Any(a => a.HospitalId == h.Id),
                    IsDefault = access.Any(a => a.HospitalId == h.Id && a.IsDefault),
                    // An Admin can give or take away only the hospitals they work in themselves.
                    CanEdit = (IsSuperAdmin || !targetIsAdmin) && CanManageHospital(h.Id)
                }).ToList()
            };
        }

        // ── Hospital switcher (any staff member) ─────────────────

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Switch(string hospital, string? returnUrl)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId) || !_hospitalContext.IsStaffContext)
            {
                return Forbid();
            }

            string value;
            string name;
            if (string.Equals(hospital, HospitalContextMiddleware.AllHospitalsValue, StringComparison.OrdinalIgnoreCase)
                && _hospitalContext.CanViewAllHospitals)
            {
                value = HospitalContextMiddleware.AllHospitalsValue;
                name = "All hospitals";
            }
            else if (int.TryParse(hospital, out var id) && _hospitalContext.AccessibleHospitals.Any(h => h.Id == id))
            {
                value = id.ToString();
                name = _hospitalContext.AccessibleHospitals.First(h => h.Id == id).Name;
            }
            else
            {
                TempData["ErrorMessage"] = "You do not have access to that hospital.";
                return RedirectToLocal(returnUrl);
            }

            HospitalContextMiddleware.WriteSelection(HttpContext, userId, value);

            TempData["InfoMessage"] = $"Now working in: {name}.";
            return RedirectToLocal(returnUrl);
        }

        // Records of the previous hospital are not visible after switching, so record pages (with an id)
        // go back to their list; list pages are reopened as they were.
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var path = returnUrl.Split('?')[0];
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 3 || segments.Any(s => s.All(char.IsDigit)))
            {
                return Redirect("/" + segments[0]);
            }

            return Redirect(returnUrl);
        }

        // ── helpers ──────────────────────────────────────────────

        private static void Normalize(Hospital model)
        {
            model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
            model.Name = (model.Name ?? string.Empty).Trim();
            model.Address = model.Address?.Trim() ?? string.Empty;
            model.City = model.City?.Trim() ?? string.Empty;
            model.Phone = model.Phone?.Trim() ?? string.Empty;
            model.Email = model.Email?.Trim() ?? string.Empty;
            model.LicenseNumber = model.LicenseNumber?.Trim() ?? string.Empty;
        }

        private async Task ValidateHospitalAsync(Hospital model, bool isNew, Hospital? existing = null)
        {
            if (await _context.Hospitals.AnyAsync(h => h.Code == model.Code && h.Id != model.Id))
            {
                ModelState.AddModelError(nameof(Hospital.Code), "Another hospital already uses this code.");
            }

            if (!string.IsNullOrEmpty(model.Email) && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(model.Email))
            {
                ModelState.AddModelError(nameof(Hospital.Email), "Please enter a valid e-mail address.");
            }

            if (model.IsDefault && !model.IsActive)
            {
                ModelState.AddModelError(nameof(Hospital.IsActive), "The default hospital must be active.");
            }

            if (!isNew && existing != null)
            {
                if (existing.IsDefault && !model.IsDefault)
                {
                    ModelState.AddModelError(nameof(Hospital.IsDefault), "Choose another hospital as default instead (edit that hospital and tick \"Default hospital\").");
                }

                if (existing.IsActive && !model.IsActive && !await _context.Hospitals.AnyAsync(h => h.IsActive && h.Id != existing.Id))
                {
                    ModelState.AddModelError(nameof(Hospital.IsActive), "At least one hospital must stay active.");
                }
            }
        }

        private async Task ClearDefaultAsync()
        {
            var defaults = await _context.Hospitals.Where(h => h.IsDefault).ToListAsync();
            defaults.ForEach(h => h.IsDefault = false);
        }

        private async Task<HospitalAccessViewModel> BuildAccessModelAsync(Hospital hospital)
        {
            var userRoles = await (from ur in _context.Set<IdentityUserRole<string>>()
                                   join r in _context.Set<IdentityRole>() on ur.RoleId equals r.Id
                                   select new { ur.UserId, r.Name }).ToListAsync();
            var rolesByUser = userRoles.GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Name ?? string.Empty).OrderBy(n => n).ToList());

            var staffIds = rolesByUser.Where(kv => kv.Value.Any(r => !string.Equals(r, "Patient", StringComparison.OrdinalIgnoreCase)))
                .Select(kv => kv.Key).ToHashSet();

            var users = await _context.Users.AsNoTracking()
                .Where(u => u.IsActive && staffIds.Contains(u.Id))
                .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                .Select(u => new { u.Id, u.UserName, u.FirstName, u.LastName })
                .ToListAsync();

            var access = await _context.UserHospitalAccesses.AsNoTracking()
                .Where(a => staffIds.Contains(a.UserId))
                .Select(a => new { a.UserId, a.HospitalId, a.IsDefault, HospitalName = a.Hospital.Name })
                .ToListAsync();

            var model = new HospitalAccessViewModel
            {
                Hospital = hospital,
                CanEditSuperAdmins = User.IsInRole("SuperAdmin"),
                CanEditAdmins = User.IsInRole("SuperAdmin"),
                Users = users.Select(u =>
                {
                    var roles = rolesByUser.GetValueOrDefault(u.Id) ?? new List<string>();
                    var mine = access.FirstOrDefault(a => a.UserId == u.Id && a.HospitalId == hospital.Id);
                    var others = access.Where(a => a.UserId == u.Id && a.HospitalId != hospital.Id).Select(a => a.HospitalName).ToList();
                    return new HospitalAccessUserRow
                    {
                        UserId = u.Id,
                        UserName = u.UserName ?? string.Empty,
                        FullName = $"{u.FirstName} {u.LastName}".Trim(),
                        Roles = string.Join(", ", roles.Where(r => !string.Equals(r, "Patient", StringComparison.OrdinalIgnoreCase))),
                        IsSuperAdmin = roles.Contains("SuperAdmin"),
                        IsAdmin = roles.Contains("Admin"),
                        HasAccess = mine != null,
                        IsDefault = mine?.IsDefault == true,
                        OtherHospitals = others,
                        // Unassigned staff work in the default hospital (until they are given hospitals).
                        ImplicitDefaultAccess = hospital.IsDefault && !access.Any(a => a.UserId == u.Id)
                    };
                }).ToList()
            };

            return model;
        }
    }
}
