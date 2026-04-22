using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for AccountController and its related runtime behavior.
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
        private readonly ILicenseService _licenseService;
        private readonly IEmailNotificationProvider _emailProvider;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IAuditService auditService,
            IConcurrentSessionService concurrentSessionService,
            ILicenseService licenseService,
            IEmailNotificationProvider emailProvider)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _auditService = auditService;
            _concurrentSessionService = concurrentSessionService;
            _licenseService = licenseService;
            _emailProvider = emailProvider;
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
            model.UserName = (model.UserName ?? string.Empty).Trim();

            var isSuperAdmin = User.Identity?.IsAuthenticated == true && User.IsInRole("SuperAdmin");

            var allowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Staff", "Doctor", "Nurse", "Receptionist", "Accountant", "Pharmacist", "LabTechnician", "Radiologist", "Patient"
            };

            // Only SuperAdmin can assign Admin or SuperAdmin roles
            if ((model.RequestedRole == "Admin" || model.RequestedRole == "SuperAdmin") && !isSuperAdmin)
            {
                ModelState.AddModelError(nameof(model.RequestedRole), "Only a SuperAdmin can create Admin or SuperAdmin accounts.");
            }
            else if (model.RequestedRole == "Admin" || model.RequestedRole == "SuperAdmin")
            {
                allowedRoles.Add("Admin");
                allowedRoles.Add("SuperAdmin");
            }

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

            var normalizedUserName = _userManager.NormalizeName(model.UserName);
            if (!string.IsNullOrWhiteSpace(normalizedUserName) &&
                await _userManager.Users.AnyAsync(u => u.NormalizedUserName == normalizedUserName))
            {
                ModelState.AddModelError(nameof(model.UserName), "User name is already in use.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                Id = await GetNextNumericUserIdAsync(),
                UserName = model.UserName,
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

        private async Task<string> GetNextNumericUserIdAsync()
        {
            var maxId = await _userManager.Users
                .Select(u => (int?)ConvertToNumericUserId(u.Id))
                .MaxAsync() ?? 0;

            return (maxId + 1).ToString();
        }

        private static int ConvertToNumericUserId(string? rawId)
        {
            if (string.IsNullOrWhiteSpace(rawId))
                return 0;

            return int.TryParse(rawId, out var numericId) ? numericId : 0;
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
        /// Credentials are NOT persisted/signed-in here - only checked so the UI can
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
                    // Legacy bcrypt migration path: if old hash verification succeeds,
                    // reset to Identity hash and retry sign-in once.
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
                            // Submitted a role they don't have - ignore it silently and use normal precedence
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
                        // Enforce concurrent-user licensing/session limits at login time.
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

                    // â”€â”€ License expiry gate â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
                    var snapshot = await _licenseService.GetCurrentSnapshotAsync();
                    if (snapshot.State == LicenseState.Expired)
                    {
                        var isPrivileged = model.SelectedRole == "SuperAdmin" || model.SelectedRole == "Patient";
                        if (!isPrivileged)
                        {
                            if (model.SelectedRole == "Admin")
                            {
                                // Admin may log in but is limited to the license-expired page
                                return RedirectToAction(nameof(LicenseExpired));
                            }
                            else
                            {
                                // All other roles cannot log in when license is expired
                                await _signInManager.SignOutAsync();
                                await _concurrentSessionService.EndSessionAsync(HttpContext.Session.Id);
                                HttpContext.Session.Remove("ActiveRole");
                                ModelState.AddModelError(string.Empty, "Your license has expired. Please contact your administrator.");
                                return View(model);
                            }
                        }
                    }

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
            return LocalRedirect("/Account/Login");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public IActionResult LicenseExpired()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> RequestLicenseFile()
        {
            var requestingUser = await _userManager.GetUserAsync(User);
            var requestingName = requestingUser != null
                ? $"{requestingUser.FirstName} {requestingUser.LastName} ({requestingUser.Email})"
                : User.Identity?.Name ?? "Admin";

            // Get all SuperAdmin emails
            var superAdminRoleId = await _context.Set<IdentityRole>()
                .Where(r => r.Name == "SuperAdmin")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var superAdminEmails = new List<string>();
            if (!string.IsNullOrWhiteSpace(superAdminRoleId))
            {
                superAdminEmails = await _context.UserRoles
                    .Where(ur => ur.RoleId == superAdminRoleId)
                    .Join(_context.Users, ur => ur.UserId, u => u.Id, (ur, u) => u)
                    .Where(u => u.IsActive && !string.IsNullOrWhiteSpace(u.Email))
                    .Select(u => u.Email!)
                    .ToListAsync();
            }

            var sentCount = 0;
            foreach (var email in superAdminEmails)
            {
                try
                {
                    await _emailProvider.SendAsync(
                        email,
                        "License File Request - MedyxHMS",
                        $"<p>A license renewal request has been submitted by <strong>{System.Net.WebUtility.HtmlEncode(requestingName)}</strong>.</p>" +
                        $"<p>The current license has expired. Please generate and upload a new license file at your earliest convenience.</p>" +
                        $"<p>Requested at: {DateTime.UtcNow:f} UTC</p>");
                    sentCount++;
                }
                catch { /* best-effort */ }
            }

            await _auditService.LogActivityAsync(requestingUser?.Id, "LICENSE_REQUEST_SENT", "License", null, null,
                $"License request emailed to {sentCount} SuperAdmin(s).");

            TempData["SuccessMessage"] = sentCount > 0
                ? $"License renewal request sent to {sentCount} SuperAdmin(s)."
                : "No active SuperAdmin email found. Please contact your system administrator directly.";

            return RedirectToAction(nameof(LicenseExpired));
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
            // Resolve active role first so we can validate the returnUrl against it.
            var roleToUse = selectedRole;
            if (string.IsNullOrWhiteSpace(roleToUse))
            {
                var roles = await _userManager.GetRolesAsync(user);
                roleToUse = PickPrimaryRole(roles);
            }

            bool isPatientRole = string.Equals(roleToUse, "Patient", StringComparison.OrdinalIgnoreCase);

            // Only honour a returnUrl when it belongs to the same "zone" as the active role.
            // PatientPortal URLs must never be used as landing pages for staff roles, and vice versa.
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                bool isPatientPortalUrl = returnUrl.StartsWith("/PatientPortal", StringComparison.OrdinalIgnoreCase);
                if (isPatientRole == isPatientPortalUrl)
                    return Redirect(returnUrl);
                // returnUrl zone doesn't match the role - fall through to role-based routing below.
            }

            return roleToUse switch
            {
                "Patient"       => LocalRedirect("/PatientPortal/Dashboard"),
                "Receptionist"  => LocalRedirect("/FrontOffice"),
                "Accountant"    => LocalRedirect("/Billing"),
                "Pharmacist"    => LocalRedirect("/Prescription"),
                "Nurse"         => LocalRedirect("/IPD"),
                "Doctor"        => LocalRedirect("/OPD"),
                _               => LocalRedirect("/Dashboard"),
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
