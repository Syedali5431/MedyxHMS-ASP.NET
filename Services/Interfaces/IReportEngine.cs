using MedyxHMS.ViewModels;

namespace MedyxHMS.Services.Interfaces
{
    public sealed record ReportDefinition(string Key, string Title, string Category, string Description, ReportFilterKind Filter);

    /// <summary>
    /// Builds the hospital's reports (Reports workspace R1–R44) from live data. The same document is shown on
    /// screen and exported as PDF or Excel.
    /// </summary>
    public interface IReportEngine
    {
        IReadOnlyList<ReportDefinition> Definitions { get; }

        ReportDefinition? GetDefinition(string? key);

        /// <summary>Null when the key is not a data report (tool pages such as the report builder).</summary>
        Task<ReportDocument?> BuildAsync(string? key, ReportParameters parameters);
    }
}
