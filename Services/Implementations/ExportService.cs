using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using MedyxHMS.Data;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// Purpose: PDF, Excel and CSV exports with the hospital letterhead (reports and module lists).
namespace MedyxHMS.Services.Implementations
{
    public class ExportService : IExportService
    {
        // House colours: navy letterhead, light blue-grey shading.
        private const string Primary = "#1F3864";
        private const string Accent = "#2E75B6";
        private const string HeaderText = "#FFFFFF";
        private const string Zebra = "#F5F8FC";
        private const string RuleColor = "#D9E1EC";
        private const string TotalsFill = "#E8EEF7";
        private const string Muted = "#5F6B7A";

        private readonly ApplicationDbContext? _context;
        private readonly IHospitalContext? _hospitalContext;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        static ExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public ExportService(ApplicationDbContext? context = null, IHospitalContext? hospitalContext = null, IHttpContextAccessor? httpContextAccessor = null)
        {
            _context = context;
            _hospitalContext = hospitalContext;
            _httpContextAccessor = httpContextAccessor;
        }

        // ── Simple lists (module exports) ────────────────────────

        public byte[] BuildCsv(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine(EscapeCsv(title));
            sb.AppendLine(EscapeCsv("Generated: " + DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt", CultureInfo.InvariantCulture)));
            sb.AppendLine();

            sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
            foreach (var row in rows)
                sb.AppendLine(string.Join(",", row.Select(v => EscapeCsv(v ?? string.Empty))));

            // UTF-8 with BOM so Excel shows names and currency symbols correctly.
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        public byte[] BuildPdfTable(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
            => BuildReportPdf(DocumentFromTable(title, headers, rows));

        public byte[] BuildExcel(string sheetName, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
            => BuildReportExcel(DocumentFromTable(sheetName, headers, rows));

        /// <summary>A plain list as a report: numeric columns (all values numbers) are right-aligned and summed in Excel.</summary>
        public ReportDocument DocumentFromTable(string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
        {
            var columns = headers.Select((h, i) =>
            {
                var values = rows.Select(r => i < r.Count ? r[i] : null).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
                var numeric = values.Count > 0 && values.All(v => decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                              && !h.Contains("ID", StringComparison.OrdinalIgnoreCase) && !h.Contains("No", StringComparison.Ordinal) && !h.Contains("Phone", StringComparison.OrdinalIgnoreCase);
                return new ReportColumn(h, numeric ? ReportColumnKind.Number : ReportColumnKind.Text);
            }).ToList();

            var doc = new ReportDocument { Title = title, FilterKind = ReportFilterKind.None };
            doc.Sections.Add(new ReportSection
            {
                Columns = columns,
                Rows = rows.Select(r => headers.Select((_, i) =>
                {
                    var v = i < r.Count ? r[i] : null;
                    return columns[i].Kind == ReportColumnKind.Number && decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? (object?)d : v;
                }).ToArray()).ToList(),
                EmptyText = "No records."
            });
            doc.Metrics.Add(new ReportMetric("Records", rows.Count));
            FillLetterhead(doc);
            return doc;
        }

        /// <summary>Hospital name/address of the active hospital (or the default one) and the signed-in user.</summary>
        public void FillLetterhead(ReportDocument doc) => FillLetterhead(doc, null);

        /// <summary>Letterhead of the given hospital (e.g. the hospital of a bill), or of the active/default one.</summary>
        public void FillLetterhead(ReportDocument doc, int? forHospitalId)
        {
            doc.GeneratedAt = DateTime.Now;
            var user = _httpContextAccessor?.HttpContext?.User;
            var name = user?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(doc.GeneratedBy)) doc.GeneratedBy = name ?? string.Empty;
            doc.CurrencySymbol = CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol is { Length: > 0 and <= 5 } symbol && symbol != "¤"
                ? symbol
                : ReceiptPrintSettings.DefaultCurrencySymbol;

            if (_context == null) return;
            var hospitalId = forHospitalId ?? _hospitalContext?.ActiveHospitalId;
            var hospital = hospitalId.HasValue
                ? _context.Hospitals.AsNoTracking().FirstOrDefault(h => h.Id == hospitalId.Value)
                : _context.Hospitals.AsNoTracking().OrderByDescending(h => h.IsDefault).ThenBy(h => h.Id).FirstOrDefault();
            if (hospital == null) return;

            var viewingAll = !forHospitalId.HasValue && _hospitalContext?.IsStaffContext == true && !_hospitalContext.FilterEnabled && (_hospitalContext.AllHospitals.Count > 1);
            doc.HospitalName = viewingAll ? hospital.Name + " – all hospitals" : hospital.Name;
            doc.HospitalAddress = string.Join(", ", new[] { hospital.Address, hospital.City }.Where(s => !string.IsNullOrWhiteSpace(s)));
            doc.HospitalContact = string.Join("  ·  ", new[]
            {
                string.IsNullOrWhiteSpace(hospital.Phone) ? null : "Tel: " + hospital.Phone,
                string.IsNullOrWhiteSpace(hospital.Email) ? null : hospital.Email,
                string.IsNullOrWhiteSpace(hospital.LicenseNumber) ? null : "Reg: " + hospital.LicenseNumber
            }.Where(s => s != null));
        }

        // ── Invoice ──────────────────────────────────────────────

        public ReportDocument InvoiceDocument(MedyxHMS.Models.Bill bill)
        {
            var inv = CultureInfo.InvariantCulture;
            var patient = bill.Patient;
            var patientName = patient == null ? "–" : (patient.FirstName + " " + patient.LastName).Trim();
            var balance = bill.TotalAmount - bill.PaidAmount;
            var doc = new ReportDocument
            {
                Key = "INV",
                Title = "Invoice",
                Category = "Billing",
                FilterKind = ReportFilterKind.None,
                PeriodText = "No. " + bill.BillNumber + "   ·   " + bill.BillDate.ToString("dd-MMM-yyyy", inv)
            };
            FillLetterhead(doc, bill.HospitalId);
            doc.FooterNote = "Invoice " + bill.BillNumber;

            doc.Metrics.Add(new ReportMetric("Total", bill.TotalAmount, ReportColumnKind.Money));
            doc.Metrics.Add(new ReportMetric("Paid", bill.PaidAmount, ReportColumnKind.Money, "success"));
            doc.Metrics.Add(new ReportMetric("Balance due", balance, ReportColumnKind.Money, balance > 0 ? "danger" : "success"));
            doc.Metrics.Add(new ReportMetric("Status", string.IsNullOrWhiteSpace(bill.Status) ? "–" : bill.Status, ReportColumnKind.Text,
                bill.Status == "Paid" ? "success" : balance > 0 && bill.DueDate.Date < DateTime.Today ? "danger" : "warning"));

            doc.Sections.Add(new ReportSection
            {
                Title = "Bill to",
                Compact = true,
                Columns = new List<ReportColumn> { new("Patient", ReportColumnKind.Text, 2), new("Patient ID"), new("Phone"), new("Bill type"), new("Due date") },
                Rows = new List<object?[]> { new object?[] { patientName, patient?.PatientId, patient?.Phone, bill.BillType, bill.DueDate.ToString("dd-MMM-yyyy", inv) } }
            });
            doc.Sections.Add(new ReportSection
            {
                Title = "Items",
                Compact = true,
                Columns = new List<ReportColumn>
                {
                    new("Description", ReportColumnKind.Text, 3), new("Type"), new("Qty", ReportColumnKind.Number, 0.6f),
                    new("Unit price", ReportColumnKind.Money), new("Amount", ReportColumnKind.Money, 1, total: true)
                },
                Rows = bill.BillItems.OrderBy(i => i.Id).Select(i => new object?[]
                {
                    string.IsNullOrWhiteSpace(i.Description) || i.Description == i.ItemName ? i.ItemName : i.ItemName + " – " + i.Description,
                    i.ItemType, i.Quantity, i.UnitPrice, i.Amount
                }).ToList(),
                ShowTotals = bill.BillItems.Count > 0,
                EmptyText = "No items."
            });
            doc.Sections.Add(new ReportSection
            {
                Title = "Payments",
                Compact = true,
                Columns = new List<ReportColumn>
                {
                    new("Date", ReportColumnKind.DateTime), new("Method"), new("Reference", ReportColumnKind.Text, 1.4f), new("Status"), new("Amount", ReportColumnKind.Money, 1, total: true)
                },
                Rows = bill.Payments.OrderBy(p => p.PaymentDate).Select(p => new object?[]
                {
                    p.PaymentDate, p.PaymentMethod, string.IsNullOrWhiteSpace(p.TransactionId) ? "–" : p.TransactionId, p.Status, p.Amount
                }).ToList(),
                ShowTotals = bill.Payments.Count > 0,
                EmptyText = "No payments yet."
            });
            // Notes typed by staff are printed; the system's own link notes ("… for OPD Visit ID: 12") are not.
            if (!string.IsNullOrWhiteSpace(bill.Notes)
                && !bill.Notes.StartsWith("OPD consultation bill for OPD Visit ID", StringComparison.OrdinalIgnoreCase)
                && !bill.Notes.StartsWith("IPD daily charges bill for IPD Admission ID", StringComparison.OrdinalIgnoreCase))
            {
                doc.Notes.Add(bill.Notes);
            }
            doc.Notes.Add(balance > 0
                ? "Please quote the invoice number " + bill.BillNumber + " when paying. Payments can be made at the billing counter or in the Patient Portal."
                : "This invoice is paid in full. Thank you.");
            return doc;
        }

        // ── PDF ──────────────────────────────────────────────────

        public byte[] BuildReportPdf(ReportDocument doc)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(doc.Landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                    page.MarginHorizontal(28);
                    page.MarginVertical(24);
                    page.DefaultTextStyle(t => t.FontSize(8.5f).FontColor("#1D2733"));

                    page.Header().Element(h => ComposeHeader(h, doc));
                    page.Content().PaddingTop(10).Element(c => ComposeContent(c, doc));
                    page.Footer().Element(f => ComposeFooter(f, doc));
                });
            }).GeneratePdf();
        }

        private static void ComposeHeader(IContainer container, ReportDocument doc)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    if (Branding.LogoPng is { } logo)
                    {
                        row.ConstantItem(52).PaddingRight(10).AlignMiddle().Image(logo);
                    }
                    row.RelativeItem(3).Column(left =>
                    {
                        left.Item().Text(string.IsNullOrWhiteSpace(doc.HospitalName) ? "MedyxHMS" : doc.HospitalName).FontSize(15).Bold().FontColor(Primary);
                        if (!string.IsNullOrWhiteSpace(doc.HospitalAddress)) left.Item().Text(doc.HospitalAddress).FontSize(8).FontColor(Muted);
                        if (!string.IsNullOrWhiteSpace(doc.HospitalContact)) left.Item().Text(doc.HospitalContact).FontSize(8).FontColor(Muted);
                    });
                    row.RelativeItem(2).AlignRight().Column(right =>
                    {
                        right.Item().AlignRight().Text(doc.Title).FontSize(13).Bold().FontColor(Primary);
                        if (!string.IsNullOrWhiteSpace(doc.PeriodText)) right.Item().AlignRight().Text(doc.PeriodText).FontSize(9).SemiBold();
                        right.Item().AlignRight().Text(t =>
                        {
                            t.Span("Generated " + doc.GeneratedAt.ToString("dd-MMM-yyyy hh:mm tt", CultureInfo.InvariantCulture)).FontSize(7.5f).FontColor(Muted);
                            if (!string.IsNullOrWhiteSpace(doc.GeneratedBy)) t.Span(" by " + doc.GeneratedBy).FontSize(7.5f).FontColor(Muted);
                        });
                    });
                });
                col.Item().PaddingTop(6).LineHorizontal(2).LineColor(Primary);
                col.Item().PaddingTop(1).LineHorizontal(0.6f).LineColor(Accent);
            });
        }

        private static void ComposeContent(IContainer container, ReportDocument doc)
        {
            container.Column(col =>
            {
                col.Spacing(10);

                if (!string.IsNullOrWhiteSpace(doc.Description))
                {
                    col.Item().Text(doc.Description).FontSize(8).Italic().FontColor(Muted);
                }

                if (doc.Metrics.Count > 0)
                {
                    // Up to five key figures on one row (six on landscape pages), otherwise rows of four.
                    var perRow = doc.Metrics.Count <= (doc.Landscape ? 6 : 5) ? Math.Max(1, doc.Metrics.Count) : 4;
                    var valueSize = perRow >= 5 ? 10.5f : 12f;
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { for (var i = 0; i < perRow; i++) c.RelativeColumn(); });
                        foreach (var m in doc.Metrics)
                        {
                            table.Cell().Padding(2).Border(0.6f).BorderColor(RuleColor).Background("#F7F9FC")
                                .BorderLeft(3).BorderColor(ToneColor(m.Tone))
                                .PaddingVertical(5).PaddingHorizontal(7).Column(c =>
                                {
                                    c.Item().Text(m.Label.ToUpperInvariant()).FontSize(6.5f).SemiBold().FontColor(Muted).LetterSpacing(0.04f);
                                    c.Item().Text(doc.Format(m.Value, m.Kind)).FontSize(valueSize).Bold().FontColor(Primary);
                                });
                        }
                    });
                }

                foreach (var chart in doc.Charts.Where(c => c.Values.Count > 0 && c.Values.Any(v => v != 0)))
                {
                    col.Item().Element(c => ComposeChart(c, doc, chart));
                }

                foreach (var section in doc.Sections)
                {
                    col.Item().Element(c => ComposeSection(c, doc, section));
                }

                if (doc.Notes.Count > 0)
                {
                    col.Item().PaddingTop(4).Column(notes =>
                    {
                        foreach (var note in doc.Notes)
                            notes.Item().Text("• " + note).FontSize(7.5f).FontColor(Muted);
                    });
                }
            });
        }

        private static void ComposeChart(IContainer container, ReportDocument doc, ReportChartData chart)
        {
            var max = chart.Values.Count == 0 ? 0 : chart.Values.Max(v => Math.Abs(v));
            var total = chart.Values.Sum();
            container.Border(0.6f).BorderColor(RuleColor).Padding(8).Column(col =>
            {
                col.Item().PaddingBottom(4).Text(chart.Title).FontSize(9.5f).Bold().FontColor(Primary);
                var palette = new[] { "#2E75B6", "#1F9D74", "#E0A526", "#C0504D", "#7E57C2", "#26A69A", "#8D6E63", "#5C6BC0" };
                for (var i = 0; i < chart.Labels.Count && i < chart.Values.Count && i < 15; i++)
                {
                    var value = chart.Values[i];
                    var share = max == 0 ? 0 : (float)(Math.Abs(value) / max);
                    var color = palette[i % palette.Length];
                    col.Item().PaddingVertical(1.5f).Row(row =>
                    {
                        row.ConstantItem(150).Text(chart.Labels[i]).FontSize(7.5f);
                        row.RelativeItem().Height(9).Row(bar =>
                        {
                            if (share > 0.001f) bar.RelativeItem(share).Background(color);
                            if (share < 0.999f) bar.RelativeItem(1 - share);
                        });
                        var label = doc.Format(value, chart.IsMoney ? ReportColumnKind.Money : ReportColumnKind.Number);
                        if (total != 0 && chart.Type == "doughnut") label += $"  ({value * 100 / total:0.#}%)";
                        row.ConstantItem(120).AlignRight().Text(label).FontSize(7.5f).SemiBold();
                    });
                }
            });
        }

        private static void ComposeSection(IContainer container, ReportDocument doc, ReportSection section)
        {
            container.Column(col =>
            {
                if (!string.IsNullOrWhiteSpace(section.Title))
                {
                    col.Item().PaddingBottom(3).Row(r =>
                    {
                        r.RelativeItem().Text(section.Title).FontSize(10).Bold().FontColor(Primary);
                        if (!section.Compact) r.AutoItem().AlignBottom().Text($"{section.Rows.Count} record(s)").FontSize(7).FontColor(Muted);
                    });
                }

                if (section.Rows.Count == 0)
                {
                    col.Item().Border(0.6f).BorderColor(RuleColor).Padding(10).AlignCenter().Text(section.EmptyText).FontSize(8.5f).Italic().FontColor(Muted);
                    return;
                }

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        if (!section.Compact) c.ConstantColumn(22);
                        foreach (var column in section.Columns) c.RelativeColumn(column.Width);
                    });

                    table.Header(header =>
                    {
                        if (!section.Compact) header.Cell().Background(Primary).PaddingVertical(4).PaddingHorizontal(3).Text("#").FontSize(7.5f).Bold().FontColor(HeaderText);
                        foreach (var column in section.Columns)
                        {
                            var cell = header.Cell().Background(Primary).PaddingVertical(4).PaddingHorizontal(4);
                            (column.IsNumeric ? cell.AlignRight() : cell).Text(column.Header).FontSize(7.5f).Bold().FontColor(HeaderText);
                        }
                    });

                    for (var r = 0; r < section.Rows.Count; r++)
                    {
                        var row = section.Rows[r];
                        var fill = r % 2 == 1 ? Zebra : "#FFFFFF";
                        if (!section.Compact)
                        {
                            table.Cell().Background(fill).BorderBottom(0.4f).BorderColor(RuleColor).PaddingVertical(3).PaddingHorizontal(3)
                                .Text((r + 1).ToString(CultureInfo.InvariantCulture)).FontSize(7).FontColor(Muted);
                        }
                        for (var c = 0; c < section.Columns.Count; c++)
                        {
                            var column = section.Columns[c];
                            var cell = table.Cell().Background(fill).BorderBottom(0.4f).BorderColor(RuleColor).PaddingVertical(3).PaddingHorizontal(4);
                            (column.IsNumeric ? cell.AlignRight() : cell).Text(doc.Format(c < row.Length ? row[c] : null, column.Kind)).FontSize(7.8f);
                        }
                    }

                    if (section.ShowTotals)
                    {
                        if (!section.Compact) table.Cell().Background(TotalsFill).BorderTop(1).BorderColor(Primary).PaddingVertical(4).PaddingHorizontal(3).Text(string.Empty);
                        for (var c = 0; c < section.Columns.Count; c++)
                        {
                            var column = section.Columns[c];
                            var cell = table.Cell().Background(TotalsFill).BorderTop(1).BorderColor(Primary).PaddingVertical(4).PaddingHorizontal(4);
                            var text = c == 0 ? "Total" : doc.Format(section.TotalFor(c), column.Kind);
                            (column.IsNumeric ? cell.AlignRight() : cell).Text(text).FontSize(7.8f).Bold();
                        }
                    }
                });
            });
        }

        private static void ComposeFooter(IContainer container, ReportDocument doc)
        {
            container.Column(col =>
            {
                col.Item().LineHorizontal(0.6f).LineColor(RuleColor);
                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text($"MedyxHMS · {doc.Title} · {doc.FooterNote ?? "Confidential – for internal use"}").FontSize(7).FontColor(Muted);
                    row.AutoItem().Text(t =>
                    {
                        t.Span("Page ").FontSize(7).FontColor(Muted);
                        t.CurrentPageNumber().FontSize(7).FontColor(Muted);
                        t.Span(" of ").FontSize(7).FontColor(Muted);
                        t.TotalPages().FontSize(7).FontColor(Muted);
                    });
                });
            });
        }

        private static string ToneColor(string tone) => tone switch
        {
            "success" => "#1F9D74",
            "danger" => "#C0392B",
            "warning" => "#E0A526",
            "info" => "#17A2B8",
            "secondary" => "#7A8594",
            _ => Accent
        };

        // ── Excel ────────────────────────────────────────────────

        public byte[] BuildReportExcel(ReportDocument doc)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(SheetName(doc.Title));
            var width = Math.Max(6, doc.Sections.Select(s => s.Columns.Count + 1).DefaultIfEmpty(6).Max());
            var moneyFormat = MoneyFormat(doc.CurrencySymbol);
            var r = 1;

            void TitleRow(string text, double size, bool bold, string color)
            {
                var range = ws.Range(r, 1, r, width);
                range.Merge();
                range.Value = text;
                range.Style.Font.FontSize = size;
                range.Style.Font.Bold = bold;
                range.Style.Font.FontColor = XLColor.FromHtml(color);
                r++;
            }

            TitleRow(string.IsNullOrWhiteSpace(doc.HospitalName) ? "MedyxHMS" : doc.HospitalName, 15, true, Primary);
            if (!string.IsNullOrWhiteSpace(doc.HospitalAddress)) TitleRow(doc.HospitalAddress, 9, false, Muted);
            if (!string.IsNullOrWhiteSpace(doc.HospitalContact)) TitleRow(doc.HospitalContact, 9, false, Muted);
            if (Branding.LogoPng is { } logo)
            {
                // Logo at the top left; the letterhead text starts to the right of it.
                ws.Range(1, 1, r - 1, width).Style.Alignment.Indent = 5;
                using var logoStream = new MemoryStream(logo);
                ws.AddPicture(logoStream).WithPlacement(ClosedXML.Excel.Drawings.XLPicturePlacement.FreeFloating).MoveTo(ws.Cell(1, 1), 4, 3).WithSize(64, 50);
            }
            r++;
            TitleRow(doc.Title, 13, true, Primary);
            var period = string.Join("   |   ", new[]
            {
                doc.PeriodText,
                "Generated " + doc.GeneratedAt.ToString("dd-MMM-yyyy hh:mm tt", CultureInfo.InvariantCulture) + (string.IsNullOrWhiteSpace(doc.GeneratedBy) ? string.Empty : " by " + doc.GeneratedBy)
            }.Where(s => !string.IsNullOrWhiteSpace(s)));
            TitleRow(period, 9, false, Muted);
            ws.Range(r - 1, 1, r - 1, width).Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            ws.Range(r - 1, 1, r - 1, width).Style.Border.BottomBorderColor = XLColor.FromHtml(Primary);
            r++;

            // Key figures: label / value pairs, two per row.
            if (doc.Metrics.Count > 0)
            {
                for (var i = 0; i < doc.Metrics.Count; i += 2)
                {
                    for (var k = 0; k < 2 && i + k < doc.Metrics.Count; k++)
                    {
                        var m = doc.Metrics[i + k];
                        var labelCell = ws.Cell(r, 2 + k * 3);
                        labelCell.Value = m.Label;
                        labelCell.Style.Font.FontColor = XLColor.FromHtml(Muted);
                        var valueCell = ws.Cell(r, 3 + k * 3);
                        SetCell(valueCell, m.Value, m.Kind, moneyFormat);
                        valueCell.Style.Font.Bold = true;
                        valueCell.Style.Font.FontColor = XLColor.FromHtml(Primary);
                        valueCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    }

                    r++;
                }

                r++;
            }

            var firstTable = true;
            foreach (var section in doc.Sections)
            {
                if (!string.IsNullOrWhiteSpace(section.Title))
                {
                    var t = ws.Cell(r, 1);
                    t.Value = section.Title;
                    t.Style.Font.Bold = true;
                    t.Style.Font.FontSize = 11;
                    t.Style.Font.FontColor = XLColor.FromHtml(Primary);
                    r++;
                }

                var headerRow = r;
                ws.Cell(r, 1).Value = "#";
                for (var c = 0; c < section.Columns.Count; c++)
                {
                    ws.Cell(r, c + 2).Value = section.Columns[c].Header;
                    if (!section.Columns[c].IsNumeric) ws.Cell(r, c + 2).Style.Alignment.Indent = 1;
                }
                var header = ws.Range(r, 1, r, section.Columns.Count + 1);
                header.Style.Font.Bold = true;
                header.Style.Font.FontColor = XLColor.White;
                header.Style.Fill.BackgroundColor = XLColor.FromHtml(Primary);
                header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                for (var c = 0; c < section.Columns.Count; c++)
                    if (section.Columns[c].IsNumeric) ws.Cell(r, c + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Row(r).Height = 20;
                r++;

                if (section.Rows.Count == 0)
                {
                    ws.Cell(r, 2).Value = section.EmptyText;
                    ws.Cell(r, 2).Style.Font.Italic = true;
                    ws.Cell(r, 2).Style.Font.FontColor = XLColor.FromHtml(Muted);
                    r += 2;
                    continue;
                }

                var firstData = r;
                for (var i = 0; i < section.Rows.Count; i++, r++)
                {
                    var row = section.Rows[i];
                    ws.Cell(r, 1).Value = i + 1;
                    ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml(Muted);
                    for (var c = 0; c < section.Columns.Count; c++)
                    {
                        SetCell(ws.Cell(r, c + 2), c < row.Length ? row[c] : null, section.Columns[c].Kind, moneyFormat);
                        if (!section.Columns[c].IsNumeric) ws.Cell(r, c + 2).Style.Alignment.Indent = 1; // space after right-aligned amounts
                    }
                    if (i % 2 == 1) ws.Range(r, 1, r, section.Columns.Count + 1).Style.Fill.BackgroundColor = XLColor.FromHtml(Zebra);
                }

                var lastData = r - 1;
                ws.Range(firstData, 1, lastData, section.Columns.Count + 1).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
                ws.Range(firstData, 1, lastData, section.Columns.Count + 1).Style.Border.BottomBorderColor = XLColor.FromHtml(RuleColor);

                if (section.ShowTotals)
                {
                    ws.Cell(r, 2).Value = "Total";
                    for (var c = 0; c < section.Columns.Count; c++)
                    {
                        if (!section.Columns[c].Total) continue;
                        var cell = ws.Cell(r, c + 2);
                        var letter = cell.Address.ColumnLetter;
                        cell.FormulaA1 = $"SUM({letter}{firstData}:{letter}{lastData})";
                        var wholeNumbers = section.Rows.All(row => c >= row.Length || row[c] == null
                            || (decimal.TryParse(Convert.ToString(row[c], CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) && v == decimal.Truncate(v)));
                        cell.Style.NumberFormat.Format = section.Columns[c].Kind switch
                        {
                            ReportColumnKind.Money => moneyFormat,
                            ReportColumnKind.Integer => "#,##0",
                            _ => wholeNumbers ? "#,##0" : "#,##0.00"
                        };
                    }

                    var totals = ws.Range(r, 1, r, section.Columns.Count + 1);
                    totals.Style.Font.Bold = true;
                    totals.Style.Fill.BackgroundColor = XLColor.FromHtml(TotalsFill);
                    totals.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    totals.Style.Border.TopBorderColor = XLColor.FromHtml(Primary);
                    r++;
                }

                if (firstTable)
                {
                    ws.Range(headerRow, 1, lastData, section.Columns.Count + 1).SetAutoFilter();
                    firstTable = false;
                }

                r++;
            }

            // Chart data (Excel charts are drawn from these tables by the reader if needed).
            foreach (var chart in doc.Charts.Where(c => c.Values.Count > 0))
            {
                ws.Cell(r, 1).Value = chart.Title;
                ws.Cell(r, 1).Style.Font.Bold = true;
                ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml(Primary);
                r++;
                for (var i = 0; i < chart.Labels.Count && i < chart.Values.Count; i++, r++)
                {
                    ws.Cell(r, 1).Value = chart.Labels[i];
                    SetCell(ws.Cell(r, 2), chart.Values[i], chart.IsMoney ? ReportColumnKind.Money : ReportColumnKind.Number, moneyFormat);
                }

                r++;
            }

            foreach (var note in doc.Notes)
            {
                ws.Cell(r, 1).Value = note;
                ws.Cell(r, 1).Style.Font.Italic = true;
                ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml(Muted);
                r++;
            }

            ws.Column(1).Width = 6;
            for (var c = 2; c <= width; c++)
            {
                ws.Column(c).AdjustToContents(8, r);
                ws.Column(c).Width += 2;
                if (ws.Column(c).Width > 50) ws.Column(c).Width = 50;
                if (ws.Column(c).Width < 10) ws.Column(c).Width = 10;
            }

            ws.PageSetup.PageOrientation = doc.Landscape ? XLPageOrientation.Landscape : XLPageOrientation.Portrait;
            ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
            ws.PageSetup.FitToPages(1, 0);
            ws.PageSetup.Margins.Left = 0.4;
            ws.PageSetup.Margins.Right = 0.4;
            ws.PageSetup.Footer.Center.AddText("Page ");
            ws.PageSetup.Footer.Center.AddText(XLHFPredefinedText.PageNumber);
            ws.PageSetup.Footer.Center.AddText(" of ");
            ws.PageSetup.Footer.Center.AddText(XLHFPredefinedText.NumberOfPages);
            ws.SheetView.ZoomScale = 100;
            ws.ShowGridLines = false;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static void SetCell(IXLCell cell, object? value, ReportColumnKind kind, string moneyFormat)
        {
            if (value == null)
            {
                return;
            }

            switch (kind)
            {
                case ReportColumnKind.Money:
                    cell.Value = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                    cell.Style.NumberFormat.Format = moneyFormat;
                    break;
                case ReportColumnKind.Integer:
                    cell.Value = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                    cell.Style.NumberFormat.Format = "#,##0";
                    break;
                case ReportColumnKind.Number:
                    if (decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var n))
                    {
                        cell.Value = n;
                        cell.Style.NumberFormat.Format = n == decimal.Truncate(n) ? "#,##0" : "#,##0.00";
                    }
                    else cell.Value = Convert.ToString(value, CultureInfo.InvariantCulture);
                    break;
                case ReportColumnKind.Percent:
                    cell.Value = Convert.ToDecimal(value, CultureInfo.InvariantCulture) / 100m;
                    cell.Style.NumberFormat.Format = "0.0%";
                    break;
                case ReportColumnKind.Date when value is DateTime d:
                    cell.Value = d;
                    cell.Style.NumberFormat.Format = "dd-mmm-yyyy";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    break;
                case ReportColumnKind.DateTime when value is DateTime dt:
                    cell.Value = dt;
                    cell.Style.NumberFormat.Format = "dd-mmm-yyyy hh:mm AM/PM";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    break;
                default:
                    cell.Value = Convert.ToString(value, CultureInfo.InvariantCulture);
                    break;
            }
        }

        private static string MoneyFormat(string? symbol)
        {
            symbol = (symbol ?? string.Empty).Replace("\"", string.Empty);
            if (symbol.Length == 0) return "#,##0.00";
            var prefix = symbol.Any(char.IsLetter) ? symbol + " " : symbol;
            return $"\"{prefix}\"#,##0.00;-\"{prefix}\"#,##0.00";
        }

        private static string SheetName(string title)
        {
            var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
            var name = new string((string.IsNullOrWhiteSpace(title) ? "Report" : title).Where(c => !invalid.Contains(c)).ToArray()).Trim();
            if (name.Length == 0) name = "Report";
            return name.Length > 31 ? name.Substring(0, 31) : name;
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
