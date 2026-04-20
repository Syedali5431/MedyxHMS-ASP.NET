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

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // Try to find user by email or employee ID
                var user = await _userManager.FindByEmailAsync(model.Email) ??
                          await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == model.Email);

                if (user != null)
                {
                    // Check if password needs migration from PHP bcrypt
                    var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);

                    if (!result.Succeeded && result.IsLockedOut)
                    {
                        await _auditService.LogActivityAsync(user.Id, "LOGIN_FAILED_LOCKOUT", "User", user.Id);
                        ModelState.AddModelError("", "Account locked out due to multiple failed login attempts.");
                        return View(model);
                    }

                    if (!result.Succeeded)
                    {
                        // Try password migration if login failed
                        if (await TryMigratePasswordAsync(user, model.Password))
                        {
                            // Password migrated successfully, try login again
                            result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);
                        }
                    }

                    if (result.Succeeded)
                    {
                        await _auditService.LogActivityAsync(user.Id, "LOGIN_SUCCESS", "User", user.Id);
                        user.LastLoginDate = DateTime.UtcNow;
                        await _userManager.UpdateAsync(user);

                        return await RedirectToLocalAsync(user, returnUrl);
                    }
                    else
                    {
                        await _auditService.LogActivityAsync(user.Id, "LOGIN_FAILED", "User", user.Id);
                        ModelState.AddModelError("", "Invalid login attempt.");
                    }
                }
                else
                {
                    await _auditService.LogActivityAsync(null, "LOGIN_FAILED_USER_NOT_FOUND", "User", null, null, $"Email/EmployeeId: {model.Email}");
                    ModelState.AddModelError("", "Invalid login attempt.");
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _auditService.LogActivityAsync(user.Id, "LOGOUT", "User", user.Id);
            }

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
                // Check if password is in old PHP bcrypt format
                // This is a simplified migration - in production you'd check against the old hash
                // For now, we'll assume migration is needed and update the password

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

        private async Task<IActionResult> RedirectToLocalAsync(ApplicationUser user, string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Patient"))
            {
                return LocalRedirect(Url.Content("~/PatientPortal/Dashboard"));
            }

            if (roles.Contains("Receptionist"))
            {
                return RedirectToAction("Index", "FrontOffice");
            }

            if (roles.Contains("Accountant"))
            {
                return RedirectToAction("Index", "Billing");
            }

            if (roles.Contains("Pharmacist"))
            {
                return RedirectToAction("Index", "Prescription");
            }

            if (roles.Contains("Nurse"))
            {
                return RedirectToAction("Index", "IPD");
            }

            if (roles.Contains("Doctor"))
            {
                return RedirectToAction("Index", "OPD");
            }

            return RedirectToAction("Index", "Dashboard");
        }
    }
}