using MedyxHMS.ViewModels;

namespace MedyxHMS.Services.Interfaces
{
    public interface IReportCatalogVisibilityService
    {
        Task<IReadOnlyList<ReportCatalogItem>> GetVisibleItemsForUserAsync(
            bool canSeeAdminOnly,
            bool isSuperAdmin,
            IEnumerable<string>? userRoles = null,
            bool includeInactiveForSuperAdmin = false);
        Task<HashSet<string>> GetInactiveKeysAsync();
        Task<bool> SetReportActiveStateAsync(string reportKey, bool isActive);
        Task<Dictionary<string, List<string>>> GetReportRoleMapAsync();
        Task<IReadOnlyList<string>> GetRolesForReportAsync(string reportKey);
        Task<bool> SetReportRolesAsync(string reportKey, IEnumerable<string> roles);
    }
}
