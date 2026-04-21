using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using PermissionService = MedyxHMS.Services.Interfaces.IAuthorizationService;

namespace MedyxHMS.Extensions
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }

    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IServiceProvider _serviceProvider;

        public PermissionAuthorizationHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var authorizationService = scope.ServiceProvider.GetRequiredService<PermissionService>();

            if (await authorizationService.HasPermissionAsync(userId, requirement.Permission))
            {
                context.Succeed(requirement);
            }
        }
    }

    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
        {
            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
            return services;
        }

        public static AuthorizationPolicyBuilder RequirePermission(
            this AuthorizationPolicyBuilder builder,
            string permission)
        {
            builder.AddRequirements(new PermissionRequirement(permission));
            return builder;
        }
    }

    public class PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
            : base(options)
        {
        }

        public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            const string prefix = "Permission:";
            if (policyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var permission = policyName[prefix.Length..];
                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(permission))
                    .Build();
                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            return base.GetPolicyAsync(policyName);
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class PermissionAuthorizeAttribute : AuthorizeAttribute
    {
        public PermissionAuthorizeAttribute(string permission)
        {
            Policy = $"Permission:{permission}";
        }
    }

    public class PermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly string _permission;
        private readonly IServiceProvider _serviceProvider;

        public PermissionFilter(string permission, IServiceProvider serviceProvider)
        {
            _permission = permission;
            _serviceProvider = serviceProvider;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                context.Result = new Microsoft.AspNetCore.Mvc.UnauthorizedResult();
                return;
            }

            var userId = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new Microsoft.AspNetCore.Mvc.UnauthorizedResult();
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var authorizationService = scope.ServiceProvider.GetRequiredService<PermissionService>();

            if (!await authorizationService.HasPermissionAsync(userId, _permission))
            {
                context.Result = new Microsoft.AspNetCore.Mvc.ForbidResult();
            }
        }
    }

    public static class ClaimsPrincipalExtensions
    {
        private static readonly Dictionary<string, Dictionary<string, string[]>> PermissionMatrix =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["ManageUsers"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["View"] = new[] { "Admin", "SuperAdmin" },
                    ["Add"] = new[] { "Admin", "SuperAdmin" },
                    ["Edit"] = new[] { "Admin", "SuperAdmin" },
                    ["Delete"] = new[] { "Admin", "SuperAdmin" }
                },
                ["Appointment"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["View"] = new[] { "Admin", "SuperAdmin", "Doctor", "Nurse", "Staff", "Receptionist" },
                    ["Add"] = new[] { "Admin", "SuperAdmin", "Doctor", "Staff", "Receptionist" },
                    ["Edit"] = new[] { "Admin", "SuperAdmin", "Doctor", "Staff", "Receptionist" },
                    ["Delete"] = new[] { "Admin", "SuperAdmin" }
                },
                ["Patient"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["View"] = new[] { "Admin", "SuperAdmin", "Doctor", "Nurse", "Staff", "Accountant", "Receptionist", "Pharmacist", "LabTechnician", "Radiologist" },
                    ["Add"] = new[] { "Admin", "SuperAdmin", "Doctor", "Nurse", "Staff", "Accountant", "Receptionist", "Pharmacist", "LabTechnician", "Radiologist" },
                    ["Edit"] = new[] { "Admin", "SuperAdmin", "Doctor", "Nurse", "Staff", "Accountant", "Receptionist", "Pharmacist", "LabTechnician", "Radiologist" },
                    ["Delete"] = new[] { "Admin", "SuperAdmin" }
                }
            };

        public static bool HasPermission(this ClaimsPrincipal user, string module, string action)
        {
            if (user.Identity?.IsAuthenticated != true)
                return false;

            if (!PermissionMatrix.TryGetValue(module ?? string.Empty, out var actionMap))
                return user.IsInRole("SuperAdmin") || user.IsInRole("Admin");

            if (!actionMap.TryGetValue(action ?? string.Empty, out var allowedRoles))
                return false;

            return allowedRoles.Any(user.IsInRole);
        }
    }
}