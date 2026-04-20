using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
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
        private const string BookingCaptchaSessionKey = "PublicBookingCaptchaExpected";

        private readonly ApplicationDbContext _db;
        private readonly ISettingService _settingService;
        private readonly ILogger<SiteController> _logger;

        public SiteController(ApplicationDbContext db, ISettingService settingService, ILogger<SiteController> logger)
        {
            _db = db;
            _settingService = settingService;
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
                HospitalTagline = "Compassionate Care, Advanced Medicine",
                ContactPhone = await _settingService.GetSettingValueAsync("PublicSitePhone") ?? "+000-000-0000",
                ContactEmail = await _settingService.GetSettingValueAsync("PublicSiteEmail") ?? "info@medyxhospital.com",
                Address = await _settingService.GetSettingValueAsync("PublicSiteAddress") ?? "Medyx Hospital, Main Road, Your City",
                MapEmbedUrl = await ResolveMapEmbedUrlAsync()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ContactUs()
        {
            var vm = new SiteContactViewModel
            {
                ContactPhone = await _settingService.GetSettingValueAsync("PublicSitePhone") ?? "+000-000-0000",
                ContactEmail = await _settingService.GetSettingValueAsync("PublicSiteEmail") ?? "info@medyxhospital.com",
                Address = await _settingService.GetSettingValueAsync("PublicSiteAddress") ?? "Medyx Hospital, Main Road, Your City",
                MapEmbedUrl = await ResolveMapEmbedUrlAsync(),
                MenuItems = await GetActiveMenuItemsAsync()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Careers()
        {
            var vm = new SiteCareersViewModel
            {
                CareersContent = await _settingService.GetSettingValueAsync("PublicSiteCareersContent")
                    ?? "We are always looking for dedicated professionals in nursing, diagnostics, administration, and patient support. Send your updated resume to the contact email below.",
                ContactEmail = await _settingService.GetSettingValueAsync("PublicSiteEmail") ?? "hr@medyxhospital.com",
                MenuItems = await GetActiveMenuItemsAsync()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Location()
        {
            var vm = new SiteContactViewModel
            {
                ContactPhone = await _settingService.GetSettingValueAsync("PublicSitePhone") ?? "+000-000-0000",
                ContactEmail = await _settingService.GetSettingValueAsync("PublicSiteEmail") ?? "info@medyxhospital.com",
                Address = await _settingService.GetSettingValueAsync("PublicSiteAddress") ?? "Medyx Hospital, Main Road, Your City",
                MapEmbedUrl = await ResolveMapEmbedUrlAsync(),
                MenuItems = await GetActiveMenuItemsAsync()
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

            SetBookingCaptchaChallenge(vm);
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

            if (!ModelState.IsValid)
            {
                SetBookingCaptchaChallenge(vm);
                return View(vm);
            }

            if (!ValidateBookingCaptcha(vm.CaptchaAnswer))
            {
                ModelState.AddModelError(nameof(vm.CaptchaAnswer), "Captcha validation failed. Please try again.");
                SetBookingCaptchaChallenge(vm);
                return View(vm);
            }

            HttpContext.Session.Remove(BookingCaptchaSessionKey);

            // Validate preferred date is in the future
            if (vm.PreferredDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(vm.PreferredDate), "Preferred date must be today or in the future.");
                SetBookingCaptchaChallenge(vm);
                return View(vm);
            }

            // Parse preferred time
            if (!TimeSpan.TryParse(vm.PreferredTimeStr, out var preferredTime))
            {
                ModelState.AddModelError(nameof(vm.PreferredTimeStr), "Invalid time format.");
                SetBookingCaptchaChallenge(vm);
                return View(vm);
            }

            var doctor = await _db.Doctors.FindAsync(vm.DoctorId);
            if (doctor == null)
            {
                ModelState.AddModelError(nameof(vm.DoctorId), "Selected doctor not found.");
                SetBookingCaptchaChallenge(vm);
                return View(vm);
            }

            var normalizedPhone = (vm.Phone ?? string.Empty).Trim();
            var duplicateRequestExists = await _db.PublicAppointmentRequests.AnyAsync(r =>
                r.Phone == normalizedPhone
                && r.DoctorId == vm.DoctorId
                && r.PreferredDate.Date == vm.PreferredDate.Date
                && r.PreferredTime == preferredTime
                && (r.Status == "Pending" || r.Status == "Confirmed"));

            if (duplicateRequestExists)
            {
                ModelState.AddModelError(string.Empty,
                    "An active appointment request already exists for this phone number, doctor, date, and time. Please choose another slot or contact the hospital.");
                SetBookingCaptchaChallenge(vm);
                return View(vm);
            }

            var patient = await FindOrCreatePatientFromBookingAsync(vm);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var request = new PublicAppointmentRequest
            {
                PatientName = vm.PatientName,
                Phone = vm.Phone,
                Email = vm.Email,
                Gender = vm.Gender,
                Age = vm.Age,
                PatientId = patient.Id,
                DoctorId = vm.DoctorId,
                PreferredDate = vm.PreferredDate,
                PreferredTime = preferredTime,
                Symptoms = vm.Symptoms,
                Notes = vm.Notes,
                Status = "Pending",
                AdminNotes = null,
                CreatedAt = DateTime.UtcNow,
                IpAddress = ipAddress
            };

            _db.PublicAppointmentRequests.Add(request);
            await _db.SaveChangesAsync();

            _logger.LogInformation("New public appointment request #{Id} for Dr. {Doctor} on {Date}",
                request.Id, $"{doctor.FirstName} {doctor.LastName}", request.PreferredDate.ToString("yyyy-MM-dd"));

            _logger.LogInformation("Public booking request #{RequestId} linked to patient {PatientId}",
                request.Id, patient.PatientId);

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

        private async Task<string> ResolveMapEmbedUrlAsync()
        {
            var configured = await _settingService.GetSettingValueAsync("PublicSiteMapEmbedUrl");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            var address = await _settingService.GetSettingValueAsync("PublicSiteAddress") ?? "Medyx Hospital";
            return $"https://www.google.com/maps?q={UrlEncoder.Default.Encode(address)}&output=embed";
        }

        private void SetBookingCaptchaChallenge(PublicBookingViewModel vm)
        {
            var left = Random.Shared.Next(1, 10);
            var right = Random.Shared.Next(1, 10);
            HttpContext.Session.SetInt32(BookingCaptchaSessionKey, left + right);
            vm.CaptchaQuestion = $"What is {left} + {right}?";
            vm.CaptchaAnswer = string.Empty;
        }

        private bool ValidateBookingCaptcha(string? answer)
        {
            var expected = HttpContext.Session.GetInt32(BookingCaptchaSessionKey);
            if (!expected.HasValue)
            {
                return false;
            }

            return int.TryParse(answer?.Trim(), out var actual) && actual == expected.Value;
        }

        private async Task<Patient> FindOrCreatePatientFromBookingAsync(PublicBookingViewModel vm)
        {
            var phone = (vm.Phone ?? string.Empty).Trim();
            var email = (vm.Email ?? string.Empty).Trim();

            var existingPatient = await _db.Patients.FirstOrDefaultAsync(p =>
                p.Phone == phone || (!string.IsNullOrWhiteSpace(email) && p.Email == email));

            if (existingPatient != null)
            {
                return existingPatient;
            }

            var (firstName, lastName) = SplitName(vm.PatientName);
            var estimatedDob = EstimateDateOfBirthFromAge(vm.Age);

            var patient = new Patient
            {
                PatientId = await GeneratePublicPatientIdAsync(),
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = phone,
                DateOfBirth = estimatedDob,
                Gender = string.IsNullOrWhiteSpace(vm.Gender) ? "Other" : vm.Gender,
                Address = string.Empty,
                City = string.Empty,
                State = string.Empty,
                Country = string.Empty,
                PostalCode = string.Empty,
                BloodGroup = string.Empty,
                EmergencyContactName = string.Empty,
                EmergencyContactPhone = string.Empty,
                EmergencyContactRelation = string.Empty,
                MedicalHistory = string.Empty,
                Allergies = string.Empty,
                GuardianName = string.Empty,
                GuardianPhone = string.Empty,
                MaritalStatus = string.Empty,
                Occupation = string.Empty,
                UserId = string.Empty,
                ProfileImagePath = string.Empty,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                LastVisitDate = vm.PreferredDate.Date
            };

            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();

            return patient;
        }

        private async Task<string> GeneratePublicPatientIdAsync()
        {
            string candidate;
            do
            {
                candidate = $"PAT{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(1000, 9999)}";
            }
            while (await _db.Patients.AnyAsync(p => p.PatientId == candidate));

            return candidate;
        }

        private static (string FirstName, string LastName) SplitName(string? fullName)
        {
            var safeName = (fullName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(safeName))
            {
                return ("Guest", "Patient");
            }

            var parts = safeName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return (parts[0], "Patient");
            }

            return (parts[0], string.Join(' ', parts.Skip(1)));
        }

        private static DateTime EstimateDateOfBirthFromAge(string? ageInput)
        {
            if (int.TryParse(ageInput, out var years) && years > 0 && years < 125)
            {
                return DateTime.Today.AddYears(-years);
            }

            return DateTime.Today.AddYears(-30);
        }
    }
}
