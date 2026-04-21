using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
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
        private readonly ISettingService _settingService;
        private readonly IEmailNotificationProvider _emailNotificationProvider;
        private readonly ISmsNotificationProvider _smsNotificationProvider;
        private readonly IPublicBookingNotificationService _publicBookingNotificationService;
        private readonly IExportService _exportService;
        private readonly IFileService _fileService;
        private readonly ISmtpHealthService? _smtpHealthService;

        public CmsController(
            ApplicationDbContext db,
            ILogger<CmsController> logger,
            ISettingService settingService,
            IEmailNotificationProvider emailNotificationProvider,
            ISmsNotificationProvider smsNotificationProvider,
            IPublicBookingNotificationService publicBookingNotificationService,
            IExportService exportService,
            IFileService fileService,
            ISmtpHealthService? smtpHealthService = null)
        {
            _db = db;
            _logger = logger;
            _settingService = settingService;
            _emailNotificationProvider = emailNotificationProvider;
            _smsNotificationProvider = smsNotificationProvider;
            _publicBookingNotificationService = publicBookingNotificationService;
            _exportService = exportService;
            _fileService = fileService;
            _smtpHealthService = smtpHealthService;
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

        [HttpGet]
        public async Task<IActionResult> IndexExport(string format = "csv", string status = null, string search = null)
        {
            format = (format ?? "csv").Trim().ToLowerInvariant();
            if (format != "csv" && format != "pdf")
                return BadRequest("Only CSV and PDF exports are supported.");

            var query = _db.CmsPages.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(p => p.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Title.Contains(search) || p.Slug.Contains(search));

            var pages = await query.OrderBy(p => p.SortOrder).ThenBy(p => p.Title).ToListAsync();
            var headers = new[] { "Title", "Slug", "Status", "In Menu", "Sort Order", "Created (UTC)" };
            var rows = pages.Select(p => (IReadOnlyList<string>)new[]
            {
                p.Title ?? string.Empty,
                p.Slug ?? string.Empty,
                p.Status ?? string.Empty,
                p.ShowInMenu ? "Yes" : "No",
                p.SortOrder.ToString(),
                p.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            if (format == "csv")
            {
                var bytes = _exportService.BuildCsv("CMS Pages Export", headers, rows);
                return File(bytes, "text/csv", $"cms_pages_{stamp}.csv");
            }

            var pdfBytes = _exportService.BuildPdfTable("CMS Pages Export", headers, rows);
            return File(pdfBytes, "application/pdf", $"cms_pages_{stamp}.pdf");
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
                FeaturedImage = vm.FeaturedImage,
                FontFamily = vm.FontFamily,
                FontSizePx = vm.FontSizePx,
                Status = vm.Status,
                ShowInMenu = vm.ShowInMenu,
                SortOrder = vm.SortOrder,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity.Name
            };

            if (vm.FeaturedImageFile != null)
            {
                page.FeaturedImage = await _fileService.UploadFileAsync(vm.FeaturedImageFile, "cms-pages");
            }

            _db.CmsPages.Add(page);
            await _db.SaveChangesAsync();
            await EnsurePageMenuItemAsync(page);
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
                FeaturedImage = page.FeaturedImage,
                FontFamily = page.FontFamily,
                FontSizePx = page.FontSizePx,
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
            page.FontFamily = vm.FontFamily;
            page.FontSizePx = vm.FontSizePx;
            page.Status = vm.Status;
            page.ShowInMenu = vm.ShowInMenu;
            page.SortOrder = vm.SortOrder;
            page.UpdatedAt = DateTime.UtcNow;
            page.UpdatedBy = User.Identity.Name;

            if (vm.FeaturedImageFile != null)
            {
                page.FeaturedImage = await _fileService.UploadFileAsync(vm.FeaturedImageFile, "cms-pages");
            }
            else
            {
                page.FeaturedImage = vm.FeaturedImage;
            }

            await _db.SaveChangesAsync();
            await EnsurePageMenuItemAsync(page);
            TempData["Success"] = "Page updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePage(int id)
        {
            var page = await _db.CmsPages.FindAsync(id);
            if (page == null) return NotFound();

            var linkedMenuItems = await _db.CmsMenuItems.Where(m => m.CmsPageId == id).ToListAsync();
            if (linkedMenuItems.Count > 0)
            {
                _db.CmsMenuItems.RemoveRange(linkedMenuItems);
            }

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

        [HttpGet]
        public async Task<IActionResult> NoticesExport(string format = "csv", string type = null, string search = null)
        {
            format = (format ?? "csv").Trim().ToLowerInvariant();
            if (format != "csv" && format != "pdf")
                return BadRequest("Only CSV and PDF exports are supported.");

            var query = _db.CmsNotices.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(n => n.Type == type);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(n => n.Title.Contains(search));

            var notices = await query.OrderByDescending(n => n.PublishedAt ?? n.CreatedAt).ToListAsync();
            var headers = new[] { "Title", "Slug", "Type", "Active", "Published (UTC)" };
            var rows = notices.Select(n => (IReadOnlyList<string>)new[]
            {
                n.Title ?? string.Empty,
                n.Slug ?? string.Empty,
                n.Type ?? string.Empty,
                n.IsActive ? "Yes" : "No",
                (n.PublishedAt ?? n.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            if (format == "csv")
            {
                var bytes = _exportService.BuildCsv("CMS Notices Export", headers, rows);
                return File(bytes, "text/csv", $"cms_notices_{stamp}.csv");
            }

            var pdfBytes = _exportService.BuildPdfTable("CMS Notices Export", headers, rows);
            return File(pdfBytes, "application/pdf", $"cms_notices_{stamp}.pdf");
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

        [HttpGet]
        public async Task<IActionResult> MenuExport(string format = "csv")
        {
            format = (format ?? "csv").Trim().ToLowerInvariant();
            if (format != "csv" && format != "pdf")
                return BadRequest("Only CSV and PDF exports are supported.");

            var menuItems = await _db.CmsMenuItems
                .AsNoTracking()
                .Include(m => m.CmsPage)
                .OrderBy(m => m.SortOrder)
                .ToListAsync();

            var headers = new[] { "Order", "Label", "URL", "Linked Page", "Open In New Tab", "Active" };
            var rows = menuItems.Select(m => (IReadOnlyList<string>)new[]
            {
                m.SortOrder.ToString(),
                m.Label ?? string.Empty,
                m.Url ?? string.Empty,
                m.CmsPage?.Title ?? string.Empty,
                m.OpenInNewTab ? "Yes" : "No",
                m.IsActive ? "Yes" : "No"
            }).ToList();

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            if (format == "csv")
            {
                var bytes = _exportService.BuildCsv("CMS Menu Export", headers, rows);
                return File(bytes, "text/csv", $"cms_menu_{stamp}.csv");
            }

            var pdfBytes = _exportService.BuildPdfTable("CMS Menu Export", headers, rows);
            return File(pdfBytes, "application/pdf", $"cms_menu_{stamp}.pdf");
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

        // ─── Notification Settings ──────────────────────────────────────────

        public async Task<IActionResult> NotificationSettings()
        {
            var hospitalSettings = await _settingService.GetHospitalSettingsAsync();

            var vm = new CmsNotificationSettingsViewModel
            {
                EnableEmailNotifications = hospitalSettings.EnableEmailNotifications,
                EnableSMSNotifications = hospitalSettings.EnableSMSNotifications,
                SmsProvider = (await GetSettingValueAsync("Notification:Sms:Provider") ?? "Twilio").Trim(),
                TwilioAccountSid = (await GetSettingValueAsync("Notification:Sms:Twilio:AccountSid"))?.Trim(),
                TwilioFromPhone = (await GetSettingValueAsync("Notification:Sms:Twilio:FromPhone"))?.Trim(),
                TwilioEnableLiveSend = bool.TryParse(await GetSettingValueAsync("Notification:Sms:Twilio:EnableLiveSend"), out var liveSend) && liveSend,
                HasSavedTwilioAuthToken = !string.IsNullOrWhiteSpace(await GetSettingValueAsync("Notification:Sms:Twilio:AuthToken")),
                AfricaTalkingUsername = (await GetSettingValueAsync("Notification:Sms:AfricaTalking:Username"))?.Trim(),
                AfricaTalkingSenderId = (await GetSettingValueAsync("Notification:Sms:AfricaTalking:SenderId"))?.Trim(),
                AfricaTalkingEnableLiveSend = bool.TryParse(await GetSettingValueAsync("Notification:Sms:AfricaTalking:EnableLiveSend"), out var africaLiveSend) && africaLiveSend,
                HasSavedAfricaTalkingApiKey = !string.IsNullOrWhiteSpace(await GetSettingValueAsync("Notification:Sms:AfricaTalking:ApiKey")),
                EmailOptOutList = (await GetSettingValueAsync("Notification:OptOut:EmailRecipients"))?.Trim(),
                SmsOptOutList = (await GetSettingValueAsync("Notification:OptOut:PhoneRecipients"))?.Trim(),
                AppointmentConfirmedEmailSubjectTemplate = await GetSettingValueAsync("Notification:Templates:AppointmentConfirmed:EmailSubject")
                    ?? "Appointment Request Confirmed",
                AppointmentConfirmedEmailBodyTemplate = await GetSettingValueAsync("Notification:Templates:AppointmentConfirmed:EmailBody")
                    ?? "Hello {{PatientName}},\n\nYour appointment request has been confirmed.\nDoctor: {{DoctorName}}\nDate: {{Date}}\nTime: {{Time}}\n\nPlease arrive 15 minutes before your scheduled time.\nIf you need to reschedule, contact the hospital front desk at {{SupportPhone}}.\n\nRegards,\n{{HospitalName}}",
                AppointmentConfirmedSmsBodyTemplate = await GetSettingValueAsync("Notification:Templates:AppointmentConfirmed:SmsBody")
                    ?? "Medyx: Appointment confirmed for {{PatientName}} with {{DoctorName}} on {{Date}} at {{Time}}.",
                LastSmsTestStatus = await GetSettingValueAsync("Notification:Test:Sms:LastStatus"),
                LastSmsTestMessage = await GetSettingValueAsync("Notification:Test:Sms:LastMessage"),
                LastSmsTestTarget = await GetSettingValueAsync("Notification:Test:Sms:LastTarget"),
                LastSmsTestAtUtc = ParseNullableDateTime(await GetSettingValueAsync("Notification:Test:Sms:LastAtUtc")),
                LastEmailTestStatus = await GetSettingValueAsync("Notification:Test:Email:LastStatus"),
                LastEmailTestMessage = await GetSettingValueAsync("Notification:Test:Email:LastMessage"),
                LastEmailTestTarget = await GetSettingValueAsync("Notification:Test:Email:LastTarget"),
                LastEmailTestAtUtc = ParseNullableDateTime(await GetSettingValueAsync("Notification:Test:Email:LastAtUtc")),
                SmtpHealth = _smtpHealthService == null
                    ? new SmtpHealthStatus
                    {
                        IsConfigured = false,
                        ConnectivityOk = false,
                        Issues = new List<string> { "SMTP health service is unavailable in this context." }
                    }
                    : await _smtpHealthService.CheckAsync()
            };

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> NotificationSettings(CmsNotificationSettingsViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.HasSavedTwilioAuthToken = !string.IsNullOrWhiteSpace(await GetSettingValueAsync("Notification:Sms:Twilio:AuthToken"));
                vm.HasSavedAfricaTalkingApiKey = !string.IsNullOrWhiteSpace(await GetSettingValueAsync("Notification:Sms:AfricaTalking:ApiKey"));
                return View(vm);
            }

            await UpsertSettingAsync("EnableEmailNotifications", vm.EnableEmailNotifications ? "true" : "false", "bool", "Hospital", "Enable/disable outbound email notifications.");
            await UpsertSettingAsync("EnableSMSNotifications", vm.EnableSMSNotifications ? "true" : "false", "bool", "Hospital", "Enable/disable outbound SMS notifications.");

            await UpsertSettingAsync("Notification:Sms:Provider", (vm.SmsProvider ?? "Twilio").Trim(), "string", "Notification", "Active SMS provider (Twilio or AfricaTalking).");
            await UpsertSettingAsync("Notification:Sms:Twilio:AccountSid", (vm.TwilioAccountSid ?? string.Empty).Trim(), "string", "Notification", "Twilio account SID for SMS provider.");
            await UpsertSettingAsync("Notification:Sms:Twilio:FromPhone", (vm.TwilioFromPhone ?? string.Empty).Trim(), "string", "Notification", "Twilio source phone number used for outbound SMS.");
            await UpsertSettingAsync("Notification:Sms:Twilio:EnableLiveSend", vm.TwilioEnableLiveSend ? "true" : "false", "bool", "Notification", "Enable live Twilio API SMS sends.");
            await UpsertSettingAsync("Notification:Sms:AfricaTalking:Username", (vm.AfricaTalkingUsername ?? string.Empty).Trim(), "string", "Notification", "Africa's Talking username for SMS provider.");
            await UpsertSettingAsync("Notification:Sms:AfricaTalking:SenderId", (vm.AfricaTalkingSenderId ?? string.Empty).Trim(), "string", "Notification", "Africa's Talking sender ID used for outbound SMS.");
            await UpsertSettingAsync("Notification:Sms:AfricaTalking:EnableLiveSend", vm.AfricaTalkingEnableLiveSend ? "true" : "false", "bool", "Notification", "Enable live Africa's Talking API SMS sends.");
            await UpsertSettingAsync("Notification:OptOut:EnableEmailOptOut", "true", "bool", "Notification", "Enable recipient-level email opt-out enforcement.");
            await UpsertSettingAsync("Notification:OptOut:EnableSmsOptOut", "true", "bool", "Notification", "Enable recipient-level SMS opt-out enforcement.");
            await UpsertSettingAsync("Notification:OptOut:EmailRecipients", (vm.EmailOptOutList ?? string.Empty).Trim(), "string", "Notification", "Comma or newline separated email recipients opted out from notifications.");
            await UpsertSettingAsync("Notification:OptOut:PhoneRecipients", (vm.SmsOptOutList ?? string.Empty).Trim(), "string", "Notification", "Comma or newline separated phone recipients opted out from notifications.");
            await UpsertSettingAsync("Notification:Templates:AppointmentConfirmed:EmailSubject", (vm.AppointmentConfirmedEmailSubjectTemplate ?? string.Empty).Trim(), "string", "Notification", "Template subject for appointment confirmation emails.");
            await UpsertSettingAsync("Notification:Templates:AppointmentConfirmed:EmailBody", (vm.AppointmentConfirmedEmailBodyTemplate ?? string.Empty).Trim(), "string", "Notification", "Template body for appointment confirmation emails.");
            await UpsertSettingAsync("Notification:Templates:AppointmentConfirmed:SmsBody", (vm.AppointmentConfirmedSmsBodyTemplate ?? string.Empty).Trim(), "string", "Notification", "Template body for appointment confirmation SMS messages.");

            if (!string.IsNullOrWhiteSpace(vm.TwilioAuthToken))
            {
                await UpsertSettingAsync("Notification:Sms:Twilio:AuthToken", vm.TwilioAuthToken.Trim(), "string", "Notification", "Twilio auth token for SMS provider.");
            }

            if (!string.IsNullOrWhiteSpace(vm.AfricaTalkingApiKey))
            {
                await UpsertSettingAsync("Notification:Sms:AfricaTalking:ApiKey", vm.AfricaTalkingApiKey.Trim(), "string", "Notification", "Africa's Talking API key for SMS provider.");
            }

            TempData["Success"] = "Notification settings saved.";
            return RedirectToAction(nameof(NotificationSettings));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTestSms(CmsNotificationSettingsViewModel vm)
        {
            var phone = (vm.TestSmsPhone ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(phone))
            {
                TempData["Error"] = "Please enter a phone number for the SMS test.";
                return RedirectToAction(nameof(NotificationSettings));
            }

            var testMessage = $"Medyx test SMS from CMS at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.";

            try
            {
                await _smsNotificationProvider.SendAsync(phone, testMessage);
                await RecordNotificationTestResultAsync("Sms", "Success", phone,
                    "Test SMS request submitted to provider.");
                TempData["Success"] = "Test SMS request submitted. Check logs/provider dashboard for delivery status.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Test SMS failed for phone {Phone}", phone);
                await RecordNotificationTestResultAsync("Sms", "Failed", phone,
                    "Test SMS failed. Review Twilio settings and logs.");
                TempData["Error"] = "Test SMS failed. Review Twilio settings and logs.";
            }

            return RedirectToAction(nameof(NotificationSettings));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTestEmail(CmsNotificationSettingsViewModel vm)
        {
            var email = (vm.TestEmailTo ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Please enter an email address for the email test.";
                return RedirectToAction(nameof(NotificationSettings));
            }

            var subject = "Medyx CMS Notification Test";
            var body = $"This is a test email from Medyx CMS Notification Settings at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.";

            try
            {
                await _emailNotificationProvider.SendAsync(email, subject, body);
                await RecordNotificationTestResultAsync("Email", "Success", email,
                    "Test email request submitted to provider.");
                TempData["Success"] = "Test email request submitted. Check inbox and SMTP logs/provider status.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Test email failed for address {Email}", email);
                await RecordNotificationTestResultAsync("Email", "Failed", email,
                    "Test email failed. Review SMTP settings and logs.");
                TempData["Error"] = "Test email failed. Review SMTP settings and logs.";
            }

            return RedirectToAction(nameof(NotificationSettings));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RunSmtpHealthCheck()
        {
            if (_smtpHealthService == null)
            {
                TempData["Error"] = "SMTP health service is unavailable.";
                return RedirectToAction(nameof(NotificationSettings));
            }

            var status = await _smtpHealthService.CheckAsync();
            if (status.IsConfigured && status.ConnectivityOk)
            {
                TempData["Success"] = $"SMTP health check passed: {status.Host}:{status.Port} is reachable.";
            }
            else
            {
                TempData["Error"] = "SMTP health check failed: " + string.Join(" | ", status.Issues);
            }

            return RedirectToAction(nameof(NotificationSettings));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearNotificationTestHistory()
        {
            var keys = new[]
            {
                "Notification:Test:Sms:LastStatus",
                "Notification:Test:Sms:LastMessage",
                "Notification:Test:Sms:LastTarget",
                "Notification:Test:Sms:LastAtUtc",
                "Notification:Test:Email:LastStatus",
                "Notification:Test:Email:LastMessage",
                "Notification:Test:Email:LastTarget",
                "Notification:Test:Email:LastAtUtc"
            };

            var settingsToRemove = await _db.Settings
                .Where(s => keys.Contains(s.Key))
                .ToListAsync();

            if (settingsToRemove.Count > 0)
            {
                _db.Settings.RemoveRange(settingsToRemove);
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = "Only notification test history was cleared. Actual patient/staff notification records were not removed.";
            return RedirectToAction(nameof(NotificationSettings));
        }

        public async Task<IActionResult> DeliveryLogs(
            string channel = null,
            string status = null,
            string recipient = null,
            bool? isTest = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            int page = 1,
            int pageSize = 20)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 5 or > 100 ? 20 : pageSize;

            var query = _db.NotificationDeliveryLogs
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(channel))
                query = query.Where(l => l.Channel == channel);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(l => l.Status == status);

            if (!string.IsNullOrWhiteSpace(recipient))
                query = query.Where(l => l.Recipient.Contains(recipient));

            if (isTest.HasValue)
                query = query.Where(l => l.IsTest == isTest.Value);

            if (dateFrom.HasValue)
            {
                var fromUtc = dateFrom.Value.Date;
                query = query.Where(l => l.CreatedAt >= fromUtc);
            }

            if (dateTo.HasValue)
            {
                var toUtcExclusive = dateTo.Value.Date.AddDays(1);
                query = query.Where(l => l.CreatedAt < toUtcExclusive);
            }

            var totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new NotificationDeliveryLogIndexViewModel
            {
                Logs = logs,
                ChannelFilter = channel,
                StatusFilter = status,
                RecipientFilter = recipient,
                IsTestFilter = isTest,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> DeliveryLogsExport(
            string format = "csv",
            string channel = null,
            string status = null,
            string recipient = null,
            bool? isTest = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            format = (format ?? "csv").Trim().ToLowerInvariant();
            if (format != "csv" && format != "pdf")
                return BadRequest("Only CSV and PDF exports are supported.");

            var query = _db.NotificationDeliveryLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(channel))
                query = query.Where(l => l.Channel == channel);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(l => l.Status == status);

            if (!string.IsNullOrWhiteSpace(recipient))
                query = query.Where(l => l.Recipient.Contains(recipient));

            if (isTest.HasValue)
                query = query.Where(l => l.IsTest == isTest.Value);

            if (dateFrom.HasValue)
                query = query.Where(l => l.CreatedAt >= dateFrom.Value.Date);

            if (dateTo.HasValue)
                query = query.Where(l => l.CreatedAt < dateTo.Value.Date.AddDays(1));

            var logs = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();

            var headers = new[] { "When (UTC)", "Channel", "Provider", "Recipient", "Status", "Type", "Subject", "Response" };
            var rows = logs.Select(l => (IReadOnlyList<string>)new[]
            {
                l.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                l.Channel ?? string.Empty,
                l.Provider ?? string.Empty,
                l.Recipient ?? string.Empty,
                l.Status ?? string.Empty,
                l.IsTest ? "Test" : "Production",
                l.Subject ?? string.Empty,
                l.ProviderResponse ?? string.Empty
            }).ToList();

            var title = "Notification Delivery Logs Export";
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            if (format == "csv")
            {
                var bytes = _exportService.BuildCsv(title, headers, rows);
                return File(bytes, "text/csv", $"delivery_logs_{stamp}.csv");
            }

            var pdfBytes = _exportService.BuildPdfTable(title, headers, rows);
            return File(pdfBytes, "application/pdf", $"delivery_logs_{stamp}.pdf");
        }

        // ─── Appointment Requests ─────────────────────────────────────────────

        public async Task<IActionResult> AppointmentRequests(
            string status = null, string search = null, DateTime? dateFrom = null, DateTime? dateTo = null,
            bool duplicatesOnly = false, int page = 1, int pageSize = 20)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 5 or > 100 ? 20 : pageSize;

            var query = _db.PublicAppointmentRequests
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(r => r.PatientName.Contains(search)
                                      || r.Phone.Contains(search)
                                      || (r.Patient != null && r.Patient.PatientId.Contains(search))
                                      || (r.Doctor != null && (r.Doctor.FirstName + " " + r.Doctor.LastName).Contains(search)));
            if (dateFrom.HasValue)
                query = query.Where(r => r.PreferredDate >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(r => r.PreferredDate <= dateTo.Value);
            if (duplicatesOnly)
            {
                query = query.Where(r => _db.PublicAppointmentRequests.Any(other =>
                    other.Id != r.Id
                    && other.Phone == r.Phone
                    && other.DoctorId == r.DoctorId
                    && other.PreferredDate == r.PreferredDate
                    && other.PreferredTime == r.PreferredTime));
            }

            var totalCount = await query.CountAsync();

            var requests = await query.OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var duplicateCounts = await GetDuplicateCountsAsync(requests);

            var vm = new PublicAppointmentRequestIndexViewModel
            {
                Requests = requests,
                RequestRows = requests.Select(r =>
                {
                    var key = BuildDuplicateKey(r.Phone, r.DoctorId, r.PreferredDate, r.PreferredTime);
                    duplicateCounts.TryGetValue(key, out var counts);

                    return new PublicAppointmentRequestListItemViewModel
                    {
                        Request = r,
                        DuplicateCount = counts.totalCount,
                        ActiveDuplicateCount = counts.activeCount
                    };
                }).ToList(),
                StatusFilter = status,
                SearchTerm = search,
                DateFrom = dateFrom,
                DateTo = dateTo,
                DuplicatesOnly = duplicatesOnly,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> AppointmentRequestsExport(
            string format = "csv",
            string status = null,
            string search = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            bool duplicatesOnly = false)
        {
            format = (format ?? "csv").Trim().ToLowerInvariant();
            if (format != "csv" && format != "pdf")
                return BadRequest("Only CSV and PDF exports are supported.");

            var query = _db.PublicAppointmentRequests
                .AsNoTracking()
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(r => r.PatientName.Contains(search)
                                      || r.Phone.Contains(search)
                                      || (r.Patient != null && r.Patient.PatientId.Contains(search))
                                      || (r.Doctor != null && (r.Doctor.FirstName + " " + r.Doctor.LastName).Contains(search)));
            if (dateFrom.HasValue)
                query = query.Where(r => r.PreferredDate >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(r => r.PreferredDate <= dateTo.Value);
            if (duplicatesOnly)
            {
                query = query.Where(r => _db.PublicAppointmentRequests.Any(other =>
                    other.Id != r.Id
                    && other.Phone == r.Phone
                    && other.DoctorId == r.DoctorId
                    && other.PreferredDate == r.PreferredDate
                    && other.PreferredTime == r.PreferredTime));
            }

            var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

            var headers = new[] { "Request #", "Patient", "Patient ID", "Phone", "Doctor", "Preferred Date", "Preferred Time", "Status", "Submitted (UTC)" };
            var rows = requests.Select(r => (IReadOnlyList<string>)new[]
            {
                r.Id.ToString(),
                r.PatientName ?? string.Empty,
                r.Patient?.PatientId ?? string.Empty,
                r.Phone ?? string.Empty,
                r.Doctor != null ? ($"Dr. {r.Doctor.FirstName} {r.Doctor.LastName}").Trim() : string.Empty,
                r.PreferredDate.ToString("yyyy-MM-dd"),
                DateTime.Today.Add(r.PreferredTime).ToString("HH:mm"),
                r.Status ?? string.Empty,
                r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            if (format == "csv")
            {
                var bytes = _exportService.BuildCsv("Public Appointment Requests Export", headers, rows);
                return File(bytes, "text/csv", $"appointment_requests_{stamp}.csv");
            }

            var pdfBytes = _exportService.BuildPdfTable("Public Appointment Requests Export", headers, rows);
            return File(pdfBytes, "application/pdf", $"appointment_requests_{stamp}.pdf");
        }

        public async Task<IActionResult> ReviewDuplicates(int id)
        {
            var request = await _db.PublicAppointmentRequests
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            var matches = await _db.PublicAppointmentRequests
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                .Where(r => r.Phone == request.Phone
                    && r.DoctorId == request.DoctorId
                    && r.PreferredDate == request.PreferredDate
                    && r.PreferredTime == request.PreferredTime)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var vm = new PublicAppointmentDuplicateReviewViewModel
            {
                PrimaryRequest = request,
                MatchingRequests = matches
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ReviewDuplicatesExport(int id, string format = "csv")
        {
            format = (format ?? "csv").Trim().ToLowerInvariant();
            if (format != "csv" && format != "pdf")
                return BadRequest("Only CSV and PDF exports are supported.");

            var request = await _db.PublicAppointmentRequests
                .AsNoTracking()
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            var matches = await _db.PublicAppointmentRequests
                .AsNoTracking()
                .Include(r => r.Patient)
                .Include(r => r.Doctor)
                .Where(r => r.Phone == request.Phone
                    && r.DoctorId == request.DoctorId
                    && r.PreferredDate == request.PreferredDate
                    && r.PreferredTime == request.PreferredTime)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var headers = new[] { "Request #", "Patient", "Patient ID", "Doctor", "Phone", "Status", "Submitted (UTC)", "Admin Notes" };
            var rows = matches.Select(r => (IReadOnlyList<string>)new[]
            {
                r.Id.ToString(),
                r.PatientName ?? string.Empty,
                r.Patient?.PatientId ?? string.Empty,
                r.Doctor != null ? ($"Dr. {r.Doctor.FirstName} {r.Doctor.LastName}").Trim() : string.Empty,
                r.Phone ?? string.Empty,
                r.Status ?? string.Empty,
                r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                r.AdminNotes ?? string.Empty
            }).ToList();

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var title = $"Duplicate Requests Export (#{id})";

            if (format == "csv")
            {
                var bytes = _exportService.BuildCsv(title, headers, rows);
                return File(bytes, "text/csv", $"duplicate_requests_{id}_{stamp}.csv");
            }

            var pdfBytes = _exportService.BuildPdfTable(title, headers, rows);
            return File(pdfBytes, "application/pdf", $"duplicate_requests_{id}_{stamp}.pdf");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRequestStatus(int id, string status, string adminNotes, bool returnToDuplicateReview = false)
        {
            var request = await _db.PublicAppointmentRequests.FindAsync(id);
            if (request == null) return NotFound();

            var previousStatus = request.Status;

            var allowedStatuses = new[] { "Pending", "Confirmed", "Cancelled" };
            if (!allowedStatuses.Contains(status))
            {
                TempData["Success"] = "Invalid status update requested.";
                if (returnToDuplicateReview)
                    return RedirectToAction(nameof(ReviewDuplicates), new { id });
                return RedirectToAction(nameof(AppointmentRequests));
            }

            var normalizedNotes = string.IsNullOrWhiteSpace(adminNotes)
                ? null
                : adminNotes.Trim();

            if (normalizedNotes != null && normalizedNotes.Length > 300)
                normalizedNotes = normalizedNotes[..300];

            request.Status = status;
            request.AdminNotes = normalizedNotes;
            request.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            if (!string.Equals(previousStatus, "Confirmed", StringComparison.OrdinalIgnoreCase)
                && string.Equals(status, "Confirmed", StringComparison.OrdinalIgnoreCase))
            {
                var doctorName = await _db.Doctors
                    .Where(d => d.Id == request.DoctorId)
                    .Select(d => "Dr. " + d.FirstName + " " + d.LastName)
                    .FirstOrDefaultAsync() ?? "Assigned Doctor";

                try
                {
                    await _publicBookingNotificationService.NotifyAppointmentConfirmedAsync(request, doctorName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Public booking confirmation notification failed for request {RequestId}",
                        request.Id);
                }
            }

            TempData["Success"] = $"Request #{id} marked as {status}.";

            if (returnToDuplicateReview)
                return RedirectToAction(nameof(ReviewDuplicates), new { id });

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

        private async Task<Dictionary<string, (int totalCount, int activeCount)>> GetDuplicateCountsAsync(
            System.Collections.Generic.IReadOnlyCollection<PublicAppointmentRequest> requests)
        {
            if (requests.Count == 0)
                return new Dictionary<string, (int totalCount, int activeCount)>();

            var phones = requests.Select(r => r.Phone).Distinct().ToList();
            var doctorIds = requests.Select(r => r.DoctorId).Distinct().ToList();
            var dates = requests.Select(r => r.PreferredDate).Distinct().ToList();
            var times = requests.Select(r => r.PreferredTime).Distinct().ToList();

            var counts = await _db.PublicAppointmentRequests
                .Where(r => phones.Contains(r.Phone)
                    && doctorIds.Contains(r.DoctorId)
                    && dates.Contains(r.PreferredDate)
                    && times.Contains(r.PreferredTime))
                .GroupBy(r => new { r.Phone, r.DoctorId, r.PreferredDate, r.PreferredTime })
                .Select(g => new
                {
                    g.Key.Phone,
                    g.Key.DoctorId,
                    g.Key.PreferredDate,
                    g.Key.PreferredTime,
                    TotalCount = g.Count(),
                    ActiveCount = g.Count(r => r.Status == "Pending" || r.Status == "Confirmed")
                })
                .ToListAsync();

            return counts.ToDictionary(
                x => BuildDuplicateKey(x.Phone, x.DoctorId, x.PreferredDate, x.PreferredTime),
                x => (x.TotalCount, x.ActiveCount));
        }

        private static string BuildDuplicateKey(string phone, int doctorId, DateTime preferredDate, TimeSpan preferredTime)
        {
            return string.Join("|", phone, doctorId, preferredDate.ToString("yyyy-MM-dd"), preferredTime.Ticks);
        }

        private async Task EnsurePageMenuItemAsync(CmsPage page)
        {
            var menuItem = await _db.CmsMenuItems.FirstOrDefaultAsync(m => m.CmsPageId == page.Id);
            var shouldPublishToMenu = page.ShowInMenu && string.Equals(page.Status, "Published", StringComparison.OrdinalIgnoreCase);

            if (!shouldPublishToMenu)
            {
                if (menuItem != null)
                {
                    _db.CmsMenuItems.Remove(menuItem);
                    await _db.SaveChangesAsync();
                }
                return;
            }

            if (menuItem == null)
            {
                menuItem = new CmsMenuItem
                {
                    CmsPageId = page.Id
                };
                _db.CmsMenuItems.Add(menuItem);
            }

            menuItem.Label = page.Title;
            menuItem.Url = $"/site/page/{page.Slug}";
            menuItem.SortOrder = page.SortOrder;
            menuItem.IsActive = true;
            menuItem.OpenInNewTab = false;

            await _db.SaveChangesAsync();
        }

        private async Task<string?> GetSettingValueAsync(string key)
        {
            var setting = await _db.Settings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == key);

            return setting?.Value;
        }

        private async Task UpsertSettingAsync(string key, string value, string type, string category, string description)
        {
            var setting = await _db.Settings.FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                _db.Settings.Add(new Setting
                {
                    Key = key,
                    Value = value,
                    Type = type,
                    Category = category,
                    Description = description,
                    IsSystem = false,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedBy = User?.Identity?.Name ?? string.Empty
                });
            }
            else
            {
                setting.Value = value;
                setting.Type = type;
                setting.Category = category;
                setting.Description = description;
                setting.ModifiedDate = DateTime.UtcNow;
                setting.ModifiedBy = User?.Identity?.Name ?? string.Empty;
            }

            await _db.SaveChangesAsync();
        }

        private DateTime? ParseNullableDateTime(string value)
        {
            if (DateTime.TryParse(value, out var parsed))
                return parsed;

            return null;
        }

        private async Task RecordNotificationTestResultAsync(string channel, string status, string target, string message)
        {
            var nowUtc = DateTime.UtcNow.ToString("O");
            await UpsertSettingAsync($"Notification:Test:{channel}:LastStatus", status, "string", "Notification", $"Latest {channel} test result status.");
            await UpsertSettingAsync($"Notification:Test:{channel}:LastMessage", message, "string", "Notification", $"Latest {channel} test result message.");
            await UpsertSettingAsync($"Notification:Test:{channel}:LastTarget", target, "string", "Notification", $"Latest {channel} test target.");
            await UpsertSettingAsync($"Notification:Test:{channel}:LastAtUtc", nowUtc, "string", "Notification", $"Latest {channel} test execution timestamp in UTC.");
        }
    }
}
