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
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IConcurrentSessionService _concurrentSessionService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IAuditService auditService,
            IConcurrentSessionService concurrentSessionService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _auditService = auditService;
            _concurrentSessionService = concurrentSessionService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            var allowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Staff", "Doctor", "Nurse", "Receptionist", "Accountant", "Pharmacist", "LabTechnician", "Radiologist", "Patient"
            };

            if (!allowedRoles.Contains(model.RequestedRole ?? string.Empty))
            {
                ModelState.AddModelError(nameof(model.RequestedRole), "Invalid role selection.");
            }

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Email is already in use.");
            }

            if (await _userManager.Users.AnyAsync(u => u.EmployeeId == model.EmployeeId))
            {
                ModelState.AddModelError(nameof(model.EmployeeId), "Employee ID is already in use.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmployeeId = model.EmployeeId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                IsActive = false,
                CreatedDate = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }

            if (!await _roleManager.RoleExistsAsync(model.RequestedRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.RequestedRole));
            }

            await _userManager.AddToRoleAsync(user, model.RequestedRole);

            _context.AccountApprovalRequests.Add(new AccountApprovalRequest
            {
                RequestedUserId = user.Id,
                RequestedRole = model.RequestedRole,
                Status = "Pending",
                RequestedAtUtc = DateTime.UtcNow,
                Notes = "Signup request awaiting Admin/SuperAdmin approval."
            });
            await _context.SaveChangesAsync();

            await _auditService.LogActivityAsync(user.Id, "SIGNUP_REQUEST_CREATED", "AccountApprovalRequest", user.Id, null, model.RequestedRole);
            TempData["SuccessMessage"] = "Signup submitted. Your account will be activated after Admin or SuperAdmin approval.";
            return RedirectToAction(nameof(Login));
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

            if (user == null)
                return Json(new { success = false, message = "Invalid credentials." });

            if (!user.IsActive)
            {
                var approvalStatus = await _context.AccountApprovalRequests
                    .Where(r => r.RequestedUserId == user.Id)
                    .Select(r => r.Status)
                    .FirstOrDefaultAsync();

                var inactiveMessage = approvalStatus == "Pending"
                    ? "Your account is pending approval. Please wait for Admin/SuperAdmin activation."
                    : approvalStatus == "Rejected"
                        ? "Your account request was rejected. Please contact Admin or SuperAdmin."
                        : "Your account is inactive. Please contact Admin or SuperAdmin.";

                return Json(new { success = false, message = inactiveMessage });
            }

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
                if (!user.IsActive)
                {
                    var approvalStatus = await _context.AccountApprovalRequests
                        .Where(r => r.RequestedUserId == user.Id)
                        .Select(r => r.Status)
                        .FirstOrDefaultAsync();

                    var inactiveMessage = approvalStatus == "Pending"
                        ? "Your account is pending approval. Please wait for Admin/SuperAdmin activation."
                        : approvalStatus == "Rejected"
                            ? "Your account request was rejected. Please contact Admin or SuperAdmin."
                            : "Your account is inactive. Please contact Admin or SuperAdmin.";

                    ModelState.AddModelError("", inactiveMessage);
                    return View(model);
                }

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
                    var userRoles = await _userManager.GetRolesAsync(user);
                    if (!string.IsNullOrWhiteSpace(model.SelectedRole))
                    {
                        if (!userRoles.Contains(model.SelectedRole, StringComparer.OrdinalIgnoreCase))
                        {
                            // Submitted a role they don't have – ignore it silently and use normal precedence
                            model.SelectedRole = null;
                        }
                    }

                    var activeRole = string.IsNullOrWhiteSpace(model.SelectedRole)
                        ? PickPrimaryRole(userRoles)
                        : model.SelectedRole;

                    var sessionDecision = await _concurrentSessionService.TryRegisterLoginAsync(
                        user.Id,
                        activeRole,
                        HttpContext.Session.Id,
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        Request.Headers.UserAgent.ToString());

                    if (!sessionDecision.IsAllowed)
                    {
                        await _signInManager.SignOutAsync();
                        await _auditService.LogActivityAsync(
                            user.Id,
                            "LOGIN_BLOCKED_CONCURRENT_LIMIT",
                            "User",
                            user.Id,
                            null,
                            sessionDecision.DenyReason);

                        ModelState.AddModelError(string.Empty, sessionDecision.DenyReason ?? "Concurrent user limit reached.");
                        return View(model);
                    }

                    model.SelectedRole = activeRole;

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

            await _concurrentSessionService.EndSessionAsync(HttpContext.Session.Id);
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
