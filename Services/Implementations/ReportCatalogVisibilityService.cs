using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using System.Text.Json;

namespace MedyxHMS.Services.Implementations
{
    public class ReportCatalogVisibilityService : IReportCatalogVisibilityService
    {
        private const string InactiveKeysSetting = "SystemManagement.ReportCatalog.InactiveKeys";
        private const string RoleMapSetting = "SystemManagement.ReportCatalog.RoleMap";
        private readonly ISettingService _settingService;

        public ReportCatalogVisibilityService(ISettingService settingService)
        {
            _settingService = settingService;
        }

        public async Task<IReadOnlyList<ReportCatalogItem>> GetVisibleItemsForUserAsync(
            bool canSeeAdminOnly,
            bool isSuperAdmin,
            IEnumerable<string>? userRoles = null,
            bool includeInactiveForSuperAdmin = false)
        {
            var items = ReportCatalogRegistry.GetVisibleItems(canSeeAdminOnly);
            if (isSuperAdmin && includeInactiveForSuperAdmin)
            {
                return items;
            }

            var inactiveKeys = await GetInactiveKeysAsync();
            var roleMap = await GetReportRoleMapAsync();
            var roleSet = new HashSet<string>(
                userRoles ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            return items
                .Where(item => !inactiveKeys.Contains(item.Key))
                .Where(item =>
                {
                    if (!roleMap.TryGetValue(item.Key, out var allowedRoles) || allowedRoles.Count == 0)
                    {
                        return true;
                    }

                    if (isSuperAdmin)
                    {
                        return true;
                    }

                    return allowedRoles.Any(roleSet.Contains);
                })
                .ToList();
        }

        public async Task<HashSet<string>> GetInactiveKeysAsync()
        {
            var raw = await _settingService.GetSettingValueAsync(InactiveKeysSetting);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            var keys = raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrWhiteSpace(k));

            return new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<bool> SetReportActiveStateAsync(string reportKey, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(reportKey))
            {
                return false;
            }

            var exists = ReportCatalogRegistry.All.Any(r => r.Key.Equals(reportKey, StringComparison.OrdinalIgnoreCase));
            if (!exists)
            {
                return false;
            }

            var inactiveKeys = await GetInactiveKeysAsync();

            if (isActive)
            {
                inactiveKeys.Remove(reportKey);
            }
            else
            {
                inactiveKeys.Add(reportKey);
            }

            var persisted = string.Join(',', inactiveKeys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
            return await _settingService.UpdateSettingAsync(InactiveKeysSetting, persisted);
        }

        public async Task<Dictionary<string, List<string>>> GetReportRoleMapAsync()
        {
            var raw = await _settingService.GetSettingValueAsync(RoleMapSetting);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(raw)
                    ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                var normalized = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in parsed)
                {
                    var roles = (kvp.Value ?? new List<string>())
                        .Where(r => !string.IsNullOrWhiteSpace(r))
                        .Select(r => r.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    normalized[kvp.Key] = roles;
                }

                return normalized;
            }
            catch
            {
                return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public async Task<IReadOnlyList<string>> GetRolesForReportAsync(string reportKey)
        {
            if (string.IsNullOrWhiteSpace(reportKey))
            {
                return Array.Empty<string>();
            }

            var roleMap = await GetReportRoleMapAsync();
            if (!roleMap.TryGetValue(reportKey, out var roles))
            {
                return Array.Empty<string>();
            }

            return roles;
        }

        public async Task<bool> SetReportRolesAsync(string reportKey, IEnumerable<string> roles)
        {
            if (string.IsNullOrWhiteSpace(reportKey))
            {
                return false;
            }

            var exists = ReportCatalogRegistry.All.Any(r => r.Key.Equals(reportKey, StringComparison.OrdinalIgnoreCase));
            if (!exists)
            {
                return false;
            }

            var normalizedRoles = (roles ?? Enumerable.Empty<string>())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var roleMap = await GetReportRoleMapAsync();
            if (normalizedRoles.Count == 0)
            {
                roleMap.Remove(reportKey);
            }
            else
            {
                roleMap[reportKey] = normalizedRoles;
            }

            var persisted = JsonSerializer.Serialize(roleMap);
            return await _settingService.UpdateSettingAsync(RoleMapSetting, persisted);
        }
    }
}
