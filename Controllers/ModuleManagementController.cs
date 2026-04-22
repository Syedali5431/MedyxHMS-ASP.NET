using System.Security.Claims;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for ModuleManagementController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize]
    public class ModuleManagementController : Controller
    {
        private readonly IModuleService _moduleService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ModuleManagementController(IModuleService moduleService, UserManager<ApplicationUser> userManager)
        {
            _moduleService = moduleService;
            _userManager = userManager;
        }

        // â”€â”€ Global Module List (SuperAdmin only) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        /// <summary>
        /// Shows all system modules with a global enable/disable toggle.
        /// Only SuperAdmin may access this page.
        /// </summary>
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Index()
        {
            var modules = await _moduleService.GetAllModulesAsync();
            return View(new SystemModuleListViewModel { Modules = modules });
        }

        /// <summary>
        /// SuperAdmin AJAX: toggle a module's global enabled state.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ToggleGlobal([FromBody] ToggleGlobalRequest req)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            var ok = await _moduleService.SetGlobalModuleEnabledAsync(req.ModuleId, req.IsEnabled, userId);
            return ok
                ? Json(new { success = true })
                : Json(new { success = false, message = "Module not found." });
        }

        // â”€â”€ User List for Module Assignment (Admin + SuperAdmin) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        /// <summary>
        /// Shows a searchable list of users so an admin can pick one to configure modules.
        /// </summary>
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Users(string? search)
        {
            var query = _userManager.Users.Where(u => u.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                query = query.Where(u =>
                    u.Email!.ToLower().Contains(lower) ||
                    u.FirstName.ToLower().Contains(lower) ||
                    u.LastName.ToLower().Contains(lower) ||
                    u.EmployeeId.ToLower().Contains(lower));
            }

            var users = await query
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var summaries = new List<UserSummary>(users.Count);
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                summaries.Add(new UserSummary
                {
                    Id = u.Id,
                    FullName = $"{u.FirstName} {u.LastName}".Trim(),
                    Email = u.Email ?? string.Empty,
                    EmployeeId = u.EmployeeId,
                    IsActive = u.IsActive,
                    Roles = roles.ToList()
                });
            }

            return View(new UserListForModulesViewModel
            {
                Users = summaries,
                SearchTerm = search
            });
        }

        // â”€â”€ Per-User Module Access â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        /// <summary>
        /// Shows the module access grid for a specific user.
        /// Admin can configure access; SuperAdmin sees an additional note when
        /// a module is globally disabled.
        /// </summary>
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UserAccess(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var rows = await _moduleService.GetUserModuleAccessRowsAsync(userId);

            return View(new UserModuleAccessViewModel
            {
                UserId = userId,
                UserFullName = $"{user.FirstName} {user.LastName}".Trim(),
                UserEmail = user.Email ?? string.Empty,
                Rows = rows
            });
        }

        /// <summary>
        /// SuperAdmin AJAX: set per-user module access.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> SetUserAccess([FromBody] SetUserAccessRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.UserId))
                return Json(new { success = false, message = "User ID required." });

            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            var ok = await _moduleService.SetUserModuleAccessAsync(req.UserId, req.ModuleId, req.IsEnabled, performedBy);

            return ok
                ? Json(new { success = true })
                : Json(new { success = false, message = "Cannot enable a globally-disabled module for this user." });
        }
    }

    // â”€â”€ Request DTOs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public record ToggleGlobalRequest(int ModuleId, bool IsEnabled);
    public record SetUserAccessRequest(string UserId, int ModuleId, bool IsEnabled);
}
