using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using MedyxHMS.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// Purpose: Contains application code for ExportService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class ExportService : IExportService
    {
        static ExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] BuildCsv(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine(EscapeCsv(title));
            sb.AppendLine(EscapeCsv("Generated At (UTC): " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)));
            sb.AppendLine();

            sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
            foreach (var row in rows)
                sb.AppendLine(string.Join(",", row.Select(v => EscapeCsv(v ?? string.Empty))));

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public byte[] BuildPdfTable(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4.Landscape());

                    page.Header().Column(column =>
                    {
                        column.Item().Text(title).Bold().FontSize(16);
                        column.Item().Text("Generated At (UTC): " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)).FontSize(9).FontColor(Colors.Grey.Darken2);
                    });

                    page.Content().PaddingVertical(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            for (var i = 0; i < headers.Count; i++)
                                columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            foreach (var h in headers)
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(h).Bold().FontSize(9);
                            }
                        });

                        foreach (var row in rows)
                        {
                            foreach (var cell in row)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(cell ?? string.Empty).FontSize(8);
                            }
                        }
                    });

                    page.Footer().AlignRight().Text(text =>
                    {
                        text.Span("Page ").FontSize(8);
                        text.CurrentPageNumber().FontSize(8);
                        text.Span(" of ").FontSize(8);
                        text.TotalPages().FontSize(8);
                    });
                });
            }).GeneratePdf();
        }

        public byte[] BuildExcel(string sheetName, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(sheetName?.Length > 31 ? sheetName.Substring(0, 31) : (sheetName ?? "Sheet1"));

            // Header row
            for (var col = 0; col < headers.Count; col++)
            {
                var cell = ws.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
                cell.Style.Font.FontColor = XLColor.White;
            }

            // Data rows
            for (var row = 0; row < rows.Count; row++)
            {
                var dataRow = rows[row];
                for (var col = 0; col < dataRow.Count; col++)
                    ws.Cell(row + 2, col + 1).Value = dataRow[col] ?? string.Empty;

                if (row % 2 == 1)
                    ws.Row(row + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            }

            ws.Columns().AdjustToContents();
            ws.Row(1).Height = 18;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static string EscapeCsv(string value)
        {
            if (value == null)
                return string.Empty;

            var escaped = value.Replace("\"", "\"\"");
            if (escaped.Contains(',') || escaped.Contains('"') || escaped.Contains('\n') || escaped.Contains('\r'))
                return "\"" + escaped + "\"";

            return escaped;
        }
    }
}
