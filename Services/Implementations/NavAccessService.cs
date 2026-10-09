using System.Security.Claims;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;

// Purpose: Decides whether a navigation link should be shown, using the same authorization rules
// ([Authorize] roles/policies and in-action permission checks) that apply when the link is opened.
namespace MedyxHMS.Services.Implementations
{
    public class NavAccessService : INavAccessService
    {
        private readonly IActionDescriptorCollectionProvider _actions;
        private readonly IAuthorizationPolicyProvider _policyProvider;
        private readonly Microsoft.AspNetCore.Authorization.IAuthorizationService _authorization;
        private readonly MedyxHMS.Services.Interfaces.IAuthorizationService _permissions;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Dictionary<string, bool> _cache = new(StringComparer.OrdinalIgnoreCase);

        // Actions that check a permission inside the action body instead of with an attribute
        // (mirrors PatientController / AppointmentController.HasPermissionAsync).
        private static readonly Dictionary<string, string> InActionPermissions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Patient/Index"] = "Patient.View",
            ["Patient/Export"] = "Patient.View",
            ["Patient/Details"] = "Patient.View",
            ["Patient/Create"] = "Patient.Add",
            ["Patient/Edit"] = "Patient.Edit",
            ["Patient/Delete"] = "Patient.Delete",
            ["Appointment/Index"] = "ViewAppointments",
            ["Appointment/Export"] = "ViewAppointments",
            ["Appointment/Calendar"] = "ViewAppointments",
            ["Appointment/Dashboard"] = "ViewAppointments",
            ["Appointment/Details"] = "ViewAppointments",
            ["Appointment/ThermalSlip"] = "ViewAppointments",
            ["Appointment/Create"] = "AddAppointments",
            ["Appointment/Edit"] = "EditAppointments",
            ["Appointment/UpdateStatus"] = "EditAppointments",
            ["Appointment/Delete"] = "DeleteAppointments",
        };

        public NavAccessService(
            IActionDescriptorCollectionProvider actions,
            IAuthorizationPolicyProvider policyProvider,
            Microsoft.AspNetCore.Authorization.IAuthorizationService authorization,
            MedyxHMS.Services.Interfaces.IAuthorizationService permissions,
            IHttpContextAccessor httpContextAccessor)
        {
            _actions = actions;
            _policyProvider = policyProvider;
            _authorization = authorization;
            _permissions = permissions;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> CanAccessAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true;

            var path = url.Split('?', '#')[0].Trim('/');
            if (_cache.TryGetValue(path, out var cached))
                return cached;

            var allowed = await EvaluateAsync(path);
            _cache[path] = allowed;
            return allowed;
        }

        public async Task<bool> CanAccessAnyAsync(params string[] urls)
        {
            foreach (var url in urls)
            {
                if (await CanAccessAsync(url))
                    return true;
            }
            return false;
        }

        private async Task<bool> EvaluateAsync(string path)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
                return false;

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string? area = null;
            if (segments.Length > 0 && segments[0].Equals("PatientPortal", StringComparison.OrdinalIgnoreCase))
            {
                area = "PatientPortal";
                segments = segments.Skip(1).ToArray();
            }

            var controller = segments.Length > 0 ? segments[0] : "Home";
            var action = segments.Length > 1 ? segments[1] : "Index";

            var descriptor = FindGetAction(area, controller, action);
            if (descriptor == null)
                return true; // not an MVC action this service can evaluate - leave the link as it is

            var metadata = descriptor.EndpointMetadata;
            if (!metadata.OfType<IAllowAnonymous>().Any())
            {
                var authorizeData = metadata.OfType<IAuthorizeData>().ToList();
                if (authorizeData.Count > 0)
                {
                    var policy = await AuthorizationPolicy.CombineAsync(_policyProvider, authorizeData);
                    if (policy != null && !(await _authorization.AuthorizeAsync(user, policy)).Succeeded)
                        return false;
                }
            }

            if (InActionPermissions.TryGetValue($"{descriptor.ControllerName}/{descriptor.ActionName}", out var permission))
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !await _permissions.HasPermissionAsync(userId, permission))
                    return false;
            }

            return true;
        }

        private ControllerActionDescriptor? FindGetAction(string? area, string controller, string action)
        {
            var candidates = _actions.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Where(a => a.ControllerName.Equals(controller, StringComparison.OrdinalIgnoreCase)
                         && a.ActionName.Equals(action, StringComparison.OrdinalIgnoreCase)
                         && string.Equals(AreaOf(a), area, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Prefer the GET action (what a link opens); fall back to a POST-only action such as a
            // Delete button, so buttons that post to it follow the same rule.
            return candidates.FirstOrDefault(AllowsGet) ?? candidates.FirstOrDefault();
        }

        private static string? AreaOf(ControllerActionDescriptor action)
        {
            return action.RouteValues.TryGetValue("area", out var value) && !string.IsNullOrEmpty(value) ? value : null;
        }

        private static bool AllowsGet(ControllerActionDescriptor action)
        {
            var methods = action.ActionConstraints?
                .OfType<HttpMethodActionConstraint>()
                .SelectMany(c => c.HttpMethods)
                .ToList();
            return methods == null || methods.Count == 0 || methods.Contains("GET", StringComparer.OrdinalIgnoreCase);
        }
    }
}
