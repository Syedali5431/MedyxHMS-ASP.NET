using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;

namespace MedyxHMS.Services.Implementations
{
    public class ReportCatalogVisibilityService : IReportCatalogVisibilityService
    {
        private const string InactiveKeysSetting = "SystemManagement.ReportCatalog.InactiveKeys";
        private readonly ISettingService _settingService;

        public ReportCatalogVisibilityService(ISettingService settingService)
        {
            _settingService = settingService;
        }

        public async Task<IReadOnlyList<ReportCatalogItem>> GetVisibleItemsForUserAsync(bool canSeeAdminOnly, bool isSuperAdmin)
        {
            var items = ReportCatalogRegistry.GetVisibleItems(canSeeAdminOnly);
            if (isSuperAdmin)
            {
                return items;
            }

            var inactiveKeys = await GetInactiveKeysAsync();
            return items
                .Where(item => !inactiveKeys.Contains(item.Key))
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
    }
}
