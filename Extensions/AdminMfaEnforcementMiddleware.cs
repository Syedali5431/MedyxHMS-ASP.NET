using System.Security.Claims;
using MedyxHMS.Services.Interfaces;

namespace MedyxHMS.Extensions
{
    /// <summary>
    /// Security policy "Require two-step login for Admin and SuperAdmin": a signed-in admin who has not set up
    /// two-step login can only open the set-up page (and sign out) until it is done. Can be switched off in
    /// Security &amp; Backups, or in an emergency with the configuration value Security:AdminMfaEnforcementDisabled = true.
    /// </summary>
    public class AdminMfaEnforcementMiddleware
    {
        private static readonly string[] AllowedPrefixes =
        {
            "/Account/EnableMFA", "/Account/CompleteMFASetup", "/Account/Logout", "/Account/Login", "/Account/AccessDenied",
            "/Account/VerifyMFA", "/Home/HttpStatus", "/Home/Error", "/SystemManagement/ThemeStylesheet",
            "/css", "/js", "/lib", "/images", "/img", "/fonts", "/favicon"
        };

        private readonly RequestDelegate _next;
        private readonly bool _disabledByConfig;

        public AdminMfaEnforcementMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _disabledByConfig = configuration.GetValue<bool>("Security:AdminMfaEnforcementDisabled");
        }

        public async Task InvokeAsync(HttpContext context, ISecurityPolicyService policyService)
        {
            var user = context.User;
            if (_disabledByConfig
                || user?.Identity?.IsAuthenticated != true
                || !(user.IsInRole("SuperAdmin") || user.IsInRole("Admin"))
                || IsAllowed(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var policy = await policyService.GetPolicyAsync();
            // Test accounts listed in Security & Backups ("Test accounts without two-step login") are not asked.
            if (!policy.RequireMfaForAdmins || string.IsNullOrEmpty(userId) || (policy.IsMfaExempt(user.Identity?.Name) && !user.IsInRole("SuperAdmin"))
                || await policyService.IsMfaEnabledAsync(userId))
            {
                await _next(context);
                return;
            }

            var accept = context.Request.Headers.Accept.ToString();
            var isAjax = context.Request.Headers.XRequestedWith == "XMLHttpRequest";
            if (context.Request.Path.StartsWithSegments("/api") || isAjax || (!accept.Contains("text/html") && accept.Contains("json")))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Two-step login must be set up before using the system." });
                return;
            }

            context.Response.Redirect("/Account/EnableMFA?required=true");
        }

        private static bool IsAllowed(PathString path) =>
            AllowedPrefixes.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));

        public static bool IsDisabledByConfig(IConfiguration configuration) =>
            configuration.GetValue<bool>("Security:AdminMfaEnforcementDisabled");
    }
}
