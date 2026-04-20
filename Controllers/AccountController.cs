using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IAuditService auditService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _auditService = auditService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// AJAX endpoint: validate credentials and return the roles assigned to the user.
        /// Credentials are NOT persisted/signed-in here – only checked so the UI can
        /// display only the roles relevant to that user.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateCredentials([FromForm] string email, [FromForm] string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return Json(new { success = false, message = "Email and password are required." });

            var user = await _userManager.FindByEmailAsync(email)
                    ?? await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == email);

            if (user == null || !user.IsActive)
                return Json(new { success = false, message = "Invalid credentials." });

            // Check password without signing in
            var passwordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordValid)
            {
                // Attempt bcrypt migration check (same logic as full login)
                var migrated = await TryMigratePasswordAsync(user, password);
                if (!migrated)
                    return Json(new { success = false, message = "Invalid credentials." });
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Count == 0)
                return Json(new { success = false, message = "No roles assigned to this account." });

            return Json(new { success = true, roles = roles.OrderBy(r => r).ToList() });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            // Try to find user by email or employee ID
            var user = await _userManager.FindByEmailAsync(model.Email)
                    ?? await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == model.Email);

            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.IsLockedOut)
                {
                    await _auditService.LogActivityAsync(user.Id, "LOGIN_FAILED_LOCKOUT", "User", user.Id);
                    ModelState.AddModelError("", "Account locked out due to multiple failed login attempts.");
                    return View(model);
                }

                if (!result.Succeeded)
                {
                    if (await TryMigratePasswordAsync(user, model.Password))
                        result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);
                }

                if (result.Succeeded)
                {
                    await _auditService.LogActivityAsync(user.Id, "LOGIN_SUCCESS", "User", user.Id);
                    user.LastLoginDate = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    // Validate the selected role actually belongs to the user
                    if (!string.IsNullOrWhiteSpace(model.SelectedRole))
                    {
                        var userRoles = await _userManager.GetRolesAsync(user);
                        if (!userRoles.Contains(model.SelectedRole, StringComparer.OrdinalIgnoreCase))
                        {
                            // Submitted a role they don't have – ignore it silently and use normal precedence
                            model.SelectedRole = null;
                        }
                    }

                    // Persist the active role for this session so the navigation can use it
                    if (!string.IsNullOrWhiteSpace(model.SelectedRole))
                        HttpContext.Session.SetString("ActiveRole", model.SelectedRole);

                    return await RedirectToLocalAsync(user, model.SelectedRole, returnUrl);
                }

                await _auditService.LogActivityAsync(user.Id, "LOGIN_FAILED", "User", user.Id);
                ModelState.AddModelError("", "Invalid login attempt.");
            }
            else
            {
                await _auditService.LogActivityAsync(null, "LOGIN_FAILED_USER_NOT_FOUND", "User", null, null, $"Email/EmployeeId: {model.Email}");
                ModelState.AddModelError("", "Invalid login attempt.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
                await _auditService.LogActivityAsync(user.Id, "LOGOUT", "User", user.Id);

            HttpContext.Session.Remove("ActiveRole");
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task<bool> TryMigratePasswordAsync(ApplicationUser user, string password)
        {
            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, password);

                if (result.Succeeded)
                {
                    await _auditService.LogActivityAsync(user.Id, "PASSWORD_MIGRATED", "User", user.Id);
                    return true;
                }
            }
            catch (Exception ex)
            {
                await _auditService.LogActivityAsync(user.Id, "PASSWORD_MIGRATION_FAILED", "User", user.Id, null, $"Error: {ex.Message}");
            }

            return false;
        }

        private async Task<IActionResult> RedirectToLocalAsync(ApplicationUser user, string? selectedRole, string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // If the user selected a specific role, route to that role's dashboard
            var roleToUse = selectedRole;

            // Fallback: derive from assigned roles using priority order
            if (string.IsNullOrWhiteSpace(roleToUse))
            {
                var roles = await _userManager.GetRolesAsync(user);
                roleToUse = PickPrimaryRole(roles);
            }

            return roleToUse switch
            {
                "Patient"       => LocalRedirect(Url.Content("~/PatientPortal/Dashboard")),
                "Receptionist"  => RedirectToAction("Index", "FrontOffice"),
                "Accountant"    => RedirectToAction("Index", "Billing"),
                "Pharmacist"    => RedirectToAction("Index", "Prescription"),
                "Nurse"         => RedirectToAction("Index", "IPD"),
                "Doctor"        => RedirectToAction("Index", "OPD"),
                _               => RedirectToAction("Index", "Dashboard"),
            };
        }

        /// <summary>
        /// Returns the highest-priority role from a set of assigned roles when no
        /// explicit selection was made by the user.
        /// </summary>
        private static string PickPrimaryRole(IList<string> roles)
        {
            foreach (var candidate in new[] { "SuperAdmin", "Admin", "Doctor", "Nurse", "Pharmacist", "Accountant", "Receptionist", "Patient" })
            {
                if (roles.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                    return candidate;
            }

            return roles.FirstOrDefault() ?? "Admin";
        }
    }
}
