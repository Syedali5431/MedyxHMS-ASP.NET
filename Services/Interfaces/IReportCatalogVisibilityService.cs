using MedyxHMS.ViewModels;

namespace MedyxHMS.Services.Interfaces
{
    public interface IReportCatalogVisibilityService
    {
        Task<IReadOnlyList<ReportCatalogItem>> GetVisibleItemsForUserAsync(bool canSeeAdminOnly, bool isSuperAdmin);
        Task<HashSet<string>> GetInactiveKeysAsync();
        Task<bool> SetReportActiveStateAsync(string reportKey, bool isActive);
    }
}
