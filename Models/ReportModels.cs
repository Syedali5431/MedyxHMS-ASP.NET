// Purpose: Contains application code for ReportModels and its related runtime behavior.
namespace MedyxHMS.Models
{
    /// <summary>
    /// Represents a custom report template that users can create and edit.
    /// </summary>
    public class ReportTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty; // e.g., "Department", "Financial", "Occupancy"
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;

        // Configuration
        public List<ReportField> Fields { get; set; } = new();
        public List<ReportFilter> Filters { get; set; } = new();
        public ReportDesign Design { get; set; } = new();
        public List<ReportChart>? Charts { get; set; } = new();
    }

    /// <summary>
    /// Represents a field in a custom report.
    /// </summary>
    public class ReportField
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = "string"; // string, int, decimal, datetime
        public string DisplayFormat { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
        public bool IsSortable { get; set; } = true;
        public bool IsFilterable { get; set; } = true;
        public int SortOrder { get; set; }
        public int? Width { get; set; } // In pixels
        public string? Alignment { get; set; } = "left"; // left, center, right

        public ReportTemplate? Template { get; set; }
    }

    /// <summary>
    /// Represents a filter option for a custom report.
    /// </summary>
    public class ReportFilter
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string FilterName { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public string OperatorType { get; set; } = "equals"; // equals, contains, greater, less, between, etc.
        public string? DefaultValue { get; set; }
        public bool IsRequired { get; set; } = false;
        public int SortOrder { get; set; }

        public ReportTemplate? Template { get; set; }
    }

    /// <summary>
    /// Represents the design/styling configuration of a report.
    /// </summary>
    public class ReportDesign
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string? HeaderText { get; set; }
        public string? FooterText { get; set; }
        public string? ColorScheme { get; set; } = "default"; // default, professional, colorful
        public bool ShowGridLines { get; set; } = true;
        public bool ShowAlternatingRows { get; set; } = true;
        public bool ShowTotals { get; set; } = false;
        public bool ShowGrouping { get; set; } = false;
        public string? PageOrientation { get; set; } = "portrait"; // portrait, landscape
        public bool IncludeTimestamp { get; set; } = true;
        public string? CompanyLogo { get; set; }
        public string? CustomCss { get; set; }

        public ReportTemplate? Template { get; set; }
    }

    /// <summary>
    /// Represents a chart to be included in a report.
    /// </summary>
    public class ReportChart
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ChartType { get; set; } = "bar"; // bar, pie, line, area, doughnut
        public string XAxisField { get; set; } = string.Empty;
        public string YAxisField { get; set; } = string.Empty;
        public bool ShowLegend { get; set; } = true;
        public bool ShowTooltip { get; set; } = true;
        public string? ColorScheme { get; set; }
        public int SortOrder { get; set; }

        public ReportTemplate? Template { get; set; }
    }

    /// <summary>
    /// Represents a saved report execution with results.
    /// </summary>
    public class SavedReport
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? Description { get; set; }
        public string? FilterParams { get; set; } // JSON serialized filters used
        public string? ReportData { get; set; } // JSON serialized report data
        public int RecordCount { get; set; }
        public decimal? ExecutionTimeMs { get; set; }

        public ReportTemplate? Template { get; set; }
    }

    /// <summary>
    /// Data transfer object for report execution parameters.
    /// </summary>
    public class ReportExecutionDto
    {
        public int TemplateId { get; set; }
        public Dictionary<string, string> Filters { get; set; } = new();
        public string? SortField { get; set; }
        public bool SortDescending { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public bool ExportAsExcel { get; set; } = false;
        public bool ExportAsPdf { get; set; } = false;
    }

    /// <summary>
    /// Result of a report execution.
    /// </summary>
    public class ReportExecutionResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<Dictionary<string, object>> Data { get; set; } = new();
        public List<ReportField> Fields { get; set; } = new();
        public List<ReportChart>? Charts { get; set; }
        public int TotalRecords { get; set; }
        public long ExecutionTimeMs { get; set; }
    }
}
