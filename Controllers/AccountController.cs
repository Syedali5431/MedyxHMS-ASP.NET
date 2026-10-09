using MedyxHMS.Data;
using MedyxHMS.Extensions;
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
                CreatedDate = DateTime.Now
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
            // ConvertToNumericUserId is a plain C# method and can't be translated to SQL, so the
            // ids must be materialized first and converted client-side before taking the max.
            var userIds = await _userManager.Users.Select(u => u.Id).ToListAsync();
            var maxId = userIds.Count == 0 ? 0 : userIds.Max(ConvertToNumericUserId);

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
                    ?? await _userManager.FindByNameAsync(email)
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

            // Check password without signing in. Failed attempts count toward lockout, the same as
            // the full login, so this anonymous endpoint cannot be used to guess passwords freely.
            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
            if (passwordCheck.IsLockedOut)
                return Json(new { success = false, message = "Account locked out due to multiple failed login attempts." });
            if (!passwordCheck.Succeeded)
                return Json(new { success = false, message = "Invalid credentials." });

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Count == 0)
                return Json(new { success = false, message = "No roles assigned to this account." });

            var displayName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = user.UserName ?? email;

            // Hospitals the staff member can work in. With one hospital the page signs in straight away; with
            // several it asks which hospital to open first.
            var hospitals = new List<object>();
            string? selectedHospital = null;
            if (roles.Any(r => !string.Equals(r, "Patient", StringComparison.OrdinalIgnoreCase)))
            {
                var isSuperAdmin = roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase);
                var (list, defaultId) = await HospitalContextMiddleware.GetUserHospitalsAsync(_context, user.Id, isSuperAdmin);
                if (isSuperAdmin && list.Count > 1)
                {
                    hospitals.Add(new { value = HospitalContextMiddleware.AllHospitalsValue, name = "All hospitals", city = "Every hospital of the group", isDefault = false });
                }

                hospitals.AddRange(list.Select(h => (object)new { value = h.Id.ToString(), name = h.Name, city = h.City, isDefault = h.Id == defaultId }));
                var previous = HospitalContextMiddleware.ReadSelection(Request, user.Id);
                selectedHospital = previous != null && (list.Any(h => h.Id.ToString() == previous) || (isSuperAdmin && list.Count > 1 && previous == HospitalContextMiddleware.AllHospitalsValue))
                    ? previous
                    : defaultId?.ToString();
            }

            return Json(new { success = true, roles = roles.OrderBy(r => r).ToList(), primaryRole = PickPrimaryRole(roles), displayName, hospitals, selectedHospital });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            // Try to find user by email, username, or employee ID
            var user = await _userManager.FindByEmailAsync(model.Email)
                    ?? await _userManager.FindByNameAsync(model.Email)
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

                if (result.Succeeded)
                {
                    // Force password change if using default password
                    if (model.Password == "Medyx147")
                    {
                        // PasswordSignInAsync has already issued the sign-in cookie; remove it so the password
                        // must really be changed before the system can be used.
                        await _signInManager.SignOutAsync();
                        HttpContext.Session.SetString("ForcePwd_UserId", user.Id);
                        HttpContext.Session.SetString("ForcePwd_ReturnUrl", returnUrl ?? "");
                        return RedirectToAction("ForceChangePassword");
                    }

                    if (user.MFAEnabled)
                    {
                        // Not signed in until the authenticator code is verified (previously the cookie from
                        // PasswordSignInAsync stayed valid, so the code page could simply be skipped).
                        await _signInManager.SignOutAsync();
                        HttpContext.Session.SetInt32("MFA_Attempts", 0);
                        HttpContext.Session.SetString("MFA_UserId", user.Id);
                        HttpContext.Session.SetString("MFA_RememberMe", model.RememberMe.ToString());
                        HttpContext.Session.SetString("MFA_ReturnUrl", returnUrl ?? "");
                        HttpContext.Session.SetString("MFA_Role", model.SelectedRole ?? "");
                        HttpContext.Session.SetString("MFA_Hospital", model.SelectedHospital ?? "");
                        return RedirectToAction("VerifyMFA");
                    }

                    await _auditService.LogActivityAsync(user.Id, "LOGIN_SUCCESS", "User", user.Id);
                    user.LastLoginDate = DateTime.Now;
                    await _userManager.UpdateAsync(user);
                    // The admin two-step check caches the MFA status: read it fresh for the new session.
                    HttpContext.RequestServices.GetRequiredService<ISecurityPolicyService>().ForgetMfaStatus(user.Id);

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

                    // Work in the hospital chosen on the sign-in page (its data is shown from the first page on).
                    await ApplyHospitalChoiceAsync(user, userRoles, model.SelectedHospital);

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
                "SuperAdmin"    => LocalRedirect("/Dashboard"),
                "Admin"         => LocalRedirect("/Dashboard"),
                "Patient"       => LocalRedirect("/PatientPortal/Dashboard"),
                "Receptionist"  => LocalRedirect("/FrontOffice"),
                "Accountant"    => LocalRedirect("/Billing"),
                "Pharmacist"    => LocalRedirect("/Prescription"),
                "Nurse"         => LocalRedirect("/IPD"),
                "Doctor"        => LocalRedirect("/OPD"),
                "LabTechnician" => LocalRedirect("/Lab"),
                "Radiologist"   => LocalRedirect("/Radiology"),
                "Staff"         => LocalRedirect("/Dashboard"),
                _               => LocalRedirect("/Dashboard"),
            };
        }

        /// <summary>
        /// Remembers the hospital chosen on the sign-in page (only one the user has access to). Without a valid
        /// choice the user's default hospital is used, so every sign-in starts in a known hospital.
        /// </summary>
        private async Task ApplyHospitalChoiceAsync(ApplicationUser user, IList<string> roles, string? choice)
        {
            if (!roles.Any(r => !string.Equals(r, "Patient", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var isSuperAdmin = roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase);
            var (hospitals, defaultId) = await HospitalContextMiddleware.GetUserHospitalsAsync(_context, user.Id, isSuperAdmin);
            if (hospitals.Count == 0)
            {
                return;
            }

            string value;
            if (isSuperAdmin && hospitals.Count > 1 && string.Equals(choice, HospitalContextMiddleware.AllHospitalsValue, StringComparison.OrdinalIgnoreCase))
            {
                value = HospitalContextMiddleware.AllHospitalsValue;
            }
            else if (int.TryParse(choice, out var id) && hospitals.Any(h => h.Id == id))
            {
                value = id.ToString();
            }
            else
            {
                value = (defaultId ?? hospitals[0].Id).ToString();
            }

            HospitalContextMiddleware.WriteSelection(HttpContext, user.Id, value);
            var name = value == HospitalContextMiddleware.AllHospitalsValue ? "All hospitals" : hospitals.First(h => h.Id.ToString() == value).Name;
            if (hospitals.Count > 1 || value == HospitalContextMiddleware.AllHospitalsValue)
            {
                TempData["InfoMessage"] = $"Working in: {name}.";
            }

            await _auditService.LogActivityAsync(user.Id, "HOSPITAL_SELECTED", "Hospital", value, null, $"Signed in to {name}");
        }

        /// <summary>
        /// Returns the highest-priority role from a set of assigned roles when no
        /// explicit selection was made by the user.
        /// </summary>
        private static string PickPrimaryRole(IList<string> roles)
        {
            foreach (var candidate in new[] { "SuperAdmin", "Admin", "Doctor", "Nurse", "Pharmacist", "Accountant", "Receptionist", "LabTechnician", "Radiologist", "Staff", "Patient" })
            {
                if (roles.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                    return candidate;
            }

            return roles.FirstOrDefault() ?? "Admin";
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Login");
            return View(user);
        }

        // POST /Account/UpdateProfile – personal details from My Profile (e-mail, user name and roles stay with the administrators).
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([Bind(Prefix = "Edit")] MyProfileEditViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var user = string.IsNullOrEmpty(userId) ? null : await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Login");

            if (model.DateOfBirth.HasValue && (model.DateOfBirth.Value.Date >= DateTime.Today || model.DateOfBirth.Value.Year < 1900))
            {
                ModelState.AddModelError("Edit.DateOfBirth", "Enter a valid date of birth.");
            }
            if (!string.IsNullOrWhiteSpace(model.Gender) && !MyProfileEditViewModel.Genders.Contains(model.Gender))
            {
                ModelState.AddModelError("Edit.Gender", "Choose a gender from the list.");
            }

            string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            var before = $"{user.FirstName} {user.LastName}, {user.PhoneNumber}";
            user.FirstName = model.FirstName?.Trim() ?? user.FirstName;
            user.LastName = model.LastName?.Trim() ?? user.LastName;
            user.PhoneNumber = Clean(model.PhoneNumber);
            user.Gender = Clean(model.Gender);
            user.DateOfBirth = model.DateOfBirth?.Date;
            user.Address = Clean(model.Address);
            user.City = Clean(model.City);
            user.EmergencyContactName = Clean(model.EmergencyContactName);
            user.EmergencyContactPhone = Clean(model.EmergencyContactPhone);
            user.About = Clean(model.About);

            if (!ModelState.IsValid)
            {
                // Show the form again with the entered values and the messages.
                return View("Profile", user);
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return View("Profile", user);
            }

            // Keep the staff record (HR) in step with the account.
            var staff = await _context.Staff.FirstOrDefaultAsync(s => s.Id == user.Id);
            if (staff != null)
            {
                staff.FirstName = user.FirstName;
                staff.LastName = user.LastName;
                if (!string.IsNullOrWhiteSpace(user.PhoneNumber)) staff.Phone = user.PhoneNumber;
                if (!string.IsNullOrWhiteSpace(user.Address)) staff.Address = string.Join(", ", new[] { user.Address, user.City }.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (user.About != null) staff.About = user.About;
                await _context.SaveChangesAsync();
            }

            await _auditService.LogActivityAsync(userId, "PROFILE_UPDATE", "User", userId, before, $"{user.FirstName} {user.LastName}, {user.PhoneNumber}");
            TempData["SuccessMessage"] = "Your profile has been updated.";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfileImage(IFormFile profileImage)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

            try
            {
                var profileService = HttpContext.RequestServices.GetRequiredService<IProfileImageService>();
                var fileName = await profileService.UploadAsync(userId, profileImage);

                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    if (fileName == null)
                    {
                        TempData["ErrorMessage"] = "Choose a JPG or PNG picture first.";
                        return RedirectToAction("Profile");
                    }
                    user.ProfileImage = fileName;
                    await _userManager.UpdateAsync(user);
                }

                await _auditService.LogActivityAsync(userId, "PROFILE_IMAGE_UPLOAD", "User", userId);
                TempData["SuccessMessage"] = "Profile picture updated.";
            }
            catch (InvalidOperationException ex)
            {
                // The page is shown again after a redirect: carry the message in TempData.
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfileImage()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

            var profileService = HttpContext.RequestServices.GetRequiredService<IProfileImageService>();
            await profileService.DeleteAsync(userId);

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.ProfileImage = null;
                await _userManager.UpdateAsync(user);
            }

            await _auditService.LogActivityAsync(userId, "PROFILE_IMAGE_DELETE", "User", userId);
            return RedirectToAction("Profile");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EnableMFA(bool required = false)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Login");
            ViewBag.Required = required;
            if (user.MFAEnabled)
            {
                TempData["InfoMessage"] = "MFA is already enabled.";
                return RedirectToAction("Profile");
            }
            var mfaService = HttpContext.RequestServices.GetRequiredService<IMFAService>();
            var qrUri = await mfaService.BeginSetupAsync(userId, user.Email);
            var secret = new Uri(qrUri).Query.TrimStart('?').Split('&')
                .FirstOrDefault(p => p.StartsWith("secret="))?.Replace("secret=", "") ?? "";
            ViewBag.SecretKey = secret;
            return View("EnableMFA", qrUri);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteMFASetup(string code)
        {
            var userId = _userManager.GetUserId(User);
            var mfaService = HttpContext.RequestServices.GetRequiredService<IMFAService>();
            if (await mfaService.CompleteSetupAsync(userId, code))
            {
                HttpContext.RequestServices.GetRequiredService<ISecurityPolicyService>().ForgetMfaStatus(userId);
                TempData["SuccessMessage"] = "MFA has been enabled.";
                return RedirectToAction("Profile");
            }
            ModelState.AddModelError("code", "Invalid code. Try again.");
            var user = await _userManager.FindByIdAsync(userId);
            ViewBag.SecretKey = user?.MFATempSecret;
            var qrUri = $"otpauth://totp/MedyxHMS:{user?.Email}?secret={user?.MFATempSecret}&issuer=MedyxHMS";
            return View("EnableMFA", qrUri);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableMFA(string password)
        {
            var userId = _userManager.GetUserId(User);
            var securityPolicy = HttpContext.RequestServices.GetRequiredService<ISecurityPolicyService>();
            if ((User.IsInRole("SuperAdmin") || User.IsInRole("Admin")) && (await securityPolicy.GetPolicyAsync()).RequireMfaForAdmins)
            {
                TempData["ErrorMessage"] = "Two-step login is required for administrators and cannot be switched off.";
                return RedirectToAction("Profile");
            }

            var mfaService = HttpContext.RequestServices.GetRequiredService<IMFAService>();
            var disabled = await mfaService.DisableAsync(userId, password);
            securityPolicy.ForgetMfaStatus(userId);
            TempData[disabled ? "SuccessMessage" : "ErrorMessage"]
                = disabled ? "MFA disabled." : "Incorrect password.";
            return RedirectToAction("Profile");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyMFA()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("MFA_UserId")))
                return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyMFA(MFAVerifyViewModel model)
        {
            var userId = HttpContext.Session.GetString("MFA_UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");
            if (!ModelState.IsValid) return View(model);

            var mfaService = HttpContext.RequestServices.GetRequiredService<IMFAService>();
            if (!await mfaService.ValidateLoginMfaAsync(userId, model.Code))
            {
                // At most 5 wrong codes per sign-in; each also counts towards the account lock-out.
                var attempts = (HttpContext.Session.GetInt32("MFA_Attempts") ?? 0) + 1;
                HttpContext.Session.SetInt32("MFA_Attempts", attempts);
                var failedUser = await _userManager.FindByIdAsync(userId);
                if (failedUser != null)
                {
                    await _userManager.AccessFailedAsync(failedUser);
                }

                if (attempts >= 5 || (failedUser != null && await _userManager.IsLockedOutAsync(failedUser)))
                {
                    HttpContext.Session.Remove("MFA_UserId");
                    HttpContext.Session.Remove("MFA_RememberMe");
                    HttpContext.Session.Remove("MFA_ReturnUrl");
                    HttpContext.Session.Remove("MFA_Attempts");
                    TempData["ErrorMessage"] = "Too many incorrect codes. Please sign in again.";
                    return RedirectToAction("Login");
                }

                ModelState.AddModelError("Code", "Invalid verification code.");
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(userId);
            var rememberMe = HttpContext.Session.GetString("MFA_RememberMe") == "True";
            var returnUrl = HttpContext.Session.GetString("MFA_ReturnUrl");
            var chosenRole = HttpContext.Session.GetString("MFA_Role");
            var chosenHospital = HttpContext.Session.GetString("MFA_Hospital");
            HttpContext.Session.Remove("MFA_UserId");
            HttpContext.Session.Remove("MFA_RememberMe");
            HttpContext.Session.Remove("MFA_ReturnUrl");
            HttpContext.Session.Remove("MFA_Attempts");
            HttpContext.Session.Remove("MFA_Role");
            HttpContext.Session.Remove("MFA_Hospital");
            await _userManager.ResetAccessFailedCountAsync(user);

            await _signInManager.SignInAsync(user, rememberMe);
            await _auditService.LogActivityAsync(user.Id, "LOGIN_SUCCESS_MFA", "User", user.Id);
            user.LastLoginDate = DateTime.Now;
            await _userManager.UpdateAsync(user);

            var userRoles = await _userManager.GetRolesAsync(user);
            // The role and hospital chosen on the sign-in page apply after the code is verified.
            var activeRole = !string.IsNullOrWhiteSpace(chosenRole) && userRoles.Contains(chosenRole, StringComparer.OrdinalIgnoreCase)
                ? userRoles.First(r => string.Equals(r, chosenRole, StringComparison.OrdinalIgnoreCase))
                : PickPrimaryRole(userRoles);
            var sessionDecision = await _concurrentSessionService.TryRegisterLoginAsync(
                user.Id, activeRole, HttpContext.Session.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());
            if (!sessionDecision.IsAllowed) { await _signInManager.SignOutAsync(); return RedirectToAction("AccessDenied"); }
            HttpContext.Session.SetString("ActiveRole", activeRole);
            await ApplyHospitalChoiceAsync(user, userRoles, chosenHospital);
            return await RedirectToLocalAsync(user, activeRole, returnUrl);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> TestMFA(string code)
        {
            var userId = _userManager.GetUserId(User);
            var mfaService = HttpContext.RequestServices.GetRequiredService<IMFAService>();
            return Json(new { success = await mfaService.TestCodeAsync(userId, code) });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForceChangePassword()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("ForcePwd_UserId")))
                return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForceChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetString("ForcePwd_UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login");

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Passwords do not match.");
                return View();
            }

            if (newPassword.Length < 8)
            {
                ModelState.AddModelError("newPassword", "Password must be at least 8 characters.");
                return View();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);
                return View();
            }

            var returnUrl = HttpContext.Session.GetString("ForcePwd_ReturnUrl");
            HttpContext.Session.Remove("ForcePwd_UserId");
            HttpContext.Session.Remove("ForcePwd_ReturnUrl");

            await _signInManager.SignInAsync(user, false);
            await _auditService.LogActivityAsync(userId, "PASSWORD_CHANGED_FORCED", "User", userId);
            TempData["SuccessMessage"] = "Password changed successfully.";

            var userRoles = await _userManager.GetRolesAsync(user);
            var activeRole = PickPrimaryRole(userRoles);
            HttpContext.Session.SetString("ActiveRole", activeRole);
            await ApplyHospitalChoiceAsync(user, userRoles, null);
            return await RedirectToLocalAsync(user, activeRole, returnUrl);
        }
    }
}
