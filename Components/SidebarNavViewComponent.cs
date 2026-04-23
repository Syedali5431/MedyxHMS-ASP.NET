using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedyxHMS.Components
{
    /// <summary>
    /// Renders the staff portal left sidebar for all authenticated pages.
    /// Respects per-user module enablement and role-based item visibility.
    /// </summary>
    public class SidebarNavViewComponent : ViewComponent
    {
        private readonly IModuleService _moduleService;

        public SidebarNavViewComponent(IModuleService moduleService)
        {
            _moduleService = moduleService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return View(new SidebarNavViewModel());

            var roles = HttpContext.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            var isSuperAdmin = roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase);
            var moduleMap = await _moduleService.GetUserModuleMapAsync(userId, isSuperAdmin);

            var vm = new SidebarNavViewModel
            {
                Roles = roles,
                IsSuperAdmin = isSuperAdmin,
                IsAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase) || isSuperAdmin,
                ModuleMap = moduleMap,
                CurrentPath = HttpContext.Request.Path.ToString()
            };

            return View(vm);
        }
    }

    public class SidebarNavViewModel
    {
        public List<string> Roles { get; set; } = new();
        public bool IsSuperAdmin { get; set; }
        public bool IsAdmin { get; set; }
        public Dictionary<string, bool> ModuleMap { get; set; } = new();
        public string CurrentPath { get; set; } = string.Empty;

        /// <summary>Returns true if a module is enabled for the current user.</summary>
        public bool ModuleOn(string key) =>
            IsSuperAdmin || (ModuleMap.TryGetValue(key, out var v) && v);

        /// <summary>Returns true if user is Admin or SuperAdmin.</summary>
        public bool IsAdminOrSuper => IsAdmin || IsSuperAdmin;

        /// <summary>Returns true if the current request path starts with the given prefix (case-insensitive).</summary>
        public bool IsActive(string pathPrefix) =>
            CurrentPath.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase);

        /// <summary>Returns true if the current path matches any of the given prefixes.</summary>
        public bool IsActiveAny(params string[] prefixes) =>
            prefixes.Any(p => CurrentPath.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}
