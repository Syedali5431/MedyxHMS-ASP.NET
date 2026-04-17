using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedyxHMS.Controllers
{
    /// <summary>
    /// Public-facing website controller — no authentication required.
    /// Serves CMS pages, notices/news, doctor listings, and the online appointment booking form.
    /// </summary>
    public class SiteController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SiteController> _logger;

        public SiteController(ApplicationDbContext db, ILogger<SiteController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ─── Homepage ─────────────────────────────────────────────────────────

        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;

            var vm = new SiteHomeViewModel
            {
                RecentNotices = await _db.CmsNotices
                    .Where(n => n.IsActive && n.Type == "Notice" && (n.PublishedAt == null || n.PublishedAt <= now))
                    .OrderByDescending(n => n.PublishedAt ?? n.CreatedAt)
                    .Take(5)
                    .ToListAsync(),

                RecentNews = await _db.CmsNotices
                    .Where(n => n.IsActive && n.Type == "News" && (n.PublishedAt == null || n.PublishedAt <= now))
                    .OrderByDescending(n => n.PublishedAt ?? n.CreatedAt)
                    .Take(3)
                    .ToListAsync(),

                UpcomingEvents = await _db.CmsNotices
                    .Where(n => n.IsActive && n.Type == "Event" && (n.PublishedAt == null || n.PublishedAt >= now))
                    .OrderBy(n => n.PublishedAt)
                    .Take(3)
                    .ToListAsync(),

                MenuItems = await GetActiveMenuItemsAsync(),
                HospitalName = "Medyx Hospital",
                HospitalTagline = "Compassionate Care, Advanced Medicine"
            };

            return View(vm);
        }

        // ─── CMS Page by Slug ────────────────────────────────────────────────

        [Route("site/page/{slug}")]
        public async Task<IActionResult> Page(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return NotFound();

            var page = await _db.CmsPages
                .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == "Published");

            if (page == null) return NotFound();

            var vm = new SitePageViewModel
            {
                Page = page,
                MenuItems = await GetActiveMenuItemsAsync()
            };
            return View(vm);
        }

        // ─── Notices / News / Events ─────────────────────────────────────────

        public async Task<IActionResult> Notices(string type = "Notice", string search = null, int page = 1, int pageSize = 9)
        {
            var now = DateTime.UtcNow;
            var validTypes = new[] { "Notice", "News", "Event", "Program" };
            if (!validTypes.Contains(type)) type = "Notice";

            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 6 or > 30 ? 9 : pageSize;

            var query = _db.CmsNotices
                .Where(n => n.IsActive && n.Type == type && (n.PublishedAt == null || n.PublishedAt <= now));

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(n => n.Title.Contains(search) || (n.Summary ?? string.Empty).Contains(search));

            var totalCount = await query.CountAsync();

            var vm = new SiteNoticeListViewModel
            {
                Notices = await query
                    .OrderByDescending(n => n.PublishedAt ?? n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(),
                Type = type,
                PageTitle = type == "News" ? "Latest News"
                          : type == "Event" ? "Upcoming Events"
                          : type == "Program" ? "Programs"
                          : "Notices",
                SearchTerm = search,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                MenuItems = await GetActiveMenuItemsAsync()
            };
            return View(vm);
        }

        [Route("site/notice/{slug}")]
        public async Task<IActionResult> NoticeDetail(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return NotFound();

            var now = DateTime.UtcNow;
            var notice = await _db.CmsNotices
                .FirstOrDefaultAsync(n => n.Slug == slug && n.IsActive && (n.PublishedAt == null || n.PublishedAt <= now));

            if (notice == null) return NotFound();

            ViewBag.MenuItems = await GetActiveMenuItemsAsync();
            return View(notice);
        }

        // ─── Doctor Listing ───────────────────────────────────────────────────

        public async Task<IActionResult> Doctors(int? departmentId = null)
        {
            var doctorsQuery = _db.Doctors
                .Include(d => d.Department)
                .Where(d => d.IsActive);

            if (departmentId.HasValue)
                doctorsQuery = doctorsQuery.Where(d => d.DepartmentId == departmentId.Value);

            var doctors = await doctorsQuery.OrderBy(d => d.Department).ThenBy(d => d.FirstName).ToListAsync();

            var shiftsByDoctor = await _db.DoctorShifts
                .Where(s => s.IsActive && doctors.Select(d => d.Id).Contains(s.DoctorId))
                .ToListAsync();

            var vm = new SiteDoctorListViewModel
            {
                Doctors = doctors.Select(d => new DoctorWithShifts
                {
                    Doctor = d,
                    Shifts = shiftsByDoctor.Where(s => s.DoctorId == d.Id).ToList()
                }).ToList(),
                Departments = await _db.Departments.OrderBy(d => d.Name).ToListAsync(),
                DepartmentFilter = departmentId,
                MenuItems = await GetActiveMenuItemsAsync()
            };
            return View(vm);
        }

        // ─── Book Appointment ────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BookAppointment(int? doctorId = null)
        {
            var vm = new PublicBookingViewModel
            {
                DoctorId = doctorId ?? 0,
                AvailableDoctors = await _db.Doctors
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.FirstName)
                    .ToListAsync(),
                MenuItems = await GetActiveMenuItemsAsync()
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(PublicBookingViewModel vm)
        {
            // Honeypot check — bots fill this field, humans don't see it
            if (!string.IsNullOrEmpty(vm.Website))
            {
                // Silent reject — appear to succeed
                return RedirectToAction(nameof(BookingConfirmation), new { requestId = 0 });
            }

            vm.AvailableDoctors = await _db.Doctors.Where(d => d.IsActive).OrderBy(d => d.FirstName).ToListAsync();
            vm.MenuItems = await GetActiveMenuItemsAsync();

            if (!ModelState.IsValid) return View(vm);

            // Validate preferred date is in the future
            if (vm.PreferredDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(vm.PreferredDate), "Preferred date must be today or in the future.");
                return View(vm);
            }

            // Parse preferred time
            if (!TimeSpan.TryParse(vm.PreferredTimeStr, out var preferredTime))
            {
                ModelState.AddModelError(nameof(vm.PreferredTimeStr), "Invalid time format.");
                return View(vm);
            }

            var doctor = await _db.Doctors.FindAsync(vm.DoctorId);
            if (doctor == null)
            {
                ModelState.AddModelError(nameof(vm.DoctorId), "Selected doctor not found.");
                return View(vm);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var request = new PublicAppointmentRequest
            {
                PatientName = vm.PatientName,
                Phone = vm.Phone,
                Email = vm.Email,
                Gender = vm.Gender,
                Age = vm.Age,
                DoctorId = vm.DoctorId,
                PreferredDate = vm.PreferredDate,
                PreferredTime = preferredTime,
                Symptoms = vm.Symptoms,
                Notes = vm.Notes,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                IpAddress = ipAddress
            };

            _db.PublicAppointmentRequests.Add(request);
            await _db.SaveChangesAsync();

            _logger.LogInformation("New public appointment request #{Id} for Dr. {Doctor} on {Date}",
                request.Id, $"{doctor.FirstName} {doctor.LastName}", request.PreferredDate.ToString("yyyy-MM-dd"));

            return RedirectToAction(nameof(BookingConfirmation), new { requestId = request.Id });
        }

        public async Task<IActionResult> BookingConfirmation(int requestId)
        {
            if (requestId == 0)
            {
                // Honeypot rejection — show generic confirmation
                return View(new BookingConfirmationViewModel
                {
                    PatientName = "Guest",
                    DoctorName = "N/A",
                    PreferredDate = DateTime.Today,
                    PreferredTime = "TBD",
                    RequestId = 0,
                    MenuItems = await GetActiveMenuItemsAsync()
                });
            }

            var request = await _db.PublicAppointmentRequests
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return NotFound();

            var vm = new BookingConfirmationViewModel
            {
                PatientName = request.PatientName,
                DoctorName = request.Doctor != null
                    ? $"Dr. {request.Doctor.FirstName} {request.Doctor.LastName}"
                    : "N/A",
                PreferredDate = request.PreferredDate,
                PreferredTime = DateTime.Today.Add(request.PreferredTime).ToString("hh:mm tt"),
                RequestId = request.Id,
                MenuItems = await GetActiveMenuItemsAsync()
            };
            return View(vm);
        }

        // ─── AJAX: Available Slots ────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(int doctorId, string date)
        {
            if (!DateTime.TryParse(date, out var selectedDate))
                return BadRequest("Invalid date.");

            var dayOfWeek = (int)selectedDate.DayOfWeek;

            var shift = await _db.DoctorShifts
                .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.DayOfWeek == dayOfWeek && s.IsActive);

            if (shift == null)
                return Json(new List<AvailableSlotDto>());

            // Get already-booked times for this doctor/date
            var bookedTimes = await _db.PublicAppointmentRequests
                .Where(r => r.DoctorId == doctorId
                         && r.PreferredDate.Date == selectedDate.Date
                         && r.Status != "Cancelled")
                .Select(r => r.PreferredTime)
                .ToListAsync();

            var slots = new List<AvailableSlotDto>();
            var current = shift.StartTime;

            while (current + TimeSpan.FromMinutes(shift.SlotDurationMinutes) <= shift.EndTime)
            {
                var bookedCount = bookedTimes.Count(t => t == current);
                if (bookedCount < shift.MaxPatientsPerSlot)
                {
                    slots.Add(new AvailableSlotDto
                    {
                        Time = current.ToString(@"hh\:mm"),
                        Display = DateTime.Today.Add(current).ToString("hh:mm tt")
                    });
                }
                current = current.Add(TimeSpan.FromMinutes(shift.SlotDurationMinutes));
            }

            return Json(slots);
        }

        // ─── Helper ───────────────────────────────────────────────────────────

        private Task<List<CmsMenuItem>> GetActiveMenuItemsAsync() =>
            _db.CmsMenuItems
               .Where(m => m.IsActive)
               .OrderBy(m => m.SortOrder)
               .ToListAsync();
    }
}
