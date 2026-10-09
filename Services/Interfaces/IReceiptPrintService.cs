using MedyxHMS.ViewModels;

namespace MedyxHMS.Services.Interfaces
{
    /// <summary>Thermal receipt printing: printer settings and the hospital header for a record.</summary>
    public interface IReceiptPrintService
    {
        Task<ReceiptPrintSettings> GetSettingsAsync();
        Task SaveSettingsAsync(ReceiptPrintSettings settings, string? modifiedBy);

        /// <summary>Header for the given hospital (the record's own hospital), falling back to the default hospital.</summary>
        Task<ReceiptHeader> GetHeaderAsync(int? hospitalId);

        /// <summary>Creates a receipt with header, settings and paper width (?w=58 / ?w=80 overrides the setting).</summary>
        Task<ThermalReceiptViewModel> CreateAsync(string title, int? hospitalId, int? paperWidthOverride, string? printedBy);
    }
}
