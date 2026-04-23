using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MedyxHMS.Controllers
{
    [Authorize]
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
        public IActionResult Create()
        {
            return View(new InventoryItem());
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Create(InventoryItem model)
        {
            if (!ModelState.IsValid) return View(model);

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
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Edit(int id, InventoryItem model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            _context.InventoryItems.Update(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Item updated.";
            return RedirectToAction(nameof(Index));
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
            if (!ModelState.IsValid)
            {
                ViewBag.Items = await _context.InventoryItems.Where(i => i.IsActive).ToListAsync();
                ViewBag.PreselectedItemId = model.InventoryItemId;
                return View(model);
            }

            model.PerformedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            model.TransactionDate = DateTime.UtcNow;
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
