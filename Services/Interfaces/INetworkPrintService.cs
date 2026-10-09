using MedyxHMS.Models;
using MedyxHMS.ViewModels;

namespace MedyxHMS.Services.Interfaces
{
    public sealed record PrintResult(bool Success, string Message);

    /// <summary>Printing straight to network printers (IP printers on a raw port, Windows-shared LAN printers).</summary>
    public interface INetworkPrintService
    {
        /// <summary>Active printers usable in the hospital (its own printers and printers of every hospital).</summary>
        Task<List<Printer>> GetPrintersForHospitalAsync(int? hospitalId, string? printerType = null);

        /// <summary>Checks that the printer answers on the network (no paper is printed).</summary>
        Task<PrintResult> TestConnectionAsync(Printer printer, CancellationToken cancellationToken = default);

        /// <summary>Sends print data (ESC/POS, PDF or text) to the printer.</summary>
        Task<PrintResult> SendAsync(Printer printer, byte[] data, CancellationToken cancellationToken = default);

        /// <summary>A receipt laid out for the printer's roll width, in the printer's language.</summary>
        byte[] BuildReceipt(Printer printer, ThermalReceiptViewModel receipt);

        /// <summary>A short test page (receipt or document printer).</summary>
        byte[] BuildTestPage(Printer printer, string hospitalName, string printedBy);

        /// <summary>A report for a document printer: the PDF itself, or a text version for text printers.</summary>
        byte[] BuildDocument(Printer printer, byte[] pdf, string title, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows);
    }
}
