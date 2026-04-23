using MedyxHMS.Data;
using MedyxHMS.DTOs;
using MedyxHMS.Extensions;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

// Purpose: Contains application code for AccountController and its related runtime behavior.
namespace MedyxHMS.Controllers.PatientPortal
{
    [Route("PatientPortal/[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly IPatientPortalService _patientPortalService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            IPatientPortalService patientPortalService,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _patientPortalService = patientPortalService;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // GET: /PatientPortal/Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl ?? Url.Content("~/PatientPortal/Dashboard");
            return View("~/Views/Account/Login.cshtml", new LoginViewModel());
        }

        // POST: /PatientPortal/Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel viewModel, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl ?? Url.Content("~/PatientPortal/Dashboard");

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(viewModel.Email)
                    ?? await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == viewModel.Email);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid email or password");
                    return View("~/Views/Account/Login.cshtml", viewModel);
                }

                if (!user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Your account is inactive. Please contact Admin or SuperAdmin.");
                    return View("~/Views/Account/Login.cshtml", viewModel);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName ?? user.Email ?? viewModel.Email,
                    viewModel.Password,
                    viewModel.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    user.LastLoginDate = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    var roles = await _userManager.GetRolesAsync(user);
                    bool isPatient = roles.Contains("Patient", StringComparer.OrdinalIgnoreCase);

                    if (!isPatient)
                    {
                        // Non-patient users must not be sent to the PatientPortal.
                        // Route them to their appropriate staff dashboard via the main account controller.
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty, "This portal is for patients only. Please use the main login page.");
                        return View("~/Views/Account/Login.cshtml", viewModel);
                    }

                    // Patient portal should only redirect to patient-portal-local paths.
                    if (!string.IsNullOrWhiteSpace(returnUrl)
                        && Url.IsLocalUrl(returnUrl)
                        && returnUrl.StartsWith("/PatientPortal", StringComparison.OrdinalIgnoreCase))
                    {
                        return LocalRedirect(returnUrl);
                    }

                    return LocalRedirect(Url.Content("~/PatientPortal/Dashboard"));
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Your account is locked. Please try again later.");
                    return View(viewModel);
                }

                ModelState.AddModelError(string.Empty, "Invalid email or password");
            }

            return View("~/Views/Account/Login.cshtml", viewModel);
        }

        // GET: /PatientPortal/Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View("~/Views/PatientPortal/Account/Register.cshtml", new PatientPortalRegisterViewModel());
        }

        // POST: /PatientPortal/Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(PatientPortalRegisterViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var normalizedUserName = _userManager.NormalizeName(viewModel.Register.UserName);
                if (!string.IsNullOrWhiteSpace(normalizedUserName) &&
                    await _userManager.Users.AnyAsync(u => u.NormalizedUserName == normalizedUserName))
                {
                    ModelState.AddModelError("Register.UserName", "User name is already in use");
                    return View("~/Views/PatientPortal/Account/Register.cshtml", viewModel);
                }

                try
                {
                    // Generate Patient ID
                    var patientIdPrefix = "PAT";
                    var timestamp = DateTime.Now.Ticks.ToString().Substring(DateTime.Now.Ticks.ToString().Length - 6);
                    var patientId = patientIdPrefix + timestamp;

                    // Create patient object
                    var patient = new Patient
                    {
                        PatientId = patientId,
                        FirstName = viewModel.Register.FirstName,
                        LastName = viewModel.Register.LastName,
                        Email = viewModel.Register.Email,
                        Phone = viewModel.Register.Phone,
                        DateOfBirth = viewModel.Register.DateOfBirth,
                        Gender = viewModel.Register.Gender,
                        User = new ApplicationUser
                        {
                            UserName = viewModel.Register.UserName,
                            Email = viewModel.Register.Email,
                            PhoneNumber = viewModel.Register.Phone
                        }
                    };

                    // Register patient
                    var registeredPatient = await _patientPortalService.RegisterPatientAsync(
                        patient,
                        viewModel.Register.Password);

                    if (registeredPatient != null)
                    {
                        // Create approval request instead of signing in immediately
                        var existingRequest = await _context.AccountApprovalRequests
                            .AnyAsync(r => r.RequestedUserId == registeredPatient.UserId);

                        if (!existingRequest && registeredPatient.UserId != null)
                        {
                            // Ensure account is inactive until approved
                            var appUser = await _userManager.FindByIdAsync(registeredPatient.UserId);
                            if (appUser != null)
                            {
                                appUser.IsActive = false;
                                await _userManager.UpdateAsync(appUser);
                            }

                            _context.AccountApprovalRequests.Add(new MedyxHMS.Models.AccountApprovalRequest
                            {
                                RequestedUserId = registeredPatient.UserId,
                                RequestedRole = "Patient",
                                Status = "Pending",
                                RequestedAtUtc = DateTime.UtcNow,
                                Notes = "Patient self-registration awaiting Admin/SuperAdmin approval."
                            });
                            await _context.SaveChangesAsync();
                        }

                        TempData["SuccessMessage"] = "Registration submitted successfully. Your account will be activated after Admin or SuperAdmin approval. You will receive a notification once approved.";
                        return RedirectToAction("Login");
                    }

                    ModelState.AddModelError(string.Empty, "Patient registration failed");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Registration error: {ex.Message}");
                }
            }

            return View("~/Views/PatientPortal/Account/Register.cshtml", viewModel);
        }

        // POST: /PatientPortal/Account/Logout
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["SuccessMessage"] = "You have been logged out successfully";
            return RedirectToAction("Login");
        }

        // GET: /PatientPortal/Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new PatientPortalForgotPasswordViewModel());
        }

        // POST: /PatientPortal/Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(PatientPortalForgotPasswordViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(viewModel.Email);
                if (user != null)
                {
                    // In production, send email with reset link
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var resetUrl = Url.Action("ResetPassword", "Account",
                        new { token, email = user.Email },
                        protocol: Request.Scheme);

                    // TODO: Send email with reset link
                    TempData["SuccessMessage"] = "If an account exists with this email, a password reset link has been sent.";
                }
                else
                {
                    // Don't reveal if account exists for security
                    TempData["SuccessMessage"] = "If an account exists with this email, a password reset link has been sent.";
                }

                return RedirectToAction("Login");
            }

            return View(viewModel);
        }

        // GET: /PatientPortal/Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? token = null, string? email = null)
        {
            if (token == null || email == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid password reset request");
            }

            var viewModel = new PatientPortalResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(viewModel);
        }

        // POST: /PatientPortal/Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(PatientPortalResetPasswordViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var user = await _userManager.FindByEmailAsync(viewModel.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found");
                return View(viewModel);
            }

            var result = await _userManager.ResetPasswordAsync(user, viewModel.Token, viewModel.NewPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Your password has been reset successfully. Please log in.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(viewModel);
        }

        // GET: /PatientPortal/Account/ConfirmEmail
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found");
                return View();
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            var viewModel = new PatientPortalEmailConfirmationViewModel
            {
                Email = user.Email ?? string.Empty,
                IsConfirmed = result.Succeeded,
                Message = result.Succeeded ? "Email confirmed successfully" : "Email confirmation failed"
            };

            return View(viewModel);
        }

        // GET: /PatientPortal/Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
