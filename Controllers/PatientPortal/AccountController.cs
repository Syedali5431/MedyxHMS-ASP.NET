using MedyxHMS.Data;
using MedyxHMS.DTOs;
using MedyxHMS.Extensions;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
                var result = await _signInManager.PasswordSignInAsync(
                    viewModel.Email,
                    viewModel.Password,
                    viewModel.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(viewModel.Email);
                    if (user != null)
                    {
                        user.LastLoginDate = DateTime.UtcNow;
                        await _userManager.UpdateAsync(user);
                    }

                    return LocalRedirect(returnUrl ?? Url.Content("~/PatientPortal/Dashboard"));
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
            return View(new PatientPortalRegisterViewModel());
        }

        // POST: /PatientPortal/Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(PatientPortalRegisterViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
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
                        // Sign in the user
                        await _signInManager.SignInAsync(registeredPatient.User, isPersistent: false);

                        TempData["SuccessMessage"] = "Patient registration successful!";
                        return RedirectToAction("Index", "Dashboard", new { area = "PatientPortal" });
                    }

                    ModelState.AddModelError(string.Empty, "Patient registration failed");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Registration error: {ex.Message}");
                }
            }

            return View(viewModel);
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