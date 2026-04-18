using MedyxHMS.Services.Interfaces;

namespace MedyxHMS.Extensions
{
    public class LicenseEnforcementMiddleware
    {
        private readonly RequestDelegate _next;

        public LicenseEnforcementMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ILicenseService licenseService)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (!await licenseService.ShouldRestrictAccessAsync(context.User, path))
            {
                await _next(context);
                return;
            }

            var returnUrl = $"{context.Request.Path}{context.Request.QueryString}";
            var redirectUrl = $"/License/Expired?returnUrl={Uri.EscapeDataString(returnUrl)}";
            context.Response.Redirect(redirectUrl);
        }
    }
}