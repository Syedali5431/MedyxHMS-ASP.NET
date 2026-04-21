using MedyxHMS.Data;
using MedyxHMS.DTOs;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Staff,Accountant")] // adjust roles as needed
    public class BillingController : Controller
    {
        private readonly IBillingService _billingService;
        private readonly IExportService _exportService;
        private readonly ApplicationDbContext _db;

        public BillingController(IBillingService billingService, IExportService exportService, ApplicationDbContext db)
        {
            _billingService = billingService;
            _exportService = exportService;
            _db = db;
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

        // ── Payment Gateway Settings ────────────────────────────────────────

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet]
        public async Task<IActionResult> GatewaySettings()
        {
            var vm = new PaymentGatewaySettingsViewModel
            {
                ActiveGateway             = await GetGwSetting("Payment:ActiveGateway") ?? "none",
                PayPalClientId            = await GetGwSetting("Payment:PayPal:ClientId") ?? string.Empty,
                PayPalTestMode            = (await GetGwSetting("Payment:PayPal:TestMode")) != "false",
                HasSavedPayPalClientSecret = !string.IsNullOrWhiteSpace(await GetGwSetting("Payment:PayPal:ClientSecret")),
                StripePublishableKey      = await GetGwSetting("Payment:Stripe:PublishableKey") ?? string.Empty,
                StripeTestMode            = (await GetGwSetting("Payment:Stripe:TestMode")) != "false",
                HasSavedStripeSecretKey   = !string.IsNullOrWhiteSpace(await GetGwSetting("Payment:Stripe:SecretKey")),
                PayUMerchantKey           = await GetGwSetting("Payment:PayU:MerchantKey") ?? string.Empty,
                HasSavedPayUSalt          = !string.IsNullOrWhiteSpace(await GetGwSetting("Payment:PayU:Salt")),
                CCAvenueMerchantId        = await GetGwSetting("Payment:CCAvenue:MerchantId") ?? string.Empty,
                CCAvenueAccessCode        = await GetGwSetting("Payment:CCAvenue:AccessCode") ?? string.Empty,
                HasSavedCCAvenueWorkingKey = !string.IsNullOrWhiteSpace(await GetGwSetting("Payment:CCAvenue:WorkingKey")),
                InstamojoApiKey           = await GetGwSetting("Payment:Instamojo:ApiKey") ?? string.Empty,
                InstamojoTestMode         = (await GetGwSetting("Payment:Instamojo:TestMode")) != "false",
                HasSavedInstamojoAuthToken = !string.IsNullOrWhiteSpace(await GetGwSetting("Payment:Instamojo:AuthToken")),
                PaystackPublicKey         = await GetGwSetting("Payment:Paystack:PublicKey") ?? string.Empty,
                HasSavedPaystackSecretKey = !string.IsNullOrWhiteSpace(await GetGwSetting("Payment:Paystack:SecretKey")),
                RazorpayKeyId             = await GetGwSetting("Payment:Razorpay:KeyId") ?? string.Empty,
                RazorpayTestMode          = (await GetGwSetting("Payment:Razorpay:TestMode")) != "false",
                HasSavedRazorpayKeySecret = !string.IsNullOrWhiteSpace(await GetGwSetting("Payment:Razorpay:KeySecret")),
                PaytmMerchantId           = await GetGwSetting("Payment:Paytm:MerchantId") ?? string.Empty,
                PaytmTestMode             = (await GetGwSetting("Payment:Paytm:TestMode")) != "false",
                HasSavedPaytmMerchantKey  = !string.IsNullOrWhiteSpace(await GetGwSetting("Payment:Paytm:MerchantKey")),
                MidtransClientKey         = await GetGwSetting("Payment:Midtrans:ClientKey") ?? string.Empty,
                MidtransTestMode          = (await GetGwSetting("Payment:Midtrans:TestMode")) != "false",
                HasSavedMidtransServerKey = !string.IsNullOrWhiteSpace(await GetGwSetting("Payment:Midtrans:ServerKey")),
                PesapalConsumerKey        = await GetGwSetting("Payment:Pesapal:ConsumerKey") ?? string.Empty,
                PesapalTestMode           = (await GetGwSetting("Payment:Pesapal:TestMode")) != "false",
                HasSavedPesapalConsumerSecret = !string.IsNullOrWhiteSpace(await GetGwSetting("Payment:Pesapal:ConsumerSecret")),
                FlutterwavePublicKey      = await GetGwSetting("Payment:Flutterwave:PublicKey") ?? string.Empty,
                FlutterwaveTestMode       = (await GetGwSetting("Payment:Flutterwave:TestMode")) != "false",
                HasSavedFlutterwaveSecretKey = !string.IsNullOrWhiteSpace(await GetGwSetting("Payment:Flutterwave:SecretKey")),
                IPayAfricaMerchantId      = await GetGwSetting("Payment:IPayAfrica:MerchantId") ?? string.Empty,
                HasSavedIPayAfricaHashKey = !string.IsNullOrWhiteSpace(await GetGwSetting("Payment:IPayAfrica:HashKey")),
                JazzCashMerchantId        = await GetGwSetting("Payment:JazzCash:MerchantId") ?? string.Empty,
                JazzCashTestMode          = (await GetGwSetting("Payment:JazzCash:TestMode")) != "false",
                HasSavedJazzCashPassword  = !string.IsNullOrWhiteSpace(await GetGwSetting("Payment:JazzCash:Password")),
                BillplzCollectionId       = await GetGwSetting("Payment:Billplz:CollectionId") ?? string.Empty,
                HasSavedBillplzApiKey     = !string.IsNullOrWhiteSpace(await GetGwSetting("Payment:Billplz:ApiKey")),
                SSLCommerzStoreId         = await GetGwSetting("Payment:SSLCommerz:StoreId") ?? string.Empty,
                SSLCommerzTestMode        = (await GetGwSetting("Payment:SSLCommerz:TestMode")) != "false",
                HasSavedSSLCommerzStorePassword = !string.IsNullOrWhiteSpace(await GetGwSetting("Payment:SSLCommerz:StorePassword")),
                WalkingmClientId          = await GetGwSetting("Payment:Walkingm:ClientId") ?? string.Empty,
                HasSavedWalkingmClientSecret = !string.IsNullOrWhiteSpace(await GetGwSetting("Payment:Walkingm:ClientSecret")),
                EasyPaisaMerchantId       = await GetGwSetting("Payment:EasyPaisa:MerchantId") ?? string.Empty,
                EasyPaisaTestMode         = (await GetGwSetting("Payment:EasyPaisa:TestMode")) != "false",
                HasSavedEasyPaisaHashKey  = !string.IsNullOrWhiteSpace(await GetGwSetting("Payment:EasyPaisa:HashKey")),
            };
            return View(vm);
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GatewaySettings(PaymentGatewaySettingsViewModel vm)
        {
            await UpsertGwSetting("Payment:ActiveGateway", vm.ActiveGateway ?? "none");

            // PayPal
            await UpsertGwSetting("Payment:PayPal:ClientId", vm.PayPalClientId ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(vm.PayPalClientSecret))
                await UpsertGwSetting("Payment:PayPal:ClientSecret", vm.PayPalClientSecret);
            await UpsertGwSetting("Payment:PayPal:TestMode", vm.PayPalTestMode ? "true" : "false");

            // Stripe
            await UpsertGwSetting("Payment:Stripe:PublishableKey", vm.StripePublishableKey ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(vm.StripeSecretKey))
                await UpsertGwSetting("Payment:Stripe:SecretKey", vm.StripeSecretKey);
            await UpsertGwSetting("Payment:Stripe:TestMode", vm.StripeTestMode ? "true" : "false");

            // PayU
            await UpsertGwSetting("Payment:PayU:MerchantKey", vm.PayUMerchantKey ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(vm.PayUSalt))
                await UpsertGwSetting("Payment:PayU:Salt", vm.PayUSalt);

            // CCAvenue
            await UpsertGwSetting("Payment:CCAvenue:MerchantId", vm.CCAvenueMerchantId ?? string.Empty);
            await UpsertGwSetting("Payment:CCAvenue:AccessCode", vm.CCAvenueAccessCode ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(vm.CCAvenueWorkingKey))
                await UpsertGwSetting("Payment:CCAvenue:WorkingKey", vm.CCAvenueWorkingKey);

            // Instamojo
            await UpsertGwSetting("Payment:Instamojo:ApiKey", vm.InstamojoApiKey ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(vm.InstamojoAuthToken))
                await UpsertGwSetting("Payment:Instamojo:AuthToken", vm.InstamojoAuthToken);
            await UpsertGwSetting("Payment:Instamojo:TestMode", vm.InstamojoTestMode ? "true" : "false");

            // Paystack
            await UpsertGwSetting("Payment:Paystack:PublicKey", vm.PaystackPublicKey ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(vm.PaystackSecretKey))
                await UpsertGwSetting("Payment:Paystack:SecretKey", vm.PaystackSecretKey);

            // Razorpay
            await UpsertGwSetting("Payment:Razorpay:KeyId", vm.RazorpayKeyId ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(vm.RazorpayKeySecret))
                await UpsertGwSetting("Payment:Razorpay:KeySecret", vm.RazorpayKeySecret);
            await UpsertGwSetting("Payment:Razorpay:TestMode", vm.RazorpayTestMode ? "true" : "false");

            // Paytm
            await UpsertGwSetting("Payment:Paytm:MerchantId", vm.PaytmMerchantId ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(vm.PaytmMerchantKey))
                await UpsertGwSetting("Payment:Paytm:MerchantKey", vm.PaytmMerchantKey);
            await UpsertGwSetting("Payment:Paytm:TestMode", vm.PaytmTestMode ? "true" : "false");

            // Midtrans
            await UpsertGwSetting("Payment:Midtrans:ClientKey", vm.MidtransClientKey ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(vm.MidtransServerKey))
                await UpsertGwSetting("Payment:Midtrans:ServerKey", vm.MidtransServerKey);
            await UpsertGwSetting("Payment:Midtrans:TestMode", vm.MidtransTestMode ? "true" : "false");

            // Pesapal
            await UpsertGwSetting("Payment:Pesapal:ConsumerKey", vm.PesapalConsumerKey ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(vm.PesapalConsumerSecret))
                await UpsertGwSetting("Payment:Pesapal:ConsumerSecret", vm.PesapalConsumerSecret);
            await UpsertGwSetting("Payment:Pesapal:TestMode", vm.PesapalTestMode ? "true" : "false");

            // Flutterwave
            await UpsertGwSetting("Payment:Flutterwave:PublicKey", vm.FlutterwavePublicKey ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(vm.FlutterwaveSecretKey))
                await UpsertGwSetting("Payment:Flutterwave:SecretKey", vm.FlutterwaveSecretKey);
            await UpsertGwSetting("Payment:Flutterwave:TestMode", vm.FlutterwaveTestMode ? "true" : "false");

            // iPay Africa
            await UpsertGwSetting("Payment:IPayAfrica:MerchantId", vm.IPayAfricaMerchantId ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(vm.IPayAfricaHashKey))
                await UpsertGwSetting("Payment:IPayAfrica:HashKey", vm.IPayAfricaHashKey);

            // JazzCash
            await UpsertGwSetting("Payment:JazzCash:MerchantId", vm.JazzCashMerchantId ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(vm.JazzCashPassword))
                await UpsertGwSetting("Payment:JazzCash:Password", vm.JazzCashPassword);
            await UpsertGwSetting("Payment:JazzCash:TestMode", vm.JazzCashTestMode ? "true" : "false");

            // Billplz
            await UpsertGwSetting("Payment:Billplz:CollectionId", vm.BillplzCollectionId ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(vm.BillplzApiKey))
                await UpsertGwSetting("Payment:Billplz:ApiKey", vm.BillplzApiKey);

            // SSLCommerz
            await UpsertGwSetting("Payment:SSLCommerz:StoreId", vm.SSLCommerzStoreId ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(vm.SSLCommerzStorePassword))
                await UpsertGwSetting("Payment:SSLCommerz:StorePassword", vm.SSLCommerzStorePassword);
            await UpsertGwSetting("Payment:SSLCommerz:TestMode", vm.SSLCommerzTestMode ? "true" : "false");

            // Walkingm
            await UpsertGwSetting("Payment:Walkingm:ClientId", vm.WalkingmClientId ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(vm.WalkingmClientSecret))
                await UpsertGwSetting("Payment:Walkingm:ClientSecret", vm.WalkingmClientSecret);

            // EasyPaisa
            await UpsertGwSetting("Payment:EasyPaisa:MerchantId", vm.EasyPaisaMerchantId ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(vm.EasyPaisaHashKey))
                await UpsertGwSetting("Payment:EasyPaisa:HashKey", vm.EasyPaisaHashKey);
            await UpsertGwSetting("Payment:EasyPaisa:TestMode", vm.EasyPaisaTestMode ? "true" : "false");

            TempData["SuccessMessage"] = "Payment gateway settings saved successfully.";
            return RedirectToAction(nameof(GatewaySettings));
        }

        private async Task<string?> GetGwSetting(string key)
        {
            var s = await _db.Settings.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key);
            return s?.Value;
        }

        private async Task UpsertGwSetting(string key, string value)
        {
            var s = await _db.Settings.FirstOrDefaultAsync(x => x.Key == key);
            if (s == null)
            {
                _db.Settings.Add(new Setting
                {
                    Key = key, Value = value, Type = "string",
                    Category = "Payment", Description = $"Payment gateway setting: {key}",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedBy = User?.Identity?.Name ?? string.Empty
                });
            }
            else
            {
                s.Value = value;
                s.ModifiedDate = DateTime.UtcNow;
                s.ModifiedBy = User?.Identity?.Name ?? string.Empty;
            }
            await _db.SaveChangesAsync();
        }
    }
}
