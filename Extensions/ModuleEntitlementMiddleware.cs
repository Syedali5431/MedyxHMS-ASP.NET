using System.Security.Claims;
using MedyxHMS.Services.Interfaces;

// Purpose: Contains application code for ModuleEntitlementMiddleware and its related runtime behavior.
namespace MedyxHMS.Extensions
{
    // Maps route prefixes to module keys and enforces module-level access.
    public class ModuleEntitlementMiddleware
    {
        private static readonly (string Prefix, string ModuleKey)[] PathToModuleMap =
        {
            ("/Dashboard", "Dashboard"),
            ("/Patient", "Patient"),
            ("/Appointment", "Appointment"),
            ("/OPD", "OPD"),
            ("/IPD", "IPD"),
            ("/Billing", "Billing"),
            ("/Prescription", "Prescription"),
            ("/Pharmacy", "Prescription"),
            ("/Lab", "Lab"),
            ("/Radiology", "Radiology"),
            ("/BloodBank", "BloodBank"),
            ("/OperationTheatre", "OperationTheatre"),
            ("/FrontOffice", "FrontOffice"),
            ("/Attendance", "Attendance"),
            ("/Leave", "Leave"),
            ("/Payroll", "Payroll"),
            ("/Certificate", "Certificate"),
            ("/Referral", "Referral"),
            ("/Report", "Report"),
            ("/PatientPortal", "PatientPortal"),
            ("/Ambulance", "Ambulance"),
            ("/Chatbot", "Chatbot"),
            ("/ChatbotAdmin", "Chatbot"),
            ("/Cms", "CMS"),
            ("/License", "License")
        };

        private readonly RequestDelegate _next;

        public ModuleEntitlementMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IModuleService moduleService, ILicenseService licenseService)
        {
            if (context.User.Identity?.IsAuthenticated != true || context.User.IsInRole("SuperAdmin"))
            {
                // SuperAdmin bypasses module gating by design.
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value ?? string.Empty;
            if (IsExemptPath(path) || !TryResolveModuleKey(path, out var moduleKey))
            {
                await _next(context);
                return;
            }

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                await _next(context);
                return;
            }

            var moduleEnabledForUser = await moduleService.IsModuleEnabledForUserAsync(moduleKey, userId, isSuperAdmin: false);
            if (!moduleEnabledForUser)
            {
                // Admin toggle disabled this module for the current user.
                context.Response.Redirect($"/License/FeatureLocked?moduleKey={Uri.EscapeDataString(moduleKey)}&reason=admin");
                return;
            }

            var licensed = await licenseService.IsModuleLicensedForCurrentLicenseAsync(moduleKey);
            if (!licensed)
            {
                // Module exists, but current license does not include it.
                context.Response.Redirect($"/License/FeatureLocked?moduleKey={Uri.EscapeDataString(moduleKey)}&reason=license");
                return;
            }

            await _next(context);
        }

        private static bool TryResolveModuleKey(string path, out string moduleKey)
        {
            foreach (var (prefix, key) in PathToModuleMap)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    moduleKey = key;
                    return true;
                }
            }

            moduleKey = string.Empty;
            return false;
        }

        private static bool IsExemptPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            return path.StartsWith("/License/FeatureLocked", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/License/Expired", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/Account/Logout", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/Home/Error", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/images", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase);
        }
    }
}
