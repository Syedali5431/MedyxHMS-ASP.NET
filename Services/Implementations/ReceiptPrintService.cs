using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public class ReceiptPrintService : IReceiptPrintService
    {
        private const string Category = "Printing";
        private const string KeyCurrency = "Printing:CurrencySymbol";
        private const string KeyFooter = "Printing:ReceiptFooter";
        private const string KeyAutoPrint = "Printing:AutoPrint";
        private const string KeyShowAddress = "Printing:ShowHospitalAddress";
        private const string KeyPrintMode = "Printing:Mode";

        private readonly ApplicationDbContext _context;

        /// <summary>Cookie with the paper width last chosen on a receipt (80 / 58 mm) on this computer.</summary>
        public const string WidthCookie = "MedyxHMS.ReceiptWidth";
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public ReceiptPrintService(ApplicationDbContext context, IHttpContextAccessor? httpContextAccessor = null)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ReceiptPrintSettings> GetSettingsAsync()
        {
            var values = await _context.Settings.AsNoTracking()
                .Where(s => s.Category == Category)
                .ToDictionaryAsync(s => s.Key, s => s.Value ?? string.Empty);

            var defaults = new ReceiptPrintSettings();
            return new ReceiptPrintSettings
            {
                PaperWidth = defaults.PaperWidth,
                CurrencySymbol = values.TryGetValue(KeyCurrency, out var c) ? c : defaults.CurrencySymbol,
                FooterText = values.TryGetValue(KeyFooter, out var f) ? f : defaults.FooterText,
                AutoPrint = !bool.TryParse(values.GetValueOrDefault(KeyAutoPrint), out var a) || a,
                ShowHospitalAddress = !bool.TryParse(values.GetValueOrDefault(KeyShowAddress), out var s) || s,
                PrintMode = values.GetValueOrDefault(KeyPrintMode) == ReceiptPrintSettings.PrintModeNetwork
                    ? ReceiptPrintSettings.PrintModeNetwork
                    : ReceiptPrintSettings.PrintModeWindows
            };
        }

        public async Task SaveSettingsAsync(ReceiptPrintSettings settings, string? modifiedBy)
        {
            await UpsertAsync(KeyCurrency, (settings.CurrencySymbol ?? string.Empty).Trim(), "string", "Currency symbol shown on all pages and printed on receipts", modifiedBy);
            await UpsertAsync(KeyFooter, (settings.FooterText ?? string.Empty).Trim(), "string", "Footer text printed on receipts", modifiedBy);
            await UpsertAsync(KeyAutoPrint, settings.AutoPrint.ToString().ToLowerInvariant(), "bool", "Open the print dialog automatically", modifiedBy);
            await UpsertAsync(KeyShowAddress, settings.ShowHospitalAddress.ToString().ToLowerInvariant(), "bool", "Print the hospital address and phone", modifiedBy);
            await UpsertAsync(KeyPrintMode, settings.PrintMode == ReceiptPrintSettings.PrintModeNetwork ? ReceiptPrintSettings.PrintModeNetwork : ReceiptPrintSettings.PrintModeWindows,
                "string", "Printing: Windows (default printer set in Windows, print dialog) or Network (printers on the LAN / IP from Settings → Printers)", modifiedBy);
            await _context.SaveChangesAsync();
        }

        public async Task<ReceiptHeader> GetHeaderAsync(int? hospitalId)
        {
            Hospital? hospital = null;
            if (hospitalId.HasValue)
            {
                hospital = await _context.Hospitals.AsNoTracking().FirstOrDefaultAsync(h => h.Id == hospitalId.Value);
            }

            hospital ??= await _context.Hospitals.AsNoTracking()
                .OrderByDescending(h => h.IsDefault).ThenByDescending(h => h.IsActive).ThenBy(h => h.Id)
                .FirstOrDefaultAsync();

            if (hospital == null)
            {
                return new ReceiptHeader { Name = "Medyx Hospital" };
            }

            return new ReceiptHeader
            {
                Name = hospital.Name,
                Address = string.Join(", ", new[] { hospital.Address, hospital.City }.Where(x => !string.IsNullOrWhiteSpace(x))),
                Phone = hospital.Phone,
                Email = hospital.Email,
                LicenseNumber = hospital.LicenseNumber
            };
        }

        public async Task<ThermalReceiptViewModel> CreateAsync(string title, int? hospitalId, int? paperWidthOverride, string? printedBy)
        {
            var settings = await GetSettingsAsync();
            return new ThermalReceiptViewModel
            {
                Title = title,
                Header = await GetHeaderAsync(hospitalId),
                Settings = settings,
                // Chosen on the receipt (80 mm / 58 mm buttons): ?w= first, then the last choice on this computer.
                PaperWidth = paperWidthOverride is 58 or 80 ? paperWidthOverride.Value
                    : int.TryParse(_httpContextAccessor?.HttpContext?.Request.Cookies[WidthCookie], out var remembered) && remembered is 58 or 80 ? remembered
                    : settings.PaperWidth,
                PrintedBy = printedBy ?? string.Empty,
                PrintedAt = DateTime.Now
            };
        }


        private async Task UpsertAsync(string key, string value, string type, string description, string? modifiedBy)
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null)
            {
                _context.Settings.Add(new Setting
                {
                    Key = key,
                    Value = value,
                    Type = type,
                    Category = Category,
                    Description = description,
                    IsSystem = false,
                    CreatedDate = DateTime.Now,
                    ModifiedBy = modifiedBy ?? "System"
                });
                return;
            }

            setting.Value = value;
            setting.ModifiedDate = DateTime.Now;
            setting.ModifiedBy = modifiedBy ?? "System";
        }
    }
}
