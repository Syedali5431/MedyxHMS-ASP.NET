using System.Security.Claims;
using System.Text.Json;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedyxHMS.Controllers
{
    /// <summary>Sends receipts straight to a network printer (print mode "network printers" in Settings → Printers).</summary>
    [Authorize(Roles = "SuperAdmin,Admin,Doctor,Nurse,Pharmacist,Accountant,Receptionist,LabTechnician,Pathologist,Radiologist,Staff")]
    public class PrintingController : Controller
    {
        private const int MaxReceiptJson = 200_000;

        private readonly INetworkPrintService _networkPrint;
        private readonly IReceiptPrintService _receiptPrint;
        private readonly IHospitalContext _hospitalContext;
        private readonly IAuditService _audit;

        public PrintingController(INetworkPrintService networkPrint, IReceiptPrintService receiptPrint, IHospitalContext hospitalContext, IAuditService audit)
        {
            _networkPrint = networkPrint;
            _receiptPrint = receiptPrint;
            _hospitalContext = hospitalContext;
            _audit = audit;
        }

        // POST /Printing/Receipt – the receipt shown on screen, printed on a receipt printer of the user's hospital.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Receipt(int printerId, string receipt)
        {
            if (string.IsNullOrWhiteSpace(receipt) || receipt.Length > MaxReceiptJson)
            {
                return BadRequest(new { success = false, message = "Nothing to print." });
            }

            ThermalReceiptViewModel? vm;
            try
            {
                vm = JsonSerializer.Deserialize<ThermalReceiptViewModel>(receipt);
            }
            catch (JsonException)
            {
                vm = null;
            }

            if (vm == null)
            {
                return BadRequest(new { success = false, message = "The receipt could not be read. Refresh the page and try again." });
            }

            var hospitalId = _hospitalContext.ActiveHospitalId ?? _hospitalContext.HospitalIdForNewRecords;
            var printers = await _networkPrint.GetPrintersForHospitalAsync(hospitalId, Models.Printer.TypeReceipt);
            var printer = printers.FirstOrDefault(p => p.Id == printerId);
            if (printer == null)
            {
                return BadRequest(new { success = false, message = "That printer is not available for your hospital." });
            }

            // Currency, footer and address options always come from the saved settings.
            vm.Settings = await _receiptPrint.GetSettingsAsync();
            var result = await _networkPrint.SendAsync(printer, _networkPrint.BuildReceipt(printer, vm), HttpContext.RequestAborted);
            await _audit.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "PRINT", "Receipt", vm.Highlight ?? vm.Title, null,
                $"{vm.Title} {vm.Highlight} → {printer.Name}: {result.Message}");
            return result.Success
                ? Json(new { success = true, message = result.Message })
                : StatusCode(StatusCodes.Status502BadGateway, new { success = false, message = result.Message });
        }
    }
}
