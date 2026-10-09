using System.Security.Claims;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Controllers
{
    /// <summary>
    /// Bills of items purchased: a vendor invoice is entered as a draft, submitted, approved by a different
    /// finance user (or rejected with a reason), the goods are received into stock, and payments are recorded.
    /// </summary>
    [Authorize(Roles = ViewRoles)]
    public class PurchaseBillsController : Controller
    {
        public const string ViewRoles = "SuperAdmin,Admin,Accountant,Pharmacist,Staff";
        public const string ApproveRoles = "SuperAdmin,Admin,Accountant";
        public static readonly string[] PaymentMethods = { "Bank transfer", "Cheque", "Cash", "Card", "UPI / wallet" };

        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public PurchaseBillsController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        private string UserName => User.Identity?.Name ?? string.Empty;
        private bool CanApprove => ApproveRoles.Split(',').Any(User.IsInRole);

        public async Task<IActionResult> Index(string? status, string? payment, int? vendorId, string? filter)
        {
            var today = DateTime.Today;
            var query = _context.PurchaseBills.AsNoTracking().Include(b => b.Vendor).AsQueryable();
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(b => b.Status == status);
            if (!string.IsNullOrWhiteSpace(payment)) query = query.Where(b => b.PaymentStatus == payment);
            if (vendorId.HasValue) query = query.Where(b => b.VendorId == vendorId);
            if (filter == "overdue") query = query.Where(b => (b.Status == "Approved" || b.Status == "Received") && b.PaymentStatus != "Paid" && b.DueDate < today);
            if (filter == "approval") query = query.Where(b => b.Status == "Submitted");

            var open = _context.PurchaseBills.AsNoTracking().Where(b => (b.Status == "Approved" || b.Status == "Received") && b.PaymentStatus != "Paid");
            ViewBag.Outstanding = await open.SumAsync(b => (decimal?)(b.TotalAmount - b.PaidAmount)) ?? 0;
            ViewBag.Overdue = await open.Where(b => b.DueDate < today).SumAsync(b => (decimal?)(b.TotalAmount - b.PaidAmount)) ?? 0;
            ViewBag.AwaitingApproval = await _context.PurchaseBills.CountAsync(b => b.Status == "Submitted");
            ViewBag.Vendors = await _context.Vendors.AsNoTracking().OrderBy(v => v.Name).ToListAsync();
            ViewBag.Status = status;
            ViewBag.Payment = payment;
            ViewBag.VendorId = vendorId;
            ViewBag.Filter = filter;
            return View(await query.OrderByDescending(b => b.InvoiceDate).ThenByDescending(b => b.Id).Take(500).ToListAsync());
        }

        public async Task<IActionResult> Create(int? vendorId)
        {
            ViewBag.Vendors = await _context.Vendors.AsNoTracking().Where(v => v.IsActive).OrderBy(v => v.Name).ToListAsync();
            return View(new PurchaseBill { VendorId = vendorId ?? 0, InvoiceDate = DateTime.Today });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VendorId,VendorInvoiceNumber,InvoiceDate,DueDate,Notes")] PurchaseBill input, bool useVendorTerms = true)
        {
            ModelState.Remove(nameof(PurchaseBill.Vendor));
            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == input.VendorId && v.IsActive);
            if (vendor == null) ModelState.AddModelError(nameof(input.VendorId), "Choose an active vendor.");
            input.VendorInvoiceNumber = (input.VendorInvoiceNumber ?? string.Empty).Trim();
            if (input.InvoiceDate.Date > DateTime.Today) ModelState.AddModelError(nameof(input.InvoiceDate), "The invoice date cannot be in the future.");
            if (vendor != null && await IsDuplicateInvoiceAsync(vendor.Id, input.VendorInvoiceNumber, null))
                ModelState.AddModelError(nameof(input.VendorInvoiceNumber), "This vendor invoice has already been entered.");
            if (!ModelState.IsValid)
            {
                ViewBag.Vendors = await _context.Vendors.AsNoTracking().Where(v => v.IsActive).OrderBy(v => v.Name).ToListAsync();
                return View(input);
            }

            input.DueDate = useVendorTerms || input.DueDate < input.InvoiceDate ? input.InvoiceDate.Date.AddDays(vendor!.PaymentTermsDays) : input.DueDate.Date;
            input.InvoiceDate = input.InvoiceDate.Date;
            input.BillNumber = await NextBillNumberAsync();
            input.Status = "Draft";
            input.PaymentStatus = "Unpaid";
            input.Notes = input.Notes?.Trim() ?? string.Empty;
            input.CreatedByUserId = UserId;
            input.CreatedBy = UserName;
            input.CreatedAt = DateTime.Now;
            _context.PurchaseBills.Add(input);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "CREATE", "PurchaseBill", input.Id.ToString(), null, $"{input.BillNumber} {vendor!.Name} invoice {input.VendorInvoiceNumber}");
            TempData["SuccessMessage"] = $"Purchase bill {input.BillNumber} created as a draft – add the items.";
            return RedirectToAction(nameof(Details), new { id = input.Id });
        }

        public async Task<IActionResult> Details(int id)
        {
            var bill = await _context.PurchaseBills.AsNoTracking()
                .Include(b => b.Vendor).Include(b => b.Items).ThenInclude(i => i.InventoryItem).Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (bill == null) return NotFound();
            ViewBag.Items = await _context.InventoryItems.AsNoTracking().Where(i => i.IsActive && i.HospitalId == bill.HospitalId).OrderBy(i => i.Name).ToListAsync();
            ViewBag.CanApprove = CanApprove;
            ViewBag.UserId = UserId;
            return View(bill);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLine(int id, int? inventoryItemId, string? description, decimal quantity, decimal unitCost, decimal taxPercent, string? batchNumber, DateTime? expiryDate)
        {
            var bill = await _context.PurchaseBills.Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == id);
            if (bill == null) return NotFound();

            InventoryItem? item = null;
            if (inventoryItemId.HasValue)
            {
                item = await _context.InventoryItems.FirstOrDefaultAsync(i => i.Id == inventoryItemId && i.HospitalId == bill.HospitalId);
            }

            var errors = new List<string>();
            if (bill.Status != "Draft" && bill.Status != "Rejected") errors.Add("Items can only be changed while the bill is a draft (or rejected).");
            if (inventoryItemId.HasValue && item == null) errors.Add("Choose a stock item of this hospital.");
            if (item == null && string.IsNullOrWhiteSpace(description)) errors.Add("Choose a stock item or describe the item/service.");
            if (quantity <= 0) errors.Add("Quantity must be more than 0.");
            if (unitCost < 0) errors.Add("Unit cost cannot be negative.");
            if (taxPercent < 0 || taxPercent > 100) errors.Add("Tax % must be between 0 and 100.");
            if (errors.Count > 0)
            {
                TempData["ErrorMessage"] = string.Join(" ", errors);
                return RedirectToAction(nameof(Details), new { id });
            }

            bill.Items.Add(new PurchaseBillItem
            {
                InventoryItemId = item?.Id,
                Description = string.IsNullOrWhiteSpace(description) ? item!.Name : description.Trim(),
                Quantity = quantity,
                UnitCost = unitCost,
                TaxPercent = taxPercent,
                LineTotal = Math.Round(quantity * unitCost * (1 + taxPercent / 100m), 2),
                BatchNumber = batchNumber?.Trim() ?? string.Empty,
                ExpiryDate = expiryDate?.Date
            });
            Recalculate(bill);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Item added.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLine(int id, int lineId)
        {
            var bill = await _context.PurchaseBills.Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == id);
            if (bill == null) return NotFound();
            var line = bill.Items.FirstOrDefault(i => i.Id == lineId);
            if (line == null || (bill.Status != "Draft" && bill.Status != "Rejected"))
            {
                TempData["ErrorMessage"] = "Items can only be changed while the bill is a draft (or rejected).";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.PurchaseBillItems.Remove(line);
            bill.Items.Remove(line);
            Recalculate(bill);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Item removed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            var bill = await _context.PurchaseBills.Include(b => b.Items).Include(b => b.Vendor).FirstOrDefaultAsync(b => b.Id == id);
            if (bill == null) return NotFound();
            if ((bill.Status != "Draft" && bill.Status != "Rejected") || bill.Items.Count == 0 || !bill.Vendor.IsActive)
            {
                TempData["ErrorMessage"] = bill.Items.Count == 0 ? "Add at least one item before submitting." : !bill.Vendor.IsActive ? "The vendor is inactive." : "Only a draft or rejected bill can be submitted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            bill.Status = "Submitted";
            bill.SubmittedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "SUBMIT", "PurchaseBill", id.ToString(), null, $"{bill.BillNumber} total {bill.TotalAmount:0.00}");
            TempData["SuccessMessage"] = $"{bill.BillNumber} submitted for approval.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Approve or reject a submitted bill; the person who entered it cannot decide on it.</summary>
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = ApproveRoles)]
        public async Task<IActionResult> Decide(int id, bool approve, string? comments)
        {
            var bill = await _context.PurchaseBills.FirstOrDefaultAsync(b => b.Id == id);
            if (bill == null) return NotFound();
            string? error = bill.Status != "Submitted" ? "Only a submitted bill can be approved or rejected."
                : bill.CreatedByUserId == UserId ? "You entered this bill, so another approver must decide on it."
                : !approve && string.IsNullOrWhiteSpace(comments) ? "Give a reason when rejecting." : null;
            if (error != null)
            {
                TempData["ErrorMessage"] = error;
                return RedirectToAction(nameof(Details), new { id });
            }

            bill.Status = approve ? "Approved" : "Rejected";
            bill.ApprovedBy = UserName;
            bill.ApprovedAt = DateTime.Now;
            bill.ApprovalComments = comments?.Trim() ?? string.Empty;
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, approve ? "APPROVE" : "REJECT", "PurchaseBill", id.ToString(), null, $"{bill.BillNumber}: {bill.ApprovalComments}");
            TempData["SuccessMessage"] = approve ? $"{bill.BillNumber} approved." : $"{bill.BillNumber} rejected – it can be corrected and submitted again.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Goods received: stock lines are added to inventory (IN transactions) at the purchase cost.</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Receive(int id)
        {
            var bill = await _context.PurchaseBills.Include(b => b.Items).ThenInclude(i => i.InventoryItem).FirstOrDefaultAsync(b => b.Id == id);
            if (bill == null) return NotFound();
            if (bill.Status != "Approved")
            {
                TempData["ErrorMessage"] = "Goods can only be received for an approved bill.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var received = 0;
            foreach (var line in bill.Items.Where(i => i.InventoryItem != null))
            {
                var item = line.InventoryItem!;
                item.CurrentStock += line.Quantity;
                item.UnitCost = line.UnitCost;
                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    InventoryItemId = item.Id,
                    TransactionType = "IN",
                    Quantity = line.Quantity,
                    UnitCost = line.UnitCost,
                    ReferenceNumber = $"{bill.BillNumber} / {bill.VendorInvoiceNumber}",
                    Remarks = string.IsNullOrEmpty(line.BatchNumber) ? "Received from purchase bill" : $"Batch {line.BatchNumber}" + (line.ExpiryDate.HasValue ? $", expiry {line.ExpiryDate:yyyy-MM-dd}" : string.Empty),
                    PerformedByUserId = UserId,
                    TransactionDate = DateTime.Now,
                    PurchaseBillId = bill.Id
                });
                received++;
            }

            bill.Status = "Received";
            bill.ReceivedBy = UserName;
            bill.ReceivedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "RECEIVE", "PurchaseBill", id.ToString(), null, $"{bill.BillNumber}: {received} stock line(s) added to inventory");
            TempData["SuccessMessage"] = $"Goods received – {received} stock item(s) updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = ApproveRoles)]
        public async Task<IActionResult> AddPayment(int id, decimal amount, DateTime? paymentDate, string method, string? reference, string? notes)
        {
            var bill = await _context.PurchaseBills.Include(b => b.Payments).FirstOrDefaultAsync(b => b.Id == id);
            if (bill == null) return NotFound();
            var balance = bill.TotalAmount - bill.PaidAmount;
            string? error = bill.Status != "Approved" && bill.Status != "Received" ? "Payments can be recorded only for approved bills."
                : amount <= 0 ? "Enter the amount paid."
                : amount > balance ? $"The amount is more than the balance ({balance:0.00})."
                : !PaymentMethods.Contains(method) ? "Choose the payment method."
                : !paymentDate.HasValue || paymentDate.Value.Date > DateTime.Today ? "Enter the payment date (not in the future)." : null;
            if (error != null)
            {
                TempData["ErrorMessage"] = error;
                return RedirectToAction(nameof(Details), new { id });
            }

            bill.Payments.Add(new VendorPayment { Amount = amount, PaymentDate = paymentDate!.Value.Date, Method = method, Reference = reference?.Trim() ?? string.Empty, Notes = notes?.Trim() ?? string.Empty, CreatedBy = UserName, CreatedAt = DateTime.Now });
            bill.PaidAmount += amount;
            bill.PaymentStatus = bill.PaidAmount >= bill.TotalAmount ? "Paid" : "Partially paid";
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "PAYMENT", "PurchaseBill", id.ToString(), null, $"{bill.BillNumber}: {amount:0.00} by {method} {reference}");
            TempData["SuccessMessage"] = $"Payment of {amount:0.00} recorded – {bill.PaymentStatus.ToLowerInvariant()}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var bill = await _context.PurchaseBills.FirstOrDefaultAsync(b => b.Id == id);
            if (bill == null) return NotFound();
            if (bill.Status is not ("Draft" or "Submitted" or "Rejected") || string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(reason) ? "Give a reason for cancelling." : "Approved or received bills cannot be cancelled.";
                return RedirectToAction(nameof(Details), new { id });
            }

            bill.Status = "Cancelled";
            bill.CancelReason = reason.Trim();
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "CANCEL", "PurchaseBill", id.ToString(), null, $"{bill.BillNumber}: {bill.CancelReason}");
            TempData["SuccessMessage"] = $"{bill.BillNumber} cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private static void Recalculate(PurchaseBill bill)
        {
            bill.SubTotal = Math.Round(bill.Items.Sum(i => i.Quantity * i.UnitCost), 2);
            bill.TotalAmount = Math.Round(bill.Items.Sum(i => i.LineTotal), 2);
            bill.TaxAmount = bill.TotalAmount - bill.SubTotal;
        }

        private async Task<bool> IsDuplicateInvoiceAsync(int vendorId, string invoiceNumber, int? excludeId) =>
            await _context.PurchaseBills.IgnoreQueryFilters().AnyAsync(b => b.VendorId == vendorId && b.VendorInvoiceNumber == invoiceNumber && b.Status != "Cancelled" && b.Id != excludeId);

        private async Task<string> NextBillNumberAsync()
        {
            var prefix = $"PB-{DateTime.Now:yyyy}-";
            var numbers = await _context.PurchaseBills.IgnoreQueryFilters().Where(b => b.BillNumber.StartsWith(prefix)).Select(b => b.BillNumber).ToListAsync();
            var max = numbers.Select(n => int.TryParse(n[prefix.Length..], out var x) ? x : 0).DefaultIfEmpty(0).Max();
            return $"{prefix}{max + 1:D4}";
        }
    }
}
