using MedyxHMS.DTOs;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Staff,Accountant")] // adjust roles as needed
    public class BillingController : Controller
    {
        private readonly IBillingService _billingService;
        private readonly IExportService _exportService;

        public BillingController(IBillingService billingService, IExportService exportService)
        {
            _billingService = billingService;
            _exportService = exportService;
        }

        public async Task<IActionResult> Index(string filter = "all", int page = 1, int pageSize = 10)
        {
            var bills = await _billingService.GetAllBillsAsync();
            if (!string.IsNullOrEmpty(filter) && filter != "all")
            {
                bills = bills.Where(b => string.Equals(b.Status, filter, StringComparison.OrdinalIgnoreCase));
            }

            var billList = bills
                .OrderByDescending(b => b.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new BillViewModel
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalRecords = bills.Count(),
                Filter = filter,
                Bills = billList.Select(b => new DTOs.BillDto
                {
                    Id = b.Id.ToString(),
                    BillNumber = b.BillNumber,
                    PatientId = b.PatientId,
                    PatientName = b.Patient != null ? $"{b.Patient.FirstName} {b.Patient.LastName}" : "Unknown",
                    AppointmentId = b.AppointmentId,
                    BillDate = b.BillDate,
                    DueDate = b.DueDate,
                    TotalAmount = b.TotalAmount,
                    PaidAmount = b.PaidAmount,
                    Status = b.Status,
                    Notes = b.Notes,
                    CreatedDate = b.CreatedDate,
                    UpdatedDate = b.UpdatedDate,
                    Items = b.BillItems?.Select(i => new DTOs.BillItemDto
                    {
                        Id = i.Id.ToString(),
                        BillId = i.BillId.ToString(),
                        Description = i.ItemName,
                        ItemType = i.ItemType,
                        Quantity = (int)i.Quantity,
                        UnitPrice = i.UnitPrice,
                        Amount = i.TotalPrice,
                        Category = i.ItemType,
                        CreatedDate = i.CreatedDate
                    }).ToList() ?? new List<DTOs.BillItemDto>(),
                    Payments = b.Payments?.Select(p => new DTOs.PaymentDto
                    {
                        Id = p.Id.ToString(),
                        BillId = p.BillId.ToString(),
                        PatientId = b.PatientId,
                        Amount = p.Amount,
                        PaymentDate = p.PaymentDate,
                        PaymentMethod = p.PaymentMethod,
                        TransactionId = p.TransactionId,
                        GatewayReference = p.PaymentGateway,
                        Status = p.Status,
                        Notes = p.Notes,
                        CreatedDate = p.PaymentDate
                    }).ToList() ?? new List<DTOs.PaymentDto>()
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Export(string format = "csv", string filter = "all")
        {
            format = (format ?? "csv").Trim().ToLowerInvariant();
            if (format != "csv" && format != "pdf")
                return BadRequest("Only CSV and PDF exports are supported.");

            var bills = await _billingService.GetAllBillsAsync();
            if (!string.IsNullOrEmpty(filter) && filter != "all")
                bills = bills.Where(b => string.Equals(b.Status, filter, StringComparison.OrdinalIgnoreCase));

            var headers = new[] { "Bill #", "Patient", "Bill Date", "Due Date", "Total", "Paid", "Pending", "Status" };
            var rows = bills
                .OrderByDescending(b => b.CreatedDate)
                .Select(b => (IReadOnlyList<string>)new[]
                {
                    b.BillNumber ?? string.Empty,
                    b.Patient != null ? (b.Patient.FirstName + " " + b.Patient.LastName).Trim() : "Unknown",
                    b.BillDate.ToString("yyyy-MM-dd"),
                    b.DueDate.ToString("yyyy-MM-dd"),
                    b.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture),
                    b.PaidAmount.ToString("0.00", CultureInfo.InvariantCulture),
                    (b.TotalAmount - b.PaidAmount).ToString("0.00", CultureInfo.InvariantCulture),
                    b.Status ?? string.Empty
                }).ToList();

            var title = "Billing Overview Export";
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            if (format == "csv")
            {
                var bytes = _exportService.BuildCsv(title, headers, rows);
                return File(bytes, "text/csv", $"billing_{stamp}.csv");
            }

            var pdfBytes = _exportService.BuildPdfTable(title, headers, rows);
            return File(pdfBytes, "application/pdf", $"billing_{stamp}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadReceipt(int id)
        {
            var bill = await _billingService.GetBillByIdAsync(id);
            if (bill == null)
                return NotFound();

            var headers = new[] { "Field", "Value" };
            var rows = new List<IReadOnlyList<string>>
            {
                new [] { "Bill Number", bill.BillNumber ?? string.Empty },
                new [] { "Patient", bill.Patient != null ? (bill.Patient.FirstName + " " + bill.Patient.LastName).Trim() : "Unknown" },
                new [] { "Bill Date", bill.BillDate.ToString("yyyy-MM-dd") },
                new [] { "Due Date", bill.DueDate.ToString("yyyy-MM-dd") },
                new [] { "Total Amount", bill.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture) },
                new [] { "Paid Amount", bill.PaidAmount.ToString("0.00", CultureInfo.InvariantCulture) },
                new [] { "Pending Amount", bill.PendingAmount.ToString("0.00", CultureInfo.InvariantCulture) },
                new [] { "Status", bill.Status ?? string.Empty }
            };

            var pdfBytes = _exportService.BuildPdfTable("Billing Receipt", headers, rows);
            var safeBillNumber = string.IsNullOrWhiteSpace(bill.BillNumber) ? bill.Id.ToString(CultureInfo.InvariantCulture) : bill.BillNumber;
            return File(pdfBytes, "application/pdf", $"receipt_{safeBillNumber}.pdf");
        }

        public async Task<IActionResult> Details(int id)
        {
            var bill = await _billingService.GetBillByIdAsync(id);
            if (bill == null)
                return NotFound();

            var viewModel = new BillDetailsViewModel
            {
                Bill = new DTOs.BillDto
                {
                    Id = bill.Id.ToString(),
                    BillNumber = bill.BillNumber,
                    PatientId = bill.PatientId,
                    PatientName = bill.Patient != null ? $"{bill.Patient.FirstName} {bill.Patient.LastName}" : "Unknown",
                    AppointmentId = bill.AppointmentId,
                    BillDate = bill.BillDate,
                    DueDate = bill.DueDate,
                    TotalAmount = bill.TotalAmount,
                    PaidAmount = bill.PaidAmount,
                    Status = bill.Status,
                    Notes = bill.Notes,
                    CreatedDate = bill.CreatedDate,
                    UpdatedDate = bill.UpdatedDate,
                    Items = bill.BillItems?.Select(i => new DTOs.BillItemDto
                    {
                        Id = i.Id.ToString(),
                        BillId = i.BillId.ToString(),
                        Description = i.ItemName,
                        ItemType = i.ItemType,
                        Quantity = (int)i.Quantity,
                        UnitPrice = i.UnitPrice,
                        Amount = i.TotalPrice,
                        Category = i.ItemType,
                        CreatedDate = i.CreatedDate
                    }).ToList() ?? new List<DTOs.BillItemDto>(),
                    Payments = bill.Payments?.Select(p => new DTOs.PaymentDto
                    {
                        Id = p.Id.ToString(),
                        BillId = p.BillId.ToString(),
                        PatientId = bill.PatientId,
                        Amount = p.Amount,
                        PaymentDate = p.PaymentDate,
                        PaymentMethod = p.PaymentMethod,
                        TransactionId = p.TransactionId,
                        GatewayReference = p.PaymentGateway,
                        Status = p.Status,
                        Notes = p.Notes,
                        CreatedDate = p.PaymentDate
                    }).ToList() ?? new List<DTOs.PaymentDto>()
                },
                Patient = bill.Patient != null ? new DTOs.PatientPortalDto
                {
                    Id = bill.Patient.Id.ToString(),
                    PatientId = bill.Patient.PatientId,
                    FirstName = bill.Patient.FirstName,
                    LastName = bill.Patient.LastName,
                    Email = bill.Patient.Email,
                    Phone = bill.Patient.Phone
                } : null,
                PaymentHistory = bill.Payments?.Select(p => new DTOs.PaymentDto
                {
                    Id = p.Id.ToString(),
                    BillId = p.BillId.ToString(),
                    PatientId = bill.PatientId,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    PaymentMethod = p.PaymentMethod,
                    TransactionId = p.TransactionId,
                    GatewayReference = p.PaymentGateway,
                    Status = p.Status,
                    Notes = p.Notes,
                    CreatedDate = p.PaymentDate
                }).ToList() ?? new List<DTOs.PaymentDto>()
            };

            return View(viewModel);
        }

        public IActionResult Create()
        {
            var viewModel = new CreateBillViewModel();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBillViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var bill = new Bill
            {
                PatientId = viewModel.Bill.PatientId,
                BillDate = viewModel.Bill.BillDate,
                DueDate = viewModel.Bill.DueDate ?? viewModel.Bill.BillDate.AddDays(30),
                AppointmentId = viewModel.Bill.AppointmentId,
                Notes = viewModel.Bill.Notes,
                TotalAmount = viewModel.LineItems.Sum(i => i.Amount),
                PaidAmount = 0,
                PendingAmount = viewModel.LineItems.Sum(i => i.Amount),
                Status = "Unpaid",
                CreatedDate = DateTime.UtcNow,
                BillItems = viewModel.LineItems.Select(i => new BillItem
                {
                    ItemName = i.Description,
                    ItemType = i.ItemType,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.Amount,
                    Description = i.Description,
                    CreatedDate = DateTime.UtcNow
                }).ToList()
            };

            var createdBill = await _billingService.CreateBillAsync(bill);
            if (createdBill == null)
            {
                ModelState.AddModelError(string.Empty, "Unable to create bill. Please try again.");
                return View(viewModel);
            }

            return RedirectToAction(nameof(Details), new { id = createdBill.Id });
        }

        public async Task<IActionResult> Pay(int id)
        {
            var bill = await _billingService.GetBillByIdAsync(id);
            if (bill == null)
                return NotFound();

            var viewModel = new PaymentViewModel
            {
                BillId = bill.Id.ToString(),
                BillAmount = bill.TotalAmount,
                PendingAmount = bill.PendingAmount,
                Amount = bill.PendingAmount,
                BillNumber = bill.BillNumber,
                PatientName = bill.Patient != null ? $"{bill.Patient.FirstName} {bill.Patient.LastName}" : "Unknown"
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(PaymentViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            if (!int.TryParse(viewModel.BillId, out var billId))
            {
                ModelState.AddModelError(string.Empty, "Invalid bill identifier.");
                return View(viewModel);
            }

            var payment = new Payment
            {
                BillId = billId,
                Amount = viewModel.Amount,
                PaymentMethod = viewModel.PaymentMethod,
                TransactionId = Guid.NewGuid().ToString("N").ToUpperInvariant(),
                PaymentGateway = "Internal",
                Status = "Completed",
                Notes = viewModel.Notes,
                PaymentDate = DateTime.UtcNow,
                ProcessedBy = User.Identity?.Name ?? "System"
            };

            var success = await _billingService.ProcessPaymentAsync(payment);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Payment processing failed. Please try again.");
                return View(viewModel);
            }

            return RedirectToAction(nameof(Details), new { id = billId });
        }
    }
}
