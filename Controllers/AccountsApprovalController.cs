using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

// Purpose: Contains application code for AccountsApprovalController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AccountsApprovalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public AccountsApprovalController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string status = "Pending")
        {
            var requestsQuery = _context.AccountApprovalRequests
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                requestsQuery = requestsQuery.Where(r => r.Status == status);
            }

            var requests = await requestsQuery
                .OrderByDescending(r => r.RequestedAtUtc)
                .ToListAsync();

            var userIds = requests.Select(r => r.RequestedUserId).Distinct().ToList();
            var approvedByIds = requests.Where(r => !string.IsNullOrWhiteSpace(r.ApprovedByUserId)).Select(r => r.ApprovedByUserId!).Distinct().ToList();

            var users = await _userManager.Users
                .Where(u => userIds.Contains(u.Id) || approvedByIds.Contains(u.Id))
                .ToListAsync();

            var usersById = users.ToDictionary(u => u.Id, u => u);

            var vm = new AccountsApprovalIndexViewModel
            {
                StatusFilter = status,
                Requests = requests.Select(r =>
                {
                    usersById.TryGetValue(r.RequestedUserId, out var requestedUser);
                    usersById.TryGetValue(r.ApprovedByUserId ?? string.Empty, out var approvedByUser);

                    return new AccountApprovalListItemViewModel
                    {
                        RequestId = r.Id,
                        UserId = r.RequestedUserId,
                        FullName = requestedUser == null ? "N/A" : $"{requestedUser.FirstName} {requestedUser.LastName}".Trim(),
                        Email = requestedUser?.Email ?? string.Empty,
                        EmployeeId = requestedUser?.EmployeeId ?? string.Empty,
                        RequestedRole = r.RequestedRole,
                        Status = r.Status,
                        RequestedAtUtc = r.RequestedAtUtc,
                        ApprovedAtUtc = r.ApprovedAtUtc,
                        ApprovedBy = approvedByUser == null ? r.ApprovedByUserId : $"{approvedByUser.FirstName} {approvedByUser.LastName}".Trim(),
                        Notes = r.Notes
                    };
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var actor = await _userManager.GetUserAsync(User);
            if (actor == null)
                return Challenge();

            var request = await _context.AccountApprovalRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
            {
                TempData["ErrorMessage"] = "Approval request not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                // Prevent double-processing to keep approval history deterministic.
                TempData["ErrorMessage"] = "This request has already been processed.";
                return RedirectToAction(nameof(Index));
            }

            var targetUser = await _userManager.FindByIdAsync(request.RequestedUserId);
            if (targetUser == null)
            {
                TempData["ErrorMessage"] = "User account not found for this request.";
                return RedirectToAction(nameof(Index));
            }

            targetUser.IsActive = true;
            // Activation happens only after explicit approval by Admin/SuperAdmin.
            await _userManager.UpdateAsync(targetUser);

            request.Status = "Approved";
            request.ApprovedByUserId = actor.Id;
            request.ApprovedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogActivityAsync(actor.Id, "ACCOUNT_APPROVED", "AccountApprovalRequest", id.ToString(), null, request.RequestedUserId);
            TempData["SuccessMessage"] = "Account approved and activated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reason)
        {
            var actor = await _userManager.GetUserAsync(User);
            if (actor == null)
                return Challenge();

            var request = await _context.AccountApprovalRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request == null)
            {
                TempData["ErrorMessage"] = "Approval request not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "This request has already been processed.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["RejectErrorRequestId"] = id;
                TempData["RejectErrorMessage"] = "Reject reason is required.";
                TempData["RejectErrorReason"] = string.Empty;
                return RedirectToAction(nameof(Index), new { status = "Pending" });
            }

            request.Status = "Rejected";
            request.Notes = reason.Trim();
            request.ApprovedByUserId = actor.Id;
            request.ApprovedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogActivityAsync(actor.Id, "ACCOUNT_REJECTED", "AccountApprovalRequest", id.ToString(), request.Notes, request.RequestedUserId);
            TempData["SuccessMessage"] = "Account request rejected.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Passwords(string? search)
        {
            var actor = await _userManager.GetUserAsync(User);
            if (actor == null)
                return Challenge();

            var actorRoles = await _userManager.GetRolesAsync(actor);

            var query = _userManager.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    (u.Email ?? string.Empty).Contains(search) ||
                    (u.EmployeeId ?? string.Empty).Contains(search) ||
                    (u.FirstName ?? string.Empty).Contains(search) ||
                    (u.LastName ?? string.Empty).Contains(search));
            }

            var users = await query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();
            var rows = new List<PasswordManagementListItemViewModel>(users.Count);

            foreach (var user in users)
            {
                var roles = (await _userManager.GetRolesAsync(user)).ToList();
                var canReset = CanResetByRole(actorRoles, roles);

                rows.Add(new PasswordManagementListItemViewModel
                {
                    UserId = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Email = user.Email ?? string.Empty,
                    EmployeeId = user.EmployeeId ?? string.Empty,
                    IsActive = user.IsActive,
                    Roles = roles,
                    CanResetPassword = canReset
                });
            }

            return View(new PasswordManagementIndexViewModel
            {
                SearchTerm = search ?? string.Empty,
                Users = rows
            });
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest();

            var actor = await _userManager.GetUserAsync(User);
            if (actor == null)
                return Challenge();

            var target = await _userManager.FindByIdAsync(userId);
            if (target == null)
                return NotFound();

            var actorRoles = await _userManager.GetRolesAsync(actor);
            var targetRoles = await _userManager.GetRolesAsync(target);

            if (!CanResetByRole(actorRoles, targetRoles))
                return Forbid();

            return View(new AdminPasswordResetViewModel
            {
                UserId = target.Id,
                DisplayName = $"{target.FirstName} {target.LastName}".Trim(),
                Email = target.Email ?? string.Empty,
                RolesText = string.Join(", ", targetRoles)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(AdminPasswordResetViewModel model)
        {
            var actor = await _userManager.GetUserAsync(User);
            if (actor == null)
                return Challenge();

            var target = await _userManager.FindByIdAsync(model.UserId);
            if (target == null)
                return NotFound();

            var actorRoles = await _userManager.GetRolesAsync(actor);
            var targetRoles = await _userManager.GetRolesAsync(target);

            if (!CanResetByRole(actorRoles, targetRoles))
                return Forbid();

            if (!ModelState.IsValid)
            {
                model.DisplayName = $"{target.FirstName} {target.LastName}".Trim();
                model.Email = target.Email ?? string.Empty;
                model.RolesText = string.Join(", ", targetRoles);
                return View(model);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(target);
            var result = await _userManager.ResetPasswordAsync(target, token, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                model.DisplayName = $"{target.FirstName} {target.LastName}".Trim();
                model.Email = target.Email ?? string.Empty;
                model.RolesText = string.Join(", ", targetRoles);
                return View(model);
            }

            await _auditService.LogActivityAsync(actor.Id, "ADMIN_PASSWORD_RESET", "User", target.Id);
            TempData["SuccessMessage"] = "Password updated successfully.";
            return RedirectToAction(nameof(Passwords));
        }

        private static bool CanResetByRole(IList<string> actorRoles, IList<string> targetRoles)
        {
            var actorIsSuperAdmin = actorRoles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase);
            var actorIsAdmin = actorRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
            var targetIsSuperAdmin = targetRoles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase);

            if (actorIsSuperAdmin)
                return true;

            // Admins can reset all non-SuperAdmin accounts, but cannot reset SuperAdmin passwords.
            if (actorIsAdmin && !targetIsSuperAdmin)
                return true;

            return false;
        }
    }
}
