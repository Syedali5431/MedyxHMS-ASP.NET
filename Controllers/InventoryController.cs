using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MedyxHMS.Controllers
{
    // Staff only: the patient-portal role must not reach this staff area (it exposes other patients' data).
    [Authorize(Roles = AppRoles.Staff)]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public InventoryController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // ── Items ─────────────────────────────────────────────────

        public async Task<IActionResult> Index(string? search, string? category)
        {
            var query = _context.InventoryItems.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(i => i.Name.Contains(search) || i.ItemCode.Contains(search));
            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(i => i.Category == category);

            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.LowStock = await _context.InventoryItems
                .CountAsync(i => i.CurrentStock <= i.ReorderLevel && i.IsActive);

            var items = await query.OrderBy(i => i.Name).ToListAsync();
            return View(items);
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Create()
        {
            await LoadVendorsAsync(null);
            return View(new InventoryItem());
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Create(InventoryItem model)
        {
            await ApplyVendorAsync(model, null);
            if (!ModelState.IsValid)
            {
                await LoadVendorsAsync(model.VendorId);
                return View(model);
            }

            _context.InventoryItems.Add(model);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier),
                "CREATE", "InventoryItem", model.Id.ToString(), null, model.Name);
            TempData["SuccessMessage"] = "Item added to inventory.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null) return NotFound();
            await LoadVendorsAsync(item.VendorId);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Edit(int id, InventoryItem model)
        {
            if (id != model.Id) return BadRequest();
            var currentVendorId = await _context.InventoryItems.Where(i => i.Id == id).Select(i => i.VendorId).FirstOrDefaultAsync();
            await ApplyVendorAsync(model, currentVendorId);
            if (!ModelState.IsValid)
            {
                await LoadVendorsAsync(model.VendorId);
                return View(model);
            }

            _context.InventoryItems.Update(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Item updated.";
            return RedirectToAction(nameof(Index));
        }

        // Active vendors (plus the item's current vendor even if it was deactivated since).
        private async Task LoadVendorsAsync(int? currentVendorId)
        {
            ViewBag.Vendors = await _context.Vendors.AsNoTracking()
                .Where(v => v.IsActive || v.Id == currentVendorId)
                .OrderBy(v => v.Name)
                .ToListAsync();
        }

        // A chosen vendor must exist and be active (unless it is the one already linked); its name fills Supplier.
        private async Task ApplyVendorAsync(InventoryItem model, int? currentVendorId)
        {
            if (!model.VendorId.HasValue) return;
            var vendor = await _context.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == model.VendorId);
            if (vendor == null || (!vendor.IsActive && vendor.Id != currentVendorId))
            {
                ModelState.AddModelError(nameof(InventoryItem.VendorId), "Choose an active vendor.");
                return;
            }

            model.Supplier = vendor.Name;
        }

        // ── Transactions ──────────────────────────────────────────

        public async Task<IActionResult> Transactions(int? itemId)
        {
            var query = _context.InventoryTransactions
                .Include(t => t.InventoryItem)
                .AsQueryable();

            if (itemId.HasValue)
                query = query.Where(t => t.InventoryItemId == itemId.Value);

            ViewBag.Items = await _context.InventoryItems.Where(i => i.IsActive).OrderBy(i => i.Name).ToListAsync();
            ViewBag.SelectedItemId = itemId;
            ViewBag.FilterItem = itemId.HasValue
                ? await _context.InventoryItems.Where(i => i.Id == itemId.Value).Select(i => i.Name).FirstOrDefaultAsync()
                : null;

            var txns = await query.OrderByDescending(t => t.TransactionDate).Take(200).ToListAsync();
            return View(txns);
        }

        public async Task<IActionResult> AddTransaction(int? itemId)
        {
            ViewBag.Items = await _context.InventoryItems.Where(i => i.IsActive).OrderBy(i => i.Name).ToListAsync();
            ViewBag.PreselectedItemId = itemId;
            return View(new InventoryTransaction { TransactionDate = DateTime.Now, InventoryItemId = itemId ?? 0 });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTransaction(InventoryTransaction model)
        {
            var stockItem = await _context.InventoryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == model.InventoryItemId);
            if (stockItem == null) ModelState.AddModelError(nameof(model.InventoryItemId), "Choose an item.");
            if (model.TransactionType is not ("IN" or "OUT" or "Adjustment")) ModelState.AddModelError(nameof(model.TransactionType), "Choose the transaction type.");
            if (model.Quantity < 0 || (model.Quantity == 0 && model.TransactionType != "Adjustment")) ModelState.AddModelError(nameof(model.Quantity), "Enter a quantity greater than 0.");
            if (stockItem != null && model.TransactionType == "OUT" && model.Quantity > stockItem.CurrentStock)
                ModelState.AddModelError(nameof(model.Quantity), $"Only {stockItem.CurrentStock:0.##} {stockItem.Unit} in stock.");

            if (!ModelState.IsValid)
            {
                ViewBag.Items = await _context.InventoryItems.Where(i => i.IsActive).ToListAsync();
                ViewBag.PreselectedItemId = model.InventoryItemId;
                return View(model);
            }

            model.PerformedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            model.TransactionDate = DateTime.Now;
            _context.InventoryTransactions.Add(model);

            var item = await _context.InventoryItems.FindAsync(model.InventoryItemId);
            if (item != null)
            {
                item.CurrentStock = model.TransactionType switch
                {
                    "IN" => item.CurrentStock + model.Quantity,
                    "OUT" => item.CurrentStock - model.Quantity,
                    _ => model.Quantity
                };
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Transaction recorded.";
            return RedirectToAction(nameof(Transactions));
        }

        // ── Consumption (items used by departments / patients) ─────

        public async Task<IActionResult> Consume(int? itemId)
        {
            await LoadConsumeListsAsync();
            return View(new ConsumeInput { InventoryItemId = itemId ?? 0 });
        }

        /// <summary>Issues stock to a department (optionally for a patient, optionally charged to the patient's bill).</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Consume(ConsumeInput input)
        {
            var item = await _context.InventoryItems.FirstOrDefaultAsync(i => i.Id == input.InventoryItemId && i.IsActive);
            Patient? patient = input.PatientId.HasValue ? await _context.Patients.FirstOrDefaultAsync(p => p.Id == input.PatientId) : null;
            if (item == null) ModelState.AddModelError(nameof(input.InventoryItemId), "Choose an item.");
            if (input.Quantity <= 0) ModelState.AddModelError(nameof(input.Quantity), "Enter a quantity greater than 0.");
            else if (item != null && input.Quantity > item.CurrentStock) ModelState.AddModelError(nameof(input.Quantity), $"Only {item.CurrentStock:0.##} {item.Unit} in stock.");
            if (string.IsNullOrWhiteSpace(input.Department)) ModelState.AddModelError(nameof(input.Department), "Enter the department or ward that used the items.");
            if (input.PatientId.HasValue && patient == null) ModelState.AddModelError(nameof(input.PatientId), "Unknown patient.");
            if (input.ChargeToPatient && patient == null) ModelState.AddModelError(nameof(input.PatientId), "Choose the patient to charge.");
            if (input.ChargeToPatient && (input.UnitPrice is null or < 0)) ModelState.AddModelError(nameof(input.UnitPrice), "Enter the price to charge per unit.");
            if (!ModelState.IsValid)
            {
                await LoadConsumeListsAsync();
                return View(input);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            item!.CurrentStock -= input.Quantity;
            var transaction = new InventoryTransaction
            {
                InventoryItemId = item.Id,
                TransactionType = "OUT",
                Quantity = input.Quantity,
                UnitCost = item.UnitCost,
                ReferenceNumber = "Consumption",
                Remarks = input.Notes?.Trim() ?? string.Empty,
                Department = input.Department!.Trim(),
                PatientId = patient?.Id,
                PerformedByUserId = userId,
                TransactionDate = DateTime.Now
            };
            _context.InventoryTransactions.Add(transaction);

            Bill? bill = null;
            if (input.ChargeToPatient && patient != null)
            {
                bill = await AddToConsumablesBillAsync(patient, item, input.Quantity, input.UnitPrice!.Value, userId);
            }

            await _context.SaveChangesAsync();
            if (bill != null)
            {
                transaction.BillId = bill.Id;
                await _context.SaveChangesAsync();
            }

            await _audit.LogActivityAsync(userId, "CONSUME", "InventoryItem", item.Id.ToString(), null,
                $"{input.Quantity:0.##} {item.Unit} {item.Name} to {transaction.Department}" + (patient != null ? $" for {patient.PatientId}" : string.Empty) + (bill != null ? $", charged on {bill.BillNumber}" : string.Empty));
            TempData["SuccessMessage"] = $"{input.Quantity:0.##} {item.Unit} of {item.Name} issued to {transaction.Department}." + (bill != null ? $" Charged to {patient!.FirstName} {patient.LastName} on bill {bill.BillNumber}." : string.Empty);
            return RedirectToAction(nameof(Consumption));
        }

        /// <summary>Consumption report: stock issued by department and patient, with cost value.</summary>
        public async Task<IActionResult> Consumption(DateTime? from, DateTime? to, string? department)
        {
            var start = (from ?? DateTime.Today.AddDays(-30)).Date;
            var end = (to ?? DateTime.Today).Date.AddDays(1);
            var query = _context.InventoryTransactions.AsNoTracking().Include(t => t.InventoryItem)
                .Where(t => t.TransactionType == "OUT" && t.TransactionDate >= start && t.TransactionDate < end);
            if (!string.IsNullOrWhiteSpace(department)) query = query.Where(t => t.Department == department);
            var rows = await query.OrderByDescending(t => t.TransactionDate).Take(1000).ToListAsync();
            var patientIds = rows.Where(r => r.PatientId.HasValue).Select(r => r.PatientId!.Value).Distinct().ToList();
            ViewBag.Patients = await _context.Patients.AsNoTracking().Where(p => patientIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => $"{p.FirstName} {p.LastName} ({p.PatientId})");
            ViewBag.Departments = await _context.InventoryTransactions.AsNoTracking().Where(t => t.Department != "").Select(t => t.Department).Distinct().OrderBy(d => d).ToListAsync();
            ViewBag.From = start;
            ViewBag.To = end.AddDays(-1);
            ViewBag.Department = department;
            return View(rows);
        }

        private async Task LoadConsumeListsAsync()
        {
            ViewBag.Items = await _context.InventoryItems.AsNoTracking().Where(i => i.IsActive).OrderBy(i => i.Name).ToListAsync();
            ViewBag.Patients = await _context.Patients.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.FirstName).ThenBy(p => p.LastName)
                .Select(p => new { p.Id, Name = p.FirstName + " " + p.LastName + " (" + p.PatientId + ")" }).ToListAsync();
            ViewBag.Departments = await _context.Departments.AsNoTracking().Where(d => d.IsActive).Select(d => d.Name).OrderBy(n => n).ToListAsync();
        }

        /// <summary>Adds the items to today's open "Consumables" bill of the patient (in this hospital), or opens one.</summary>
        private async Task<Bill> AddToConsumablesBillAsync(Patient patient, InventoryItem item, decimal quantity, decimal unitPrice, string userId)
        {
            var today = DateTime.Today;
            var bill = await _context.Bills.Include(b => b.BillItems)
                .Where(b => b.PatientId == patient.Id && b.BillType == "Consumables" && b.Status == "Unpaid" && b.BillDate >= today)
                .OrderByDescending(b => b.Id).FirstOrDefaultAsync();
            if (bill == null)
            {
                var prefix = $"CNS{DateTime.Now:yyyyMMdd}";
                var last = await _context.Bills.IgnoreQueryFilters().Where(b => b.BillNumber.StartsWith(prefix)).OrderByDescending(b => b.BillNumber).Select(b => b.BillNumber).FirstOrDefaultAsync();
                var next = last != null && int.TryParse(last[prefix.Length..], out var n) ? n + 1 : 1;
                bill = new Bill
                {
                    BillNumber = $"{prefix}{next:D4}",
                    PatientId = patient.Id,
                    BillDate = today,
                    DueDate = today.AddDays(7),
                    BillType = "Consumables",
                    Status = "Unpaid",
                    Notes = "Consumables charged from inventory",
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now
                };
                _context.Bills.Add(bill);
            }

            var line = new BillItem
            {
                ItemName = item.Name,
                ItemType = "Consumable",
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalPrice = Math.Round(quantity * unitPrice, 2),
                Description = $"{item.ItemCode} issued from inventory",
                CreatedDate = DateTime.Now
            };
            bill.BillItems.Add(line);
            bill.TotalAmount = bill.BillItems.Sum(i => i.TotalPrice);
            bill.PendingAmount = bill.TotalAmount - bill.PaidAmount;
            bill.UpdatedDate = DateTime.Now;
            return bill;
        }

        public class ConsumeInput
        {
            public int InventoryItemId { get; set; }
            public decimal Quantity { get; set; }
            public string? Department { get; set; }
            public int? PatientId { get; set; }
            public bool ChargeToPatient { get; set; }
            public decimal? UnitPrice { get; set; }
            public string? Notes { get; set; }
        }

        // ── Low Stock alerts ───────────────────────────────────────

        public async Task<IActionResult> LowStock()
        {
            var items = await _context.InventoryItems
                .Where(i => i.CurrentStock <= i.ReorderLevel && i.IsActive)
                .OrderBy(i => i.CurrentStock)
                .ToListAsync();
            return View(items);
        }
    }
}
