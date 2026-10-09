using System.Globalization;

namespace MedyxHMS.ViewModels
{
    /// <summary>How a report column is shown and exported (alignment, number format, totals).</summary>
    public enum ReportColumnKind { Text, Integer, Number, Money, Percent, Date, DateTime }

    /// <summary>Which filter a report offers.</summary>
    public enum ReportFilterKind { None, DateRange, SingleDate, Month, ExpiryRange }

    public sealed class ReportColumn
    {
        public ReportColumn(string header, ReportColumnKind kind = ReportColumnKind.Text, float width = 1, bool total = false)
        {
            Header = header;
            Kind = kind;
            Width = width;
            Total = total;
        }

        public string Header { get; }
        public ReportColumnKind Kind { get; }

        /// <summary>Relative width in the PDF.</summary>
        public float Width { get; }

        /// <summary>Summed in the totals row.</summary>
        public bool Total { get; }

        public bool IsNumeric => Kind is ReportColumnKind.Integer or ReportColumnKind.Number or ReportColumnKind.Money or ReportColumnKind.Percent;
    }

    /// <summary>One table of a report.</summary>
    public sealed class ReportSection
    {
        public string? Title { get; set; }
        public List<ReportColumn> Columns { get; set; } = new();
        public List<object?[]> Rows { get; set; } = new();

        /// <summary>Show a totals row (columns marked Total are summed; the first column says "Total").</summary>
        public bool ShowTotals { get; set; }
        public string EmptyText { get; set; } = "No records for the selected period.";

        /// <summary>Document-style table (invoice, discharge summary): no row numbers and no record count.</summary>
        public bool Compact { get; set; }

        public object? TotalFor(int column)
        {
            var col = Columns[column];
            if (!col.Total) return null;
            decimal sum = 0;
            foreach (var row in Rows)
            {
                if (column < row.Length && row[column] != null && decimal.TryParse(Convert.ToString(row[column], CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                    sum += v;
            }

            return sum;
        }
    }

    /// <summary>Key figure shown above the tables.</summary>
    public sealed class ReportMetric
    {
        public ReportMetric(string label, object? value, ReportColumnKind kind = ReportColumnKind.Integer, string tone = "primary")
        {
            Label = label;
            Value = value;
            Kind = kind;
            Tone = tone;
        }

        public string Label { get; }
        public object? Value { get; }
        public ReportColumnKind Kind { get; }

        /// <summary>Bootstrap colour name: primary, success, danger, warning, info, secondary.</summary>
        public string Tone { get; }
    }

    /// <summary>Simple chart: bars or a doughnut on screen, bars in the PDF, a data table in Excel.</summary>
    public sealed class ReportChartData
    {
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "bar";
        public List<string> Labels { get; set; } = new();
        public List<decimal> Values { get; set; } = new();
        public bool IsMoney { get; set; }
    }

    /// <summary>Filter values of a report request.</summary>
    public sealed class ReportParameters
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? Date { get; set; }
        public DateTime? Month { get; set; }
        public string? StaffId { get; set; }
    }

    /// <summary>
    /// A finished report: letterhead data, key figures, charts and tables. Rendered on screen (_ReportDocument),
    /// as PDF and as Excel from the same object, so all three always show the same figures.
    /// </summary>
    public sealed class ReportDocument
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public ReportFilterKind FilterKind { get; set; } = ReportFilterKind.DateRange;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? Date { get; set; }
        public DateTime? Month { get; set; }
        public string? StaffId { get; set; }

        /// <summary>Optional staff filter (Staff Report).</summary>
        public List<KeyValuePair<string, string>> StaffOptions { get; set; } = new();

        public string PeriodText { get; set; } = string.Empty;

        public string HospitalName { get; set; } = string.Empty;
        public string HospitalAddress { get; set; } = string.Empty;
        public string HospitalContact { get; set; } = string.Empty;
        public string GeneratedBy { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public string CurrencySymbol { get; set; } = ReceiptPrintSettings.DefaultCurrencySymbol;

        public List<ReportMetric> Metrics { get; set; } = new();
        public List<ReportChartData> Charts { get; set; } = new();
        public List<ReportSection> Sections { get; set; } = new();
        public List<string> Notes { get; set; } = new();

        /// <summary>Footer text instead of "Confidential – for internal use" (e.g. on documents given to patients).</summary>
        public string? FooterNote { get; set; }

        /// <summary>Wide tables are printed on landscape pages.</summary>
        public bool Landscape => Sections.Any(s => s.Columns.Count > 7);

        public int RowCount => Sections.Sum(s => s.Rows.Count);

        /// <summary>Text for screen and PDF ("PKR 1,250.00", "05-Oct-2026", "12.5%").</summary>
        public string Format(object? value, ReportColumnKind kind)
        {
            if (value == null) return string.Empty;
            var inv = CultureInfo.InvariantCulture;
            switch (kind)
            {
                case ReportColumnKind.Money:
                    var amount = Convert.ToDecimal(value, inv);
                    var number = Math.Abs(amount).ToString("N2", inv);
                    var symbol = CurrencySymbol ?? string.Empty;
                    var text = symbol.Length == 0 ? number : symbol.Any(char.IsLetter) ? symbol + " " + number : symbol + number;
                    return amount < 0 ? "-" + text : text;
                case ReportColumnKind.Integer:
                    return Convert.ToDecimal(value, inv).ToString("N0", inv);
                case ReportColumnKind.Number:
                    return Convert.ToDecimal(value, inv).ToString("#,##0.##", inv);
                case ReportColumnKind.Percent:
                    return Convert.ToDecimal(value, inv).ToString("0.0", inv) + "%";
                case ReportColumnKind.Date:
                    return value is DateTime d ? d.ToString("dd-MMM-yyyy", inv) : Convert.ToString(value, inv) ?? string.Empty;
                case ReportColumnKind.DateTime:
                    return value is DateTime dt ? dt.ToString("dd-MMM-yyyy hh:mm tt", inv) : Convert.ToString(value, inv) ?? string.Empty;
                default:
                    return Convert.ToString(value, inv) ?? string.Empty;
            }
        }

        /// <summary>File name: "Daily_Transaction_Report_2026-10-06".</summary>
        public string FileName()
        {
            var name = new string(Title.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            while (name.Contains("__")) name = name.Replace("__", "_");
            var stamp = (EndDate ?? Date ?? Month ?? GeneratedAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return name.Trim('_') + "_" + stamp;
        }
    }
}
