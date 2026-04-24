namespace MedyxHMS.ViewModels
{
    public sealed class SystemManagementReportRowViewModel
    {
        public int SerialNo { get; init; }
        public string Key { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Purpose { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }

    public sealed class SystemManagementReportListViewModel
    {
        public bool IsSuperAdmin { get; init; }
        public string SearchTerm { get; init; } = string.Empty;
        public int TotalReports { get; init; }
        public int ActiveReports { get; init; }
        public IReadOnlyList<SystemManagementReportRowViewModel> Rows { get; init; } = Array.Empty<SystemManagementReportRowViewModel>();
    }
}
