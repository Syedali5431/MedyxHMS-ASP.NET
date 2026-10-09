using System.Globalization;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32.SafeHandles;

namespace MedyxHMS.Services.Implementations
{
    /// <summary>
    /// Sends print jobs straight to printers on the network:
    /// IP printers receive the data on their raw port (9100, "JetDirect"), Windows-shared LAN printers
    /// (\\COMPUTER\Printer) receive it through the Windows print share. Receipts are laid out as ESC/POS
    /// (supported by practically every thermal receipt printer) or plain text.
    /// </summary>
    public class NetworkPrintService : INetworkPrintService
    {
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(4);
        private const int MaxJobBytes = 20 * 1024 * 1024;

        private readonly ApplicationDbContext _context;
        private readonly ILogger<NetworkPrintService> _logger;

        static NetworkPrintService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public NetworkPrintService(ApplicationDbContext context, ILogger<NetworkPrintService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Printer>> GetPrintersForHospitalAsync(int? hospitalId, string? printerType = null)
        {
            var query = _context.Printers.AsNoTracking().Where(p => p.IsActive && (p.HospitalId == null || p.HospitalId == hospitalId));
            if (!string.IsNullOrEmpty(printerType))
            {
                query = query.Where(p => p.PrinterType == printerType);
            }

            // The hospital's own default first, then defaults for every hospital, then by name.
            return await query
                .OrderByDescending(p => p.IsDefault && p.HospitalId != null)
                .ThenByDescending(p => p.IsDefault)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<PrintResult> TestConnectionAsync(Printer printer, CancellationToken cancellationToken = default)
        {
            if (printer.ConnectionType == Printer.ConnectionLan)
            {
                var host = ShareHost(printer.SharePath);
                if (host == null)
                {
                    return new PrintResult(false, "The share path must look like \\\\COMPUTER\\Printer.");
                }

                // The Windows print share is reached through file and printer sharing (SMB, port 445).
                var reachable = await CanConnectAsync(host, 445, cancellationToken);
                return reachable.Success
                    ? new PrintResult(true, $"Computer {host} answers (file and printer sharing). Jobs go to {printer.SharePath}.")
                    : new PrintResult(false, $"Computer {host} does not answer on the network ({reachable.Message}). Check that it is switched on and that file and printer sharing is on.");
            }

            var result = await CanConnectAsync(printer.IpAddress, printer.Port, cancellationToken);
            return result.Success
                ? new PrintResult(true, $"Printer answers at {printer.IpAddress}:{printer.Port}.")
                : new PrintResult(false, $"No answer from {printer.IpAddress}:{printer.Port} ({result.Message}). Check the IP address, the port (usually 9100) and that the printer is on.");
        }

        public async Task<PrintResult> SendAsync(Printer printer, byte[] data, CancellationToken cancellationToken = default)
        {
            if (data.Length == 0)
            {
                return new PrintResult(false, "Nothing to print.");
            }

            if (data.Length > MaxJobBytes)
            {
                return new PrintResult(false, "The print job is too large.");
            }

            var copies = Math.Clamp(printer.Copies, 1, 5);
            try
            {
                if (printer.ConnectionType == Printer.ConnectionLan)
                {
                    for (var i = 0; i < copies; i++)
                    {
                        await WriteToShareAsync(printer.SharePath, data, cancellationToken);
                    }

                    return new PrintResult(true, $"Sent to {printer.Name} ({printer.SharePath}).");
                }

                using var client = new TcpClient();
                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    timeout.CancelAfter(ConnectTimeout);
                    await client.ConnectAsync(printer.IpAddress, printer.Port, timeout.Token);
                }

                client.SendTimeout = 15000;
                await using var stream = client.GetStream();
                for (var i = 0; i < copies; i++)
                {
                    await stream.WriteAsync(data, cancellationToken);
                }

                await stream.FlushAsync(cancellationToken);
                client.Client.Shutdown(SocketShutdown.Send);
                return new PrintResult(true, $"Sent to {printer.Name} ({printer.IpAddress}:{printer.Port}).");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new PrintResult(false, $"{printer.Name} did not answer in time ({printer.Address}).");
            }
            catch (Exception ex) when (ex is SocketException or IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Printing to {Printer} ({Address}) failed", printer.Name, printer.Address);
                return new PrintResult(false, $"Printing to {printer.Name} failed: {ex.Message}");
            }
        }

        // ── Receipts (ESC/POS or text) ───────────────────────────

        public byte[] BuildReceipt(Printer printer, ThermalReceiptViewModel receipt)
        {
            var doc = new ReceiptDocument(printer);
            var money = receipt.Settings.FormatMoney;

            doc.Center().Logo();
            doc.Center().Bold().Large().Line(receipt.Header.Name.ToUpperInvariant()).Normal();
            if (receipt.Settings.ShowHospitalAddress)
            {
                if (!string.IsNullOrWhiteSpace(receipt.Header.Address)) doc.Wrap(receipt.Header.Address);
                if (!string.IsNullOrWhiteSpace(receipt.Header.Phone)) doc.Line("Tel: " + receipt.Header.Phone);
                if (!string.IsNullOrWhiteSpace(receipt.Header.LicenseNumber)) doc.Line("Reg: " + receipt.Header.LicenseNumber);
            }

            doc.Feed().Bold().Line(receipt.Title.ToUpperInvariant()).Normal();
            if (!string.IsNullOrWhiteSpace(receipt.Highlight))
            {
                if (!string.IsNullOrWhiteSpace(receipt.HighlightLabel)) doc.Line(receipt.HighlightLabel);
                doc.Bold().Large().Line(receipt.Highlight).Normal();
            }

            doc.Left().Rule();
            foreach (var line in receipt.Lines)
            {
                doc.Pair(line.Label, line.Value);
            }

            if (receipt.Items.Count > 0)
            {
                doc.Rule();
                foreach (var item in receipt.Items)
                {
                    doc.Wrap(item.Description);
                    doc.Pair("  " + Qty(item.Quantity) + " x " + money(item.UnitPrice), money(item.Amount));
                }
            }

            if (receipt.Totals.Count > 0)
            {
                doc.Rule();
                foreach (var total in receipt.Totals)
                {
                    if (total.Emphasis) doc.Bold();
                    doc.Pair(total.Label, money(total.Amount));
                    if (total.Emphasis) doc.Normal();
                }
            }

            if (receipt.FooterLines.Count > 0)
            {
                doc.Rule();
                foreach (var line in receipt.FooterLines)
                {
                    doc.Pair(line.Label, line.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(receipt.Note))
            {
                doc.Wrap(receipt.Note);
            }

            doc.Rule();
            doc.Pair("Printed", receipt.PrintedAt.ToString("dd-MMM-yyyy hh:mm tt", CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(receipt.PrintedBy)) doc.Pair("By", receipt.PrintedBy);
            if (!string.IsNullOrWhiteSpace(receipt.Settings.FooterText))
            {
                doc.Feed().Center().Wrap(receipt.Settings.FooterText).Left();
            }

            return doc.Finish();
        }

        public byte[] BuildTestPage(Printer printer, string hospitalName, string printedBy)
        {
            var now = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt", CultureInfo.InvariantCulture);
            if (printer.PrinterType == Printer.TypeDocument && printer.PrintLanguage == Printer.LanguagePdf)
            {
                return QuestPDF.Fluent.Document.Create(container => container.Page(page =>
                {
                    page.Size(PaperFor(printer.PaperSize));
                    page.Margin(40);
                    page.Content().Column(col =>
                    {
                        col.Spacing(8);
                        col.Item().Text("MedyxHMS – printer test page").FontSize(20).Bold();
                        col.Item().Text(hospitalName).FontSize(13);
                        col.Item().Text($"Printer: {printer.Name} ({printer.Address})");
                        col.Item().Text($"Paper: {printer.PaperSize}   Language: PDF");
                        col.Item().Text($"Printed {now} by {printedBy}");
                        col.Item().PaddingTop(10).Text("If you can read this page, the printer is set up correctly.");
                    });
                })).GeneratePdf();
            }

            var doc = new ReceiptDocument(printer);
            doc.Center().Bold().Large().Line("TEST PRINT").Normal().Line(hospitalName).Rule().Left();
            doc.Pair("Printer", printer.Name);
            doc.Pair("Address", printer.Address);
            doc.Pair("Paper", printer.PaperSize);
            doc.Pair("Language", printer.PrintLanguage);
            doc.Pair("Printed", now);
            doc.Pair("By", printedBy);
            doc.Rule();
            // Ruler 1234567890123… as wide as the paper: it must fit on one line.
            doc.Line(string.Concat(Enumerable.Range(1, doc.Columns).Select(i => (char)('0' + i % 10))));
            doc.Wrap("If the ruler above fits on one line, the paper width is right.");
            return doc.Finish();
        }

        public byte[] BuildDocument(Printer printer, byte[] pdf, string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
        {
            if (printer.PrintLanguage == Printer.LanguagePdf)
            {
                return pdf;
            }

            // Plain text: fixed-width columns, 80 characters per line (A4 portrait at 10 cpi), form feed at the end.
            var sb = new StringBuilder();
            sb.AppendLine(title);
            sb.AppendLine("Printed " + DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt", CultureInfo.InvariantCulture));
            sb.AppendLine(new string('=', 80));
            var widths = headers.Select((h, i) => Math.Min(30, Math.Max(h.Length, rows.Select(r => i < r.Count ? (r[i] ?? string.Empty).Length : 0).DefaultIfEmpty(0).Max()))).ToArray();
            sb.AppendLine(string.Join(" ", headers.Select((h, i) => Fit(h, widths[i]))));
            sb.AppendLine(new string('-', Math.Min(132, widths.Sum() + widths.Length - 1)));
            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(" ", headers.Select((_, i) => Fit(i < row.Count ? row[i] ?? string.Empty : string.Empty, widths[i]))));
            }

            sb.AppendLine(new string('-', Math.Min(132, widths.Sum() + widths.Length - 1)));
            sb.AppendLine($"{rows.Count} row(s)");
            sb.Append('\f');
            return Encoding.ASCII.GetBytes(Ascii(sb.ToString()));
        }

        // ── helpers ──────────────────────────────────────────────

        private static string Fit(string value, int width) => value.Length > width ? value[..Math.Max(0, width - 1)] + "~" : value.PadRight(width);

        private static string Qty(decimal v) => v == Math.Truncate(v) ? v.ToString("0", CultureInfo.InvariantCulture) : v.ToString("0.##", CultureInfo.InvariantCulture);

        private static QuestPDF.Helpers.PageSize PaperFor(string paper) => paper switch
        {
            "A5" => QuestPDF.Helpers.PageSizes.A5,
            "Letter" => QuestPDF.Helpers.PageSizes.Letter,
            _ => QuestPDF.Helpers.PageSizes.A4
        };

        private static string? ShareHost(string sharePath)
        {
            var parts = (sharePath ?? string.Empty).Trim().TrimStart('\\').Split('\\', StringSplitOptions.RemoveEmptyEntries);
            return (sharePath ?? string.Empty).StartsWith(@"\\") && parts.Length >= 2 ? parts[0] : null;
        }

        private static async Task<PrintResult> CanConnectAsync(string host, int port, CancellationToken cancellationToken)
        {
            try
            {
                using var client = new TcpClient();
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(ConnectTimeout);
                await client.ConnectAsync(host, port, timeout.Token);
                return new PrintResult(true, "connected");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new PrintResult(false, "timed out");
            }
            catch (SocketException ex)
            {
                return new PrintResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Writes raw data to a Windows printer share, the same as "copy /b file \\PC\Printer". .NET file APIs treat
        /// "\\PC\Printer" as a folder, so the share is opened with CreateFile directly.
        /// </summary>
        private static async Task WriteToShareAsync(string sharePath, byte[] data, CancellationToken cancellationToken)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new InvalidOperationException("LAN (Windows share) printers need the application to run on Windows.");
            }

            using var handle = CreateFileW(sharePath.Trim(), GenericWrite, 0, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
            if (handle.IsInvalid)
            {
                var error = Marshal.GetLastWin32Error();
                throw new IOException($"cannot open {sharePath} (Windows error {error}: {new System.ComponentModel.Win32Exception(error).Message})");
            }

            await using var stream = new FileStream(handle, FileAccess.Write, 4096, isAsync: false);
            await stream.WriteAsync(data, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        private const uint GenericWrite = 0x40000000;
        private const uint OpenExisting = 3;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFileHandle CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes,
            uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        /// <summary>Text for printers without a full character set: accents removed, other characters replaced.</summary>
        private static string Ascii(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(c switch
                {
                    '–' or '—' => '-',
                    '‘' or '’' => '\'',
                    '“' or '”' => '"',
                    '•' or '·' => '*',
                    '₹' => 'R',
                    _ => c < 128 ? c : '?'
                });
            }

            return sb.ToString();
        }

        /// <summary>Receipt laid out in columns for the roll width, as ESC/POS commands or plain text.</summary>
        private sealed class ReceiptDocument
        {
            private readonly MemoryStream _out = new();
            private readonly bool _escPos;
            private readonly Printer _printer;
            private int _align; // 0 left, 1 centre
            private bool _large;

            public ReceiptDocument(Printer printer)
            {
                _printer = printer;
                _escPos = printer.PrintLanguage == Printer.LanguageEscPos;
                // Font A: 48 characters on 80 mm rolls, 32 on 58 mm rolls; text pages 80 characters.
                Columns = printer.PrinterType == Printer.TypeDocument ? 80 : printer.PaperSize == "58mm" ? 32 : 48;
                if (_escPos)
                {
                    Raw(0x1B, 0x40);       // initialise
                    Raw(0x1B, 0x74, 0x00); // code page 437
                }
            }

            public int Columns { get; }

            private int Width => _large ? Columns / 2 : Columns;

            public ReceiptDocument Center() { _align = 1; if (_escPos) Raw(0x1B, 0x61, 0x01); return this; }
            public ReceiptDocument Left() { _align = 0; if (_escPos) Raw(0x1B, 0x61, 0x00); return this; }
            public ReceiptDocument Bold() { if (_escPos) Raw(0x1B, 0x45, 0x01); return this; }
            public ReceiptDocument Large() { _large = _escPos; if (_escPos) Raw(0x1D, 0x21, 0x11); return this; }

            public ReceiptDocument Normal()
            {
                _large = false;
                if (_escPos) { Raw(0x1B, 0x45, 0x00); Raw(0x1D, 0x21, 0x00); }
                return this;
            }

            public ReceiptDocument Feed() { Text("\n"); return this; }

            /// <summary>The Medyx logo as a raster image (ESC/POS printers only; plain-text printers skip it).</summary>
            public ReceiptDocument Logo()
            {
                if (_escPos && Branding.ReceiptLogoEscPos is { } logo)
                {
                    _out.Write(logo, 0, logo.Length);
                    Text("\n");
                }
                return this;
            }

            public ReceiptDocument Rule() => Line(new string('-', Columns));

            public ReceiptDocument Line(string text)
            {
                text = Ascii(text ?? string.Empty);
                if (text.Length > Width) return Wrap(text);
                // Plain text has no alignment command: centre with spaces.
                Text((!_escPos && _align == 1 ? new string(' ', Math.Max(0, (Width - text.Length) / 2)) : string.Empty) + text + "\n");
                return this;
            }

            public ReceiptDocument Wrap(string text)
            {
                foreach (var part in WrapWords(Ascii(text ?? string.Empty), Width))
                {
                    Text((!_escPos && _align == 1 ? new string(' ', Math.Max(0, (Width - part.Length) / 2)) : string.Empty) + part + "\n");
                }

                return this;
            }

            /// <summary>"Label ........ value" on one line; a long value continues right-aligned below.</summary>
            public ReceiptDocument Pair(string label, string value)
            {
                label = Ascii(label ?? string.Empty);
                value = Ascii(value ?? string.Empty);
                var width = Width;
                if (label.Length + 1 + value.Length <= width)
                {
                    Text(label + new string(' ', width - label.Length - value.Length) + value + "\n");
                    return this;
                }

                foreach (var part in WrapWords(label, width)) Text(part + "\n");
                foreach (var part in WrapWords(value, width)) Text(part.PadLeft(width) + "\n");
                return this;
            }

            public byte[] Finish()
            {
                if (_escPos)
                {
                    Raw(0x1B, 0x64, 0x04); // feed 4 lines
                    if (_printer.CutPaper) Raw(0x1D, 0x56, 0x42, 0x00); // partial cut
                    if (_printer.OpenCashDrawer) Raw(0x1B, 0x70, 0x00, 0x19, 0xFA);
                }
                else
                {
                    Text("\n\n\n\f");
                }

                return _out.ToArray();
            }

            private void Text(string s) { var bytes = Encoding.ASCII.GetBytes(s); _out.Write(bytes, 0, bytes.Length); }

            private void Raw(params byte[] bytes) => _out.Write(bytes, 0, bytes.Length);

            private static IEnumerable<string> WrapWords(string text, int width)
            {
                foreach (var paragraph in text.Replace("\r", string.Empty).Split('\n'))
                {
                    var line = new StringBuilder();
                    foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var w = word;
                        while (w.Length > width)
                        {
                            if (line.Length > 0) { yield return line.ToString(); line.Clear(); }
                            yield return w[..width];
                            w = w[width..];
                        }

                        if (line.Length > 0 && line.Length + 1 + w.Length > width) { yield return line.ToString(); line.Clear(); }
                        if (line.Length > 0) line.Append(' ');
                        line.Append(w);
                    }

                    yield return line.ToString();
                }
            }
        }
    }
}
