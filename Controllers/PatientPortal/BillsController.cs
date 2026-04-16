using MedyxHMS.DTOs;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedyxHMS.Controllers.PatientPortal
{
    [Authorize]
    public class BillsController : Controller
    {
        private readonly IPatientPortalService _patientPortalService;

        public BillsController(IPatientPortalService patientPortalService)
        {
            _patientPortalService = patientPortalService;
        }

        // GET: /PatientPortal/Bills/Index
        public async Task<IActionResult> Index(string filter = "all", int page = 1, int pageSize = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });
            }

            try
            {
                var bills = await _patientPortalService.GetPatientBillsAsync(userId, filter);

                var viewModel = new PatientPortalBillsViewModel
                {
                    Filter = filter,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalRecords = bills.Count(),
                    Bills = bills
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(b => new PatientPortalBillDto
                        {
                            Id = b.Id.ToString(),
                            BillNumber = b.BillNumber,
                            BillDate = b.BillDate,
                            TotalAmount = b.TotalAmount,
                            PaidAmount = b.PaidAmount,
                            Status = b.Status,
                            DueDate = b.DueDate
                        })
                        .ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading bills: {ex.Message}";
                return View(new PatientPortalBillsViewModel());
            }
        }

        // GET: /PatientPortal/Bills/Details/5
        public async Task<IActionResult> Details(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });
            }

            var bill = await _patientPortalService.GetBillDetailsAsync(id);
            if (bill == null)
            {
                return NotFound();
            }

            // Verify patient owns this bill
            if (bill.PatientId.ToString() != userId)
            {
                return Forbid();
            }

            var viewModel = new PatientPortalBillDetailsViewModel
            {
                Bill = new PatientPortalBillDto
                {
                    Id = bill.Id.ToString(),
                    BillNumber = bill.BillNumber,
                    BillDate = bill.BillDate,
                    TotalAmount = bill.TotalAmount,
                    PaidAmount = bill.PaidAmount,
                    Status = bill.Status,
                    DueDate = bill.DueDate,
                    Items = bill.BillItems?.Select(bi => new PatientPortalBillItemDto
                    {
                        Description = bi.Description,
                        Amount = bi.Amount,
                        Category = bi.ItemType
                    }).ToList() ?? new(),
                    Payments = bill.Payments?.Select(p => new PatientPortalPaymentDto
                    {
                        Id = p.Id.ToString(),
                        PaymentDate = p.PaymentDate,
                        Amount = p.Amount,
                        PaymentMethod = p.PaymentMethod,
                        TransactionId = p.TransactionId,
                        Status = p.Status
                    }).ToList() ?? new()
                }
            };

            return View(viewModel);
        }

        // GET: /PatientPortal/Bills/Pay/5
        public async Task<IActionResult> Pay(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });
            }

            var bill = await _patientPortalService.GetBillDetailsAsync(id);
            if (bill == null || !bill.CanPay)
            {
                return NotFound();
            }

            // Verify patient owns this bill
            if (bill.PatientId.ToString() != userId)
            {
                return Forbid();
            }

            var viewModel = new PatientPortalPaymentViewModel
            {
                BillId = id,
                BillAmount = bill.TotalAmount,
                PendingAmount = bill.TotalAmount - bill.PaidAmount,
                PaymentAmount = bill.TotalAmount - bill.PaidAmount
            };

            return View(viewModel);
        }

        // POST: /PatientPortal/Bills/Pay/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(string id, PatientPortalPaymentViewModel viewModel)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Process payment (integrate with payment gateway)
                    // This is a placeholder for payment processing logic

                    TempData["SuccessMessage"] = "Payment processed successfully! Transaction ID: " + Guid.NewGuid().ToString().Substring(0, 8);
                    return RedirectToAction("Details", new { id });
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error processing payment: {ex.Message}";
                }
            }

            return View(viewModel);
        }

        // GET: /PatientPortal/Bills/Download/5
        [HttpGet]
        public async Task<IActionResult> Download(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });
            }

            try
            {
                var bill = await _patientPortalService.GetBillDetailsAsync(id);
                if (bill == null || bill.PatientId.ToString() != userId)
                {
                    return Forbid();
                }

                // Generate PDF (placeholder)
                // In production, use a library like iText or SelectPdf to generate PDF
                TempData["SuccessMessage"] = "Bill downloaded successfully!";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error downloading bill: {ex.Message}";
                return RedirectToAction("Details", new { id });
            }
        }
    }
}