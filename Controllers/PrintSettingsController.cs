using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MedyxHMS.Controllers
{
    /// <summary>
    /// Printing settings: the Windows default printer (browser print dialog) or network printers on the LAN / IP,
    /// the printer list, receipt options (thermal, 80 mm / 58 mm) and test prints.
    /// </summary>
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class PrintSettingsController : Controller
    {
        private static readonly string[] ReceiptPapers = { "80mm", "58mm" };
        private static readonly string[] DocumentPapers = { "A4", "A5", "Letter" };

        private readonly IReceiptPrintService _receiptPrint;
        private readonly INetworkPrintService _networkPrint;
        private readonly IHospitalContext _hospitalContext;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;
        private readonly IMemoryCache _cache;

        public PrintSettingsController(IReceiptPrintService receiptPrint, INetworkPrintService networkPrint, IHospitalContext hospitalContext,
            ApplicationDbContext context, IAuditService audit, IMemoryCache cache)
        {
            _receiptPrint = receiptPrint;
            _networkPrint = networkPrint;
            _hospitalContext = hospitalContext;
            _context = context;
            _audit = audit;
            _cache = cache;
        }

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool IsSuperAdmin => User.IsInRole("SuperAdmin");

        public async Task<IActionResult> Index()
        {
            await LoadPrintersAsync();
            return View(await _receiptPrint.GetSettingsAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ReceiptPrintSettings model)
        {
            if ((model.CurrencySymbol ?? string.Empty).Trim().Length > 5)
            {
                ModelState.AddModelError(nameof(model.CurrencySymbol), "Use at most 5 characters (e.g. PKR, Rs, $).");
            }

            if ((model.FooterText ?? string.Empty).Length > 200)
            {
                ModelState.AddModelError(nameof(model.FooterText), "Use at most 200 characters.");
            }

            if (model.PrintMode != ReceiptPrintSettings.PrintModeNetwork && model.PrintMode != ReceiptPrintSettings.PrintModeWindows)
            {
                ModelState.AddModelError(nameof(model.PrintMode), "Choose the Windows default printer or network printers.");
            }

            if (!ModelState.IsValid)
            {
                await LoadPrintersAsync();
                return View(model);
            }

            await _receiptPrint.SaveSettingsAsync(model, User.Identity?.Name);
            MedyxHMS.Extensions.HospitalCurrencyMiddleware.InvalidateCache(_cache);
            await _audit.LogActivityAsync(UserId, "UPDATE", "PrinterSettings", "Printing", null,
                $"mode {model.PrintMode}, currency {model.CurrencySymbol}, auto-print {model.AutoPrint}, address {model.ShowHospitalAddress}");

            var noPrinters = model.UsesNetworkPrinters && !await _context.Printers.AnyAsync(p => p.IsActive);
            TempData[noPrinters ? "WarningMessage" : "SuccessMessage"] = noPrinters
                ? "Settings saved. Network printing is on, but no printer is set up yet – add a printer below (until then the print dialog is used)."
                : "Printer settings saved.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /PrintSettings/TestReceipt – sample receipt to check alignment on the printer.
        public async Task<IActionResult> TestReceipt(int? w)
        {
            return View("ThermalReceipt", await BuildSampleReceiptAsync(w));
        }

        // ── Printers ─────────────────────────────────────────────

        public async Task<IActionResult> Printer(int? id)
        {
            Printer model;
            if (id.HasValue)
            {
                var printer = await FindManageablePrinterAsync(id.Value);
                if (printer == null) return PrinterNotFound();
                model = printer;
            }
            else
            {
                model = new Printer { HospitalId = IsSuperAdmin ? null : _hospitalContext.ActiveHospitalId ?? _hospitalContext.HospitalIdForNewRecords };
            }

            LoadPrinterFormLists();
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Printer(Printer model)
        {
            Printer? printer = null;
            if (model.Id > 0)
            {
                printer = await FindManageablePrinterAsync(model.Id);
                if (printer == null) return PrinterNotFound();
            }

            NormalizeAndValidate(model);
            if (!ModelState.IsValid)
            {
                LoadPrinterFormLists();
                return View(model);
            }

            var isNew = printer == null;
            printer ??= new Printer { CreatedDate = DateTime.Now, CreatedBy = User.Identity?.Name ?? string.Empty };
            printer.Name = model.Name;
            printer.PrinterType = model.PrinterType;
            printer.ConnectionType = model.ConnectionType;
            printer.IpAddress = model.IpAddress;
            printer.Port = model.Port;
            printer.SharePath = model.SharePath;
            printer.PaperSize = model.PaperSize;
            printer.PrintLanguage = model.PrintLanguage;
            printer.Copies = model.Copies;
            printer.CutPaper = model.CutPaper;
            printer.OpenCashDrawer = model.OpenCashDrawer;
            printer.HospitalId = model.HospitalId;
            printer.IsDefault = model.IsDefault && model.IsActive;
            printer.IsActive = model.IsActive;
            printer.Notes = model.Notes;
            if (!isNew) printer.UpdatedDate = DateTime.Now;
            if (isNew) _context.Printers.Add(printer);

            // One default printer per type and hospital.
            if (printer.IsDefault)
            {
                var others = await _context.Printers
                    .Where(p => p.Id != printer.Id && p.IsDefault && p.PrinterType == printer.PrinterType && p.HospitalId == printer.HospitalId)
                    .ToListAsync();
                others.ForEach(p => p.IsDefault = false);
            }

            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, isNew ? "CREATE" : "UPDATE", "Printer", printer.Id.ToString(), null,
                $"{printer.Name}: {printer.PrinterType}, {printer.ConnectionType} {printer.Address}, {printer.PaperSize}, {printer.PrintLanguage}");
            TempData["SuccessMessage"] = $"Printer \"{printer.Name}\" {(isNew ? "added" : "saved")}. Use Test connection / Test print to check it.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePrinter(int id)
        {
            var printer = await FindManageablePrinterAsync(id);
            if (printer == null) return PrinterNotFound();
            _context.Printers.Remove(printer);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "DELETE", "Printer", id.ToString(), $"{printer.Name} ({printer.Address})", null);
            TempData["SuccessMessage"] = $"Printer \"{printer.Name}\" removed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> TestConnection(int id)
        {
            var printer = await FindManageablePrinterAsync(id);
            if (printer == null) return PrinterNotFound();
            var result = await _networkPrint.TestConnectionAsync(printer, HttpContext.RequestAborted);
            await RecordTestAsync(printer, "Connection: " + result.Message);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = $"{printer.Name}: {result.Message}";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> TestPrint(int id)
        {
            var printer = await FindManageablePrinterAsync(id);
            if (printer == null) return PrinterNotFound();

            var hospitalName = printer.HospitalId.HasValue
                ? _hospitalContext.HospitalName(printer.HospitalId)
                : (await _receiptPrint.GetHeaderAsync(_hospitalContext.ActiveHospitalId ?? _hospitalContext.HospitalIdForNewRecords)).Name;
            var data = printer.IsReceiptPrinter
                ? _networkPrint.BuildReceipt(printer, await BuildSampleReceiptAsync(printer.PaperSize == "58mm" ? 58 : 80))
                : _networkPrint.BuildTestPage(printer, hospitalName, User.Identity?.Name ?? string.Empty);
            var result = await _networkPrint.SendAsync(printer, data, HttpContext.RequestAborted);
            await RecordTestAsync(printer, "Test print: " + result.Message);
            await _audit.LogActivityAsync(UserId, "PRINT", "Printer", id.ToString(), null, "Test print – " + result.Message);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? $"Test print sent to {printer.Name}. Check the printer."
                : result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ── helpers ──────────────────────────────────────────────

        private async Task<ThermalReceiptViewModel> BuildSampleReceiptAsync(int? width)
        {
            var hospitalId = _hospitalContext.ActiveHospitalId ?? _hospitalContext.HospitalIdForNewRecords;
            var vm = await _receiptPrint.CreateAsync("Test Receipt", hospitalId, width, User.Identity?.Name);
            vm.HighlightLabel = "Bill No.";
            vm.Highlight = "TEST-0001";
            vm.Lines.Add(new ReceiptLine("Date", DateTime.Now.ToString("dd-MMM-yyyy")));
            vm.Lines.Add(new ReceiptLine("Patient", "Sample Patient"));
            vm.Lines.Add(new ReceiptLine("Status", "Paid"));
            vm.Items.Add(new ReceiptItem { Description = "Consultation", Quantity = 1, UnitPrice = 1500, Amount = 1500 });
            vm.Items.Add(new ReceiptItem { Description = "Paracetamol 500 mg - 1 tab - Twice daily", Quantity = 10, UnitPrice = 4.5m, Amount = 45 });
            vm.Totals.Add(new ReceiptTotal("Total", 1545, emphasis: true));
            vm.Totals.Add(new ReceiptTotal("Paid", 1545));
            vm.Totals.Add(new ReceiptTotal("Balance due", 0, emphasis: true));
            vm.Note = "This is a test print. If the text is cut off, switch between 80 mm and 58 mm.";
            vm.BackUrl = Url.Action(nameof(Index));
            return vm;
        }

        private IQueryable<Printer> ManageablePrinters()
        {
            if (IsSuperAdmin)
            {
                return _context.Printers;
            }

            // An Admin manages the printers of their hospitals (printers for every hospital are SuperAdmin's).
            var ids = _hospitalContext.AccessibleHospitals.Select(h => h.Id).ToList();
            return _context.Printers.Where(p => p.HospitalId != null && ids.Contains(p.HospitalId.Value));
        }

        private Task<Printer?> FindManageablePrinterAsync(int id) => ManageablePrinters().FirstOrDefaultAsync(p => p.Id == id);

        private IActionResult PrinterNotFound()
        {
            TempData["ErrorMessage"] = "Printer not found, or it belongs to a hospital you do not manage.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadPrintersAsync()
        {
            var manageable = await ManageablePrinters().Select(p => p.Id).ToListAsync();
            var ids = IsSuperAdmin ? null : _hospitalContext.AccessibleHospitals.Select(h => h.Id).ToList();
            ViewBag.Printers = await _context.Printers.AsNoTracking()
                .Include(p => p.Hospital)
                .Where(p => ids == null || p.HospitalId == null || ids.Contains(p.HospitalId.Value))
                .OrderBy(p => p.PrinterType).ThenByDescending(p => p.IsDefault).ThenBy(p => p.Name)
                .ToListAsync();
            ViewBag.ManageablePrinterIds = manageable;
            ViewBag.MultiHospital = _hospitalContext.AllHospitals.Count(h => h.IsActive) > 1;
        }

        private void LoadPrinterFormLists()
        {
            var hospitals = IsSuperAdmin
                ? _hospitalContext.AllHospitals.Where(h => h.IsActive).ToList()
                : _hospitalContext.AccessibleHospitals.ToList();
            ViewBag.Hospitals = hospitals;
            ViewBag.CanChooseAllHospitals = IsSuperAdmin;
            ViewBag.ReceiptPapers = ReceiptPapers;
            ViewBag.DocumentPapers = DocumentPapers;
        }

        private void NormalizeAndValidate(Printer model)
        {
            model.Name = (model.Name ?? string.Empty).Trim();
            model.IpAddress = (model.IpAddress ?? string.Empty).Trim();
            model.SharePath = (model.SharePath ?? string.Empty).Trim();
            model.Notes = (model.Notes ?? string.Empty).Trim();
            foreach (var key in new[] { nameof(model.Hospital), nameof(model.CreatedBy), nameof(model.LastTestResult), nameof(model.Notes), nameof(model.IpAddress), nameof(model.SharePath) })
            {
                ModelState.Remove(key);
            }

            if (model.Name.Length == 0) ModelState.AddModelError(nameof(model.Name), "Enter a name, e.g. \"Reception receipt printer\".");
            if (model.PrinterType != Models.Printer.TypeReceipt && model.PrinterType != Models.Printer.TypeDocument)
                ModelState.AddModelError(nameof(model.PrinterType), "Choose receipt printer or document printer.");

            if (model.ConnectionType == Models.Printer.ConnectionIp)
            {
                model.SharePath = string.Empty;
                if (model.IpAddress.Length == 0)
                    ModelState.AddModelError(nameof(model.IpAddress), "Enter the printer's IP address (printed on the printer's network status page).");
                else if (!IPAddress.TryParse(model.IpAddress, out _) && !Regex.IsMatch(model.IpAddress, @"^[A-Za-z0-9]([A-Za-z0-9\-]{0,62})(\.[A-Za-z0-9]([A-Za-z0-9\-]{0,62}))*$"))
                    ModelState.AddModelError(nameof(model.IpAddress), "Enter an IP address (e.g. 192.168.1.50) or a host name.");
                if (model.Port is < 1 or > 65535)
                    ModelState.AddModelError(nameof(model.Port), "Port 1–65535 (raw printing is usually 9100).");
            }
            else if (model.ConnectionType == Models.Printer.ConnectionLan)
            {
                model.IpAddress = string.Empty;
                model.Port = 9100;
                if (!Regex.IsMatch(model.SharePath, @"^\\\\[^\\/:*?""<>|\s][^\\/:*?""<>|]*\\[^\\/:*?""<>|]+$"))
                    ModelState.AddModelError(nameof(model.SharePath), @"Enter the share path as \\COMPUTER\PrinterShareName.");
            }
            else
            {
                ModelState.AddModelError(nameof(model.ConnectionType), "Choose IP printer or shared LAN printer.");
            }

            var receipt = model.PrinterType == Models.Printer.TypeReceipt;
            if (!(receipt ? ReceiptPapers : DocumentPapers).Contains(model.PaperSize))
                model.PaperSize = receipt ? "80mm" : "A4";
            var languages = receipt ? new[] { Models.Printer.LanguageEscPos, Models.Printer.LanguageText } : new[] { Models.Printer.LanguagePdf, Models.Printer.LanguageText };
            if (!languages.Contains(model.PrintLanguage))
                model.PrintLanguage = languages[0];
            if (!receipt)
            {
                model.CutPaper = false;
                model.OpenCashDrawer = false;
            }

            if (model.Copies is < 1 or > 5) ModelState.AddModelError(nameof(model.Copies), "Copies: 1 to 5.");

            if (model.HospitalId.HasValue)
            {
                var allowed = IsSuperAdmin
                    ? _hospitalContext.AllHospitals.Any(h => h.Id == model.HospitalId)
                    : _hospitalContext.AccessibleHospitals.Any(h => h.Id == model.HospitalId);
                if (!allowed) ModelState.AddModelError(nameof(model.HospitalId), "Choose one of your hospitals.");
            }
            else if (!IsSuperAdmin)
            {
                ModelState.AddModelError(nameof(model.HospitalId), "Choose the hospital the printer is in.");
            }

            if (model.Name.Length > 100) ModelState.AddModelError(nameof(model.Name), "Use at most 100 characters.");
            if (model.Notes.Length > 500) ModelState.AddModelError(nameof(model.Notes), "Use at most 500 characters.");
        }

        private async Task RecordTestAsync(Printer printer, string result)
        {
            printer.LastTestedDate = DateTime.Now;
            printer.LastTestResult = result.Length > 500 ? result[..500] : result;
            await _context.SaveChangesAsync();
        }
    }
}
