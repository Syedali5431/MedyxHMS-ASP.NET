using System.Text;
using MedyxHMS.Services.Implementations;
using Xunit;

namespace MedyxHMS.Tests.Services;

public class ExportServiceTests
{
    [Fact]
    public void BuildCsv_ShouldIncludeTitleHeadersAndRows()
    {
        var service = new ExportService();
        var headers = new[] { "Name", "Status" };
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "John", "Active" },
            new[] { "Jane", "Inactive" }
        };

        var bytes = service.BuildCsv("Patient Export", headers, rows);
        var csv = Encoding.UTF8.GetString(bytes);

        Assert.Contains("Patient Export", csv);
        Assert.Contains("Name,Status", csv);
        Assert.Contains("John,Active", csv);
        Assert.Contains("Jane,Inactive", csv);
    }

    [Fact]
    public void BuildCsv_ShouldEscapeCommasQuotesAndNewLines()
    {
        var service = new ExportService();
        var headers = new[] { "Value" };
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "value,with,comma" },
            new[] { "value \"with quote\"" },
            new[] { "line1\nline2" }
        };

        var bytes = service.BuildCsv("Escaping Test", headers, rows);
        var csv = Encoding.UTF8.GetString(bytes);

        Assert.Contains("\"value,with,comma\"", csv);
        Assert.Contains("\"value \"\"with quote\"\"\"", csv);
        Assert.Contains("\"line1\nline2\"", csv);
    }

    [Fact]
    public void BuildPdfTable_ShouldReturnNonEmptyPdfBytes()
    {
        var service = new ExportService();
        var headers = new[] { "Col1", "Col2" };
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "A", "B" }
        };

        var bytes = service.BuildPdfTable("PDF Export", headers, rows);

        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
