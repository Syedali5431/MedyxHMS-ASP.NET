using MedyxHMS.Services.Interfaces;

// Purpose: Contains application code for LicenseEnforcementMiddleware and its related runtime behavior.
namespace MedyxHMS.Extensions
{
    // Enforces license-expiry access restrictions for authenticated requests.
    public class LicenseEnforcementMiddleware
    {
        private readonly RequestDelegate _next;

        public LicenseEnforcementMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ILicenseService licenseService, IConcurrentSessionService concurrentSessionService)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // Session activity is updated per request for concurrent-login governance.
                await concurrentSessionService.MarkActivityAsync(context.Session.Id);
            }

            var path = context.Request.Path.Value ?? string.Empty;
            if (!await licenseService.ShouldRestrictAccessAsync(context.User, path))
            {
                await _next(context);
                return;
            }

            var returnUrl = $"{context.Request.Path}{context.Request.QueryString}";
            // Preserve target URL so users can continue after license renewal.
            var redirectUrl = $"/License/Expired?returnUrl={Uri.EscapeDataString(returnUrl)}";
            context.Response.Redirect(redirectUrl);
        }
    }
}
