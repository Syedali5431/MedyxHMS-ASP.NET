using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Controllers
{
    /// <summary>Vendors / suppliers master shared by all hospitals of the group.</summary>
    [Authorize(Roles = ViewRoles)]
    public class VendorsController : Controller
    {
        public const string ViewRoles = "SuperAdmin,Admin,Accountant,Pharmacist,Staff";
        public const string ManageRoles = "SuperAdmin,Admin,Accountant";
        public static readonly string[] Categories =
            { "Medicines / pharmacy", "Medical consumables", "Medical equipment", "Lab reagents", "Maintenance / services", "Food / catering", "Linen / housekeeping", "IT / office", "Other" };

        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public VendorsController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        private bool CanManage => ManageRoles.Split(',').Any(User.IsInRole);

        public async Task<IActionResult> Index(string? search, string? category, string status = "active")
        {
            var query = _context.Vendors.AsNoTracking().AsQueryable();
            if (status == "active") query = query.Where(v => v.IsActive);
            else if (status == "inactive") query = query.Where(v => !v.IsActive);
            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(v => v.Category == category);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(v => v.Name.Contains(search) || v.VendorCode.Contains(search) || v.ContactPerson.Contains(search) || v.Phone.Contains(search) || v.TaxNumber.Contains(search));

            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.Status = status;
            ViewBag.CanManage = CanManage;
            return View(await query.OrderBy(v => v.Name).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var vendor = await _context.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
            if (vendor == null) return NotFound();
            ViewBag.Items = await _context.InventoryItems.AsNoTracking().Where(i => i.VendorId == id).OrderBy(i => i.Name).ToListAsync();
            ViewBag.Bills = await _context.PurchaseBills.AsNoTracking().Where(b => b.VendorId == id).OrderByDescending(b => b.InvoiceDate).ThenByDescending(b => b.Id).Take(50).ToListAsync();
            ViewBag.Outstanding = await _context.PurchaseBills.Where(b => b.VendorId == id && (b.Status == "Approved" || b.Status == "Received")).SumAsync(b => (decimal?)(b.TotalAmount - b.PaidAmount)) ?? 0m;
            ViewBag.CanManage = CanManage;
            return View(vendor);
        }

        [Authorize(Roles = ManageRoles)]
        public IActionResult Create()
        {
            return View("Form", new Vendor());
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = ManageRoles)]
        public async Task<IActionResult> Create(Vendor input)
        {
            Normalize(input);
            await ValidateAsync(input);
            if (!ModelState.IsValid) return View("Form", input);

            input.VendorCode = string.IsNullOrWhiteSpace(input.VendorCode) ? await NextCodeAsync() : input.VendorCode;
            input.CreatedAt = DateTime.Now;
            input.CreatedBy = User.Identity?.Name ?? string.Empty;
            input.IsActive = true;
            _context.Vendors.Add(input);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, "CREATE", "Vendor", input.Id.ToString(), null, $"{input.VendorCode} {input.Name} ({input.Category})");
            TempData["SuccessMessage"] = $"Vendor {input.VendorCode} – {input.Name} added.";
            return RedirectToAction(nameof(Details), new { id = input.Id });
        }

        [Authorize(Roles = ManageRoles)]
        public async Task<IActionResult> Edit(int id)
        {
            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == id);
            if (vendor == null) return NotFound();
            return View("Form", vendor);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = ManageRoles)]
        public async Task<IActionResult> Edit(int id, Vendor input)
        {
            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == id);
            if (vendor == null) return NotFound();

            input.Id = id;
            Normalize(input);
            await ValidateAsync(input);
            if (!ModelState.IsValid) return View("Form", input);

            var old = $"{vendor.VendorCode} {vendor.Name} terms {vendor.PaymentTermsDays}d bank {Mask(vendor.BankAccountNumber)}";
            vendor.VendorCode = string.IsNullOrWhiteSpace(input.VendorCode) ? vendor.VendorCode : input.VendorCode;
            vendor.Name = input.Name;
            vendor.Category = input.Category;
            vendor.ContactPerson = input.ContactPerson;
            vendor.Phone = input.Phone;
            vendor.Email = input.Email;
            vendor.Address = input.Address;
            vendor.City = input.City;
            vendor.TaxNumber = input.TaxNumber;
            vendor.LicenseNumber = input.LicenseNumber;
            vendor.PaymentTermsDays = input.PaymentTermsDays;
            vendor.BankName = input.BankName;
            vendor.BankAccountName = input.BankAccountName;
            vendor.BankAccountNumber = input.BankAccountNumber;
            vendor.BankRoutingCode = input.BankRoutingCode;
            vendor.Rating = input.Rating;
            vendor.Notes = input.Notes;
            vendor.UpdatedAt = DateTime.Now;

            // Items linked to this vendor show its current name.
            var items = await _context.InventoryItems.IgnoreQueryFilters().Where(i => i.VendorId == id).ToListAsync();
            items.ForEach(i => i.Supplier = vendor.Name);

            await _context.SaveChangesAsync();
            // Bank details are logged masked.
            await _audit.LogActivityAsync(UserId, "UPDATE", "Vendor", id.ToString(), old, $"{vendor.VendorCode} {vendor.Name} terms {vendor.PaymentTermsDays}d bank {Mask(vendor.BankAccountNumber)}");
            TempData["SuccessMessage"] = "Vendor updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = ManageRoles)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == id);
            if (vendor == null) return NotFound();
            vendor.IsActive = !vendor.IsActive;
            vendor.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(UserId, vendor.IsActive ? "ACTIVATE" : "DEACTIVATE", "Vendor", id.ToString(), null, vendor.Name);
            TempData["SuccessMessage"] = vendor.IsActive ? $"{vendor.Name} reactivated." : $"{vendor.Name} deactivated – it can no longer be chosen for new items or purchases.";
            return RedirectToAction(nameof(Details), new { id });
        }

        public static string Mask(string? account) =>
            string.IsNullOrEmpty(account) ? string.Empty : account.Length <= 4 ? new string('•', account.Length) : new string('•', account.Length - 4) + account[^4..];

        private static void Normalize(Vendor v)
        {
            string T(string? x) => (x ?? string.Empty).Trim();
            v.VendorCode = T(v.VendorCode).ToUpperInvariant();
            v.Name = T(v.Name);
            v.ContactPerson = T(v.ContactPerson);
            v.Phone = T(v.Phone);
            v.Email = T(v.Email);
            v.Address = T(v.Address);
            v.City = T(v.City);
            v.TaxNumber = T(v.TaxNumber).ToUpperInvariant();
            v.LicenseNumber = T(v.LicenseNumber);
            v.BankName = T(v.BankName);
            v.BankAccountName = T(v.BankAccountName);
            v.BankAccountNumber = T(v.BankAccountNumber).Replace(" ", string.Empty);
            v.BankRoutingCode = T(v.BankRoutingCode).ToUpperInvariant();
            v.Notes = T(v.Notes);
        }

        private async Task ValidateAsync(Vendor v)
        {
            if (!Categories.Contains(v.Category)) ModelState.AddModelError(nameof(v.Category), "Choose a category.");
            if (!string.IsNullOrEmpty(v.Email) && !new EmailAddressAttribute().IsValid(v.Email)) ModelState.AddModelError(nameof(v.Email), "Enter a valid e-mail address.");
            if (!string.IsNullOrEmpty(v.BankAccountNumber) && !v.BankAccountNumber.All(char.IsLetterOrDigit)) ModelState.AddModelError(nameof(v.BankAccountNumber), "Use letters and digits only.");
            if (!string.IsNullOrEmpty(v.VendorCode) && await _context.Vendors.AnyAsync(x => x.VendorCode == v.VendorCode && x.Id != v.Id))
                ModelState.AddModelError(nameof(v.VendorCode), "This vendor code is already used.");
            if (!string.IsNullOrEmpty(v.Name) && await _context.Vendors.AnyAsync(x => x.Name == v.Name && x.Id != v.Id))
                ModelState.AddModelError(nameof(v.Name), "A vendor with this name already exists.");
            if (!string.IsNullOrEmpty(v.TaxNumber) && await _context.Vendors.AnyAsync(x => x.TaxNumber == v.TaxNumber && x.Id != v.Id))
                ModelState.AddModelError(nameof(v.TaxNumber), "Another vendor already has this tax number.");
        }

        private async Task<string> NextCodeAsync()
        {
            var codes = await _context.Vendors.Where(v => v.VendorCode.StartsWith("V-")).Select(v => v.VendorCode).ToListAsync();
            var max = codes.Select(c => int.TryParse(c[2..], out var n) ? n : 0).DefaultIfEmpty(0).Max();
            return $"V-{max + 1:D4}";
        }
    }
}
