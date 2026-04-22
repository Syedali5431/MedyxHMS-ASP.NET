using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

// Purpose: Contains application code for LicenseExpiryFilter and its related runtime behavior.
namespace MedyxHMS.Services.Filters
{
    /// <summary>
    /// Global action filter that enforces license-expiry access rules:
    /// - Admin: can only access Account/LicenseExpired, Account/RequestLicenseFile, Account/Logout
    /// - Other non-privileged roles: are signed out with a message
    /// - SuperAdmin and Patient: always allowed through
    /// </summary>
    public class LicenseExpiryFilter : IAsyncActionFilter
    {
        private readonly ILicenseService _licenseService;
        private readonly SignInManager<ApplicationUser> _signInManager;

        // Actions the Admin is allowed even with expired license
        private static readonly HashSet<string> _allowedForAdmin = new(StringComparer.OrdinalIgnoreCase)
        {
            "LicenseExpired", "RequestLicenseFile", "Logout", "Login", "AccessDenied"
        };

        // Roles that bypass the license check
        private static readonly HashSet<string> _exemptRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "SuperAdmin", "Patient"
        };

        public LicenseExpiryFilter(ILicenseService licenseService, SignInManager<ApplicationUser> signInManager)
        {
            _licenseService = licenseService;
            _signInManager = signInManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;

            // Only applies to authenticated users
            if (user.Identity?.IsAuthenticated != true)
            {
                await next();
                return;
            }

            // Exempt roles bypass
            foreach (var exemptRole in _exemptRoles)
            {
                if (user.IsInRole(exemptRole))
                {
                    await next();
                    return;
                }
            }

            // Check license state (cached by service, so this is cheap)
            LicenseSnapshot snapshot;
            try
            {
                snapshot = await _licenseService.GetCurrentSnapshotAsync();
            }
            catch
            {
                await next();
                return;
            }

            if (snapshot.State != LicenseState.Expired)
            {
                await next();
                return;
            }

            // License is expired â€” determine action by role
            if (user.IsInRole("Admin"))
            {
                var actionName = context.RouteData.Values["action"]?.ToString() ?? "";
                var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "";

                // Allow Account controller specific actions
                var isAccountController = string.Equals(controllerName, "Account", StringComparison.OrdinalIgnoreCase);
                if (isAccountController && _allowedForAdmin.Contains(actionName))
                {
                    await next();
                    return;
                }

                // Redirect Admin to LicenseExpired for all other requests
                context.Result = new RedirectToActionResult("LicenseExpired", "Account", null);
                return;
            }
            else
            {
                // Non-privileged role with expired license â€” sign them out
                await _signInManager.SignOutAsync();
                context.HttpContext.Session.Remove("ActiveRole");
                context.Result = new RedirectToActionResult("Login", "Account", new { expiredLicense = true });
                return;
            }
        }
    }
}
