using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedyxHMS.Controllers
{
    [Authorize(Policy = "RequireAdminRole")]
    public class CmsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CmsController> _logger;

        public CmsController(ApplicationDbContext db, ILogger<CmsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ─── Pages ────────────────────────────────────────────────────────────

        public async Task<IActionResult> Index(string status = null, string search = null)
        {
            var query = _db.CmsPages.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(p => p.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Title.Contains(search) || p.Slug.Contains(search));

            var vm = new CmsPageIndexViewModel
            {
                Pages = await query.OrderBy(p => p.SortOrder).ThenBy(p => p.Title).ToListAsync(),
                StatusFilter = status,
                SearchTerm = search
            };
            return View(vm);
        }

        public IActionResult CreatePage() => View("EditPage", new CmsPageEditViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePage(CmsPageEditViewModel vm)
        {
            if (!ModelState.IsValid) return View("EditPage", vm);

            if (await _db.CmsPages.AnyAsync(p => p.Slug == vm.Slug))
            {
                ModelState.AddModelError(nameof(vm.Slug), "A page with this slug already exists.");
                return View("EditPage", vm);
            }

            var page = new CmsPage
            {
                Title = vm.Title,
                Slug = vm.Slug,
                Content = vm.Content,
                MetaDescription = vm.MetaDescription,
                Status = vm.Status,
                ShowInMenu = vm.ShowInMenu,
                SortOrder = vm.SortOrder,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity.Name
            };
            _db.CmsPages.Add(page);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Page created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EditPage(int id)
        {
            var page = await _db.CmsPages.FindAsync(id);
            if (page == null) return NotFound();

            var vm = new CmsPageEditViewModel
            {
                Id = page.Id,
                Title = page.Title,
                Slug = page.Slug,
                Content = page.Content,
                MetaDescription = page.MetaDescription,
                Status = page.Status,
                ShowInMenu = page.ShowInMenu,
                SortOrder = page.SortOrder
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPage(int id, CmsPageEditViewModel vm)
        {
            if (id != vm.Id) return BadRequest();
            if (!ModelState.IsValid) return View(vm);

            var page = await _db.CmsPages.FindAsync(id);
            if (page == null) return NotFound();

            if (await _db.CmsPages.AnyAsync(p => p.Slug == vm.Slug && p.Id != id))
            {
                ModelState.AddModelError(nameof(vm.Slug), "A page with this slug already exists.");
                return View(vm);
            }

            page.Title = vm.Title;
            page.Slug = vm.Slug;
            page.Content = vm.Content;
            page.MetaDescription = vm.MetaDescription;
            page.Status = vm.Status;
            page.ShowInMenu = vm.ShowInMenu;
            page.SortOrder = vm.SortOrder;
            page.UpdatedAt = DateTime.UtcNow;
            page.UpdatedBy = User.Identity.Name;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Page updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePage(int id)
        {
            var page = await _db.CmsPages.FindAsync(id);
            if (page == null) return NotFound();

            // Remove menu items linked to this page
            var linkedMenuItems = _db.CmsMenuItems.Where(m => m.CmsPageId == id);
            foreach (var item in linkedMenuItems)
                item.CmsPageId = null;

            _db.CmsPages.Remove(page);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Page deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ─── Notices ──────────────────────────────────────────────────────────

        public async Task<IActionResult> Notices(string type = null, string search = null, int page = 1, int pageSize = 20)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 5 or > 100 ? 20 : pageSize;

            var query = _db.CmsNotices.AsQueryable();

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(n => n.Type == type);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(n => n.Title.Contains(search));

            var totalCount = await query.CountAsync();

            var vm = new CmsNoticeIndexViewModel
            {
                Notices = await query.OrderByDescending(n => n.PublishedAt ?? n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(),
                TypeFilter = type,
                SearchTerm = search,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
            return View(vm);
        }

        public IActionResult CreateNotice() => View("EditNotice", new CmsNoticeEditViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNotice(CmsNoticeEditViewModel vm)
        {
            if (!ModelState.IsValid) return View("EditNotice", vm);

            if (await _db.CmsNotices.AnyAsync(n => n.Slug == vm.Slug))
            {
                ModelState.AddModelError(nameof(vm.Slug), "A notice with this slug already exists.");
                return View("EditNotice", vm);
            }

            var notice = new CmsNotice
            {
                Title = vm.Title,
                Slug = vm.Slug,
                Summary = vm.Summary,
                Content = vm.Content,
                Type = vm.Type,
                IsActive = vm.IsActive,
                PublishedAt = vm.IsActive ? (vm.PublishedAt ?? DateTime.UtcNow) : vm.PublishedAt,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity.Name
            };
            _db.CmsNotices.Add(notice);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Notice created successfully.";
            return RedirectToAction(nameof(Notices));
        }

        public async Task<IActionResult> EditNotice(int id)
        {
            var notice = await _db.CmsNotices.FindAsync(id);
            if (notice == null) return NotFound();

            var vm = new CmsNoticeEditViewModel
            {
                Id = notice.Id,
                Title = notice.Title,
                Slug = notice.Slug,
                Summary = notice.Summary,
                Content = notice.Content,
                Type = notice.Type,
                IsActive = notice.IsActive,
                PublishedAt = notice.PublishedAt
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNotice(int id, CmsNoticeEditViewModel vm)
        {
            if (id != vm.Id) return BadRequest();
            if (!ModelState.IsValid) return View(vm);

            var notice = await _db.CmsNotices.FindAsync(id);
            if (notice == null) return NotFound();

            if (await _db.CmsNotices.AnyAsync(n => n.Slug == vm.Slug && n.Id != id))
            {
                ModelState.AddModelError(nameof(vm.Slug), "A notice with this slug already exists.");
                return View(vm);
            }

            notice.Title = vm.Title;
            notice.Slug = vm.Slug;
            notice.Summary = vm.Summary;
            notice.Content = vm.Content;
            notice.Type = vm.Type;
            notice.IsActive = vm.IsActive;
            notice.PublishedAt = vm.IsActive ? (vm.PublishedAt ?? notice.PublishedAt ?? DateTime.UtcNow) : vm.PublishedAt;
            notice.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Notice updated.";
            return RedirectToAction(nameof(Notices));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNotice(int id)
        {
            var notice = await _db.CmsNotices.FindAsync(id);
            if (notice == null) return NotFound();
            _db.CmsNotices.Remove(notice);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Notice deleted.";
            return RedirectToAction(nameof(Notices));
        }

        // ─── Menu Items ───────────────────────────────────────────────────────

        public async Task<IActionResult> Menu()
        {
            var vm = new CmsMenuIndexViewModel
            {
                MenuItems = await _db.CmsMenuItems
                    .Include(m => m.CmsPage)
                    .OrderBy(m => m.SortOrder)
                    .ToListAsync()
            };
            return View(vm);
        }

        public async Task<IActionResult> CreateMenuItem()
        {
            var vm = new CmsMenuItemEditViewModel
            {
                AvailablePages = await _db.CmsPages
                    .Where(p => p.Status == "Published")
                    .OrderBy(p => p.Title)
                    .ToListAsync()
            };
            return View("EditMenuItem", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMenuItem(CmsMenuItemEditViewModel vm)
        {
            vm.AvailablePages = await _db.CmsPages.Where(p => p.Status == "Published").OrderBy(p => p.Title).ToListAsync();
            if (!ModelState.IsValid) return View("EditMenuItem", vm);

            // Resolve URL from linked page if no explicit URL
            if (string.IsNullOrWhiteSpace(vm.Url) && vm.CmsPageId.HasValue)
            {
                var page = await _db.CmsPages.FindAsync(vm.CmsPageId.Value);
                vm.Url = page != null ? $"/site/page/{page.Slug}" : "#";
            }

            _db.CmsMenuItems.Add(new CmsMenuItem
            {
                Label = vm.Label,
                Url = vm.Url ?? "#",
                CmsPageId = vm.CmsPageId,
                SortOrder = vm.SortOrder,
                IsActive = vm.IsActive,
                OpenInNewTab = vm.OpenInNewTab
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Menu item added.";
            return RedirectToAction(nameof(Menu));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMenuItem(int id)
        {
            var item = await _db.CmsMenuItems.FindAsync(id);
            if (item == null) return NotFound();
            _db.CmsMenuItems.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Menu item deleted.";
            return RedirectToAction(nameof(Menu));
        }

        // ─── Appointment Requests ─────────────────────────────────────────────

        public async Task<IActionResult> AppointmentRequests(
            string status = null, string search = null, DateTime? dateFrom = null, DateTime? dateTo = null,
            int page = 1, int pageSize = 20)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 5 or > 100 ? 20 : pageSize;

            var query = _db.PublicAppointmentRequests
                .Include(r => r.Doctor)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(r => r.PatientName.Contains(search)
                                      || r.Phone.Contains(search)
                                      || (r.Doctor != null && (r.Doctor.FirstName + " " + r.Doctor.LastName).Contains(search)));
            if (dateFrom.HasValue)
                query = query.Where(r => r.PreferredDate >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(r => r.PreferredDate <= dateTo.Value);

            var totalCount = await query.CountAsync();

            var vm = new PublicAppointmentRequestIndexViewModel
            {
                Requests = await query.OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(),
                StatusFilter = status,
                SearchTerm = search,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRequestStatus(int id, string status, string adminNotes)
        {
            var request = await _db.PublicAppointmentRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = status;
            request.AdminNotes = adminNotes;
            request.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Request #{id} marked as {status}.";
            return RedirectToAction(nameof(AppointmentRequests));
        }

        // ─── Helper ───────────────────────────────────────────────────────────

        public static string ToSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;
            var slug = title.ToLowerInvariant();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            return slug.Trim('-');
        }
    }
}
