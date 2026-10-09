using System.Globalization;
using System.Security.Claims;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Purpose: Operation theatre bookings (with clash prevention and OT billing), theatre availability,
// and the theatre master with block times. Clinical staff book and run cases; Admin/SuperAdmin manage theatres.
namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Staff,Doctor,Nurse")]
    public class OperationTheatreController : Controller
    {
        private const string ManageRoles = AppRoles.Managers;
        private readonly IOperationTheatreService _operationTheatreService;
        private readonly IPatientService _patientService;
        private readonly IAuditService _auditService;
        private readonly ApplicationDbContext _context;
        private readonly IHospitalContext _hospitalContext;

        public OperationTheatreController(IOperationTheatreService operationTheatreService, IPatientService patientService, IAuditService auditService,
            ApplicationDbContext context, IHospitalContext hospitalContext)
        {
            _operationTheatreService = operationTheatreService;
            _patientService = patientService;
            _auditService = auditService;
            _context = context;
            _hospitalContext = hospitalContext;
        }

        private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        private string UserName => User.Identity?.Name ?? string.Empty;
        private bool IsManager => User.IsInRole("SuperAdmin") || User.IsInRole("Admin");

        // ── Bookings ────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? from, DateTime? to, int? theatreId, string? status)
        {
            var query = _context.OTSchedules.AsNoTracking().Include(o => o.Patient).Include(o => o.Theatre).AsQueryable();
            if (from.HasValue) query = query.Where(o => o.ScheduledDate >= from.Value.Date);
            if (to.HasValue) query = query.Where(o => o.ScheduledDate < to.Value.Date.AddDays(1));
            if (theatreId.HasValue) query = query.Where(o => o.OperationTheatreId == theatreId);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status);

            var today = DateTime.Today;
            ViewBag.Theatres = await _context.OperationTheatres.AsNoTracking().OrderBy(t => t.Code).ToListAsync();
            ViewBag.TodayCount = await _context.OTSchedules.CountAsync(o => o.ScheduledDate >= today && o.ScheduledDate < today.AddDays(1) && o.Status != OperationTheatreService.Cancelled);
            ViewBag.InProgressCount = await _context.OTSchedules.CountAsync(o => o.Status == OperationTheatreService.InProgress);
            ViewBag.UpcomingCount = await _context.OTSchedules.CountAsync(o => o.ScheduledDate >= DateTime.Now && o.Status == OperationTheatreService.Scheduled);
            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.TheatreId = theatreId;
            ViewBag.Status = status;
            return View(await query.OrderByDescending(o => o.ScheduledDate).ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var schedule = await _operationTheatreService.GetScheduleByIdAsync(id);
            if (schedule == null) return NotFound();
            ViewBag.History = await _context.AuditLogs.AsNoTracking()
                .Where(a => a.EntityName == "OTSchedule" && a.EntityId == id.ToString())
                .OrderBy(a => a.Timestamp)
                .Select(a => new { a.Action, a.NewValues, a.Timestamp, User = a.User != null ? a.User.UserName : "" })
                .ToListAsync();
            return View(schedule);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? theatreId, string? start, int? duration, int? patientId)
        {
            var input = new OTBookingInput
            {
                PatientId = patientId ?? 0,
                EstimatedDurationMinutes = duration is >= OperationTheatreService.MinDuration and <= OperationTheatreService.MaxDuration ? duration.Value : 60
            };

            if (theatreId.HasValue && DateTime.TryParse(start, CultureInfo.InvariantCulture, DateTimeStyles.None, out var requested))
            {
                input.OperationTheatreId = theatreId.Value;
                input.ScheduledDate = requested;
            }
            else
            {
                // Propose the next free slot that fits the duration.
                var slot = await NextFreeSlotAsync(input.EstimatedDurationMinutes, theatreId);
                input.OperationTheatreId = slot?.TheatreId ?? theatreId ?? 0;
                input.ScheduledDate = slot?.Start ?? NextQuarterHour(DateTime.Now.AddHours(1));
            }

            await LoadFormAsync();
            return View(input);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OTBookingInput input)
        {
            var schedule = new OTSchedule { Status = OperationTheatreService.Scheduled };
            await ValidateAndApplyAsync(input, schedule, isNew: true);
            if (!ModelState.IsValid)
            {
                await LoadFormAsync(input);
                return View(input);
            }

            try
            {
                var created = await _operationTheatreService.CreateScheduleAsync(schedule);
                await _auditService.LogActivityAsync(UserId, "CREATE", "OTSchedule", created.Id.ToString(), null,
                    $"Patient: {created.PatientId}, Procedure: {created.ProcedureName}, Theatre: {created.OperationTheatreNumber}, {created.ScheduledDate:yyyy-MM-dd HH:mm} for {created.EstimatedDurationMinutes} min, Surgeon: {created.SurgeonName}{(created.IsEmergency ? ", emergency" : "")}");

                TempData["SuccessMessage"] = $"OT booked: {created.OperationTheatreNumber} on {created.ScheduledDate:dd-MMM-yyyy HH:mm}; billing entry generated.";
                return RedirectToAction(nameof(Details), new { id = created.Id });
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "The booking could not be saved. Please try again.");
                await LoadFormAsync(input);
                return View(input);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var schedule = await _context.OTSchedules.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
            if (schedule == null) return NotFound();
            if (schedule.Status != OperationTheatreService.Scheduled)
            {
                TempData["ErrorMessage"] = "Only a scheduled booking can be rescheduled.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var input = new OTBookingInput
            {
                Id = schedule.Id,
                PatientId = schedule.PatientId,
                ProcedureName = schedule.ProcedureName,
                SurgeonDoctorId = schedule.SurgeonDoctorId,
                SurgeonName = schedule.SurgeonDoctorId.HasValue ? null : schedule.SurgeonName,
                OperationTheatreId = schedule.OperationTheatreId ?? 0,
                ScheduledDate = schedule.ScheduledDate,
                EstimatedDurationMinutes = schedule.EstimatedDurationMinutes,
                IsEmergency = schedule.IsEmergency,
                Notes = schedule.Notes
            };
            await LoadFormAsync(input);
            return View("Create", input);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OTBookingInput input)
        {
            var schedule = await _context.OTSchedules.FirstOrDefaultAsync(o => o.Id == id);
            if (schedule == null) return NotFound();
            if (schedule.Status != OperationTheatreService.Scheduled)
            {
                TempData["ErrorMessage"] = "Only a scheduled booking can be rescheduled.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // The patient of a booking (and of its bill) does not change.
            input.Id = id;
            input.PatientId = schedule.PatientId;
            var before = $"{schedule.OperationTheatreNumber} {schedule.ScheduledDate:yyyy-MM-dd HH:mm} {schedule.EstimatedDurationMinutes} min, {schedule.SurgeonName}";
            await ValidateAndApplyAsync(input, schedule, isNew: false);
            if (!ModelState.IsValid)
            {
                await LoadFormAsync(input);
                return View("Create", input);
            }

            var note = await _operationTheatreService.UpdateScheduleAsync(schedule);
            await _auditService.LogActivityAsync(UserId, "RESCHEDULE", "OTSchedule", id.ToString(), before,
                $"{schedule.OperationTheatreNumber} {schedule.ScheduledDate:yyyy-MM-dd HH:mm} {schedule.EstimatedDurationMinutes} min, {schedule.SurgeonName}.{note}");
            TempData["SuccessMessage"] = $"Booking updated: {schedule.OperationTheatreNumber} on {schedule.ScheduledDate:dd-MMM-yyyy HH:mm}.{note}";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? reason, string? returnTo)
        {
            var (ok, message) = await _operationTheatreService.ChangeStatusAsync(id, status, reason);
            if (ok)
            {
                await _auditService.LogActivityAsync(UserId, status == OperationTheatreService.Cancelled ? "CANCEL" : "STATUS", "OTSchedule", id.ToString(), null,
                    $"{status}{(string.IsNullOrWhiteSpace(reason) ? "" : ": " + reason.Trim())}.{message.Replace("OT booking cancelled.", string.Empty)}".TrimEnd('.') + ".");
            }
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = message;
            return returnTo == "index" ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { id });
        }

        // ── Availability ────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Availability(DateTime? date, int duration = 60)
        {
            var day = (date ?? DateTime.Today).Date;
            duration = Math.Clamp(duration, OperationTheatreService.MinDuration, OperationTheatreService.MaxDuration);
            var theatres = await _operationTheatreService.GetDayAvailabilityAsync(day);

            var weekDays = Enumerable.Range(0, 7).Select(i => day.AddDays(i)).ToList();
            var week = theatres.ToDictionary(t => t.Theatre.Id, _ => new List<(int Cases, int Booked, int Open)>());
            foreach (var d in weekDays)
            {
                var dayTheatres = d == day ? theatres : await _operationTheatreService.GetDayAvailabilityAsync(d);
                foreach (var t in dayTheatres.Where(t => week.ContainsKey(t.Theatre.Id)))
                {
                    week[t.Theatre.Id].Add((t.Cases.Count, t.BookedMinutes, t.OpenMinutes));
                }
            }

            // The time grid covers the opening hours of all theatres and any case outside them (emergencies).
            var gridFrom = theatres.Count > 0 ? theatres.Min(t => t.OpenFrom) : day.AddHours(8);
            var gridUntil = theatres.Count > 0 ? theatres.Max(t => t.OpenUntil) : day.AddHours(20);
            foreach (var c in theatres.SelectMany(t => t.Cases))
            {
                if (c.ScheduledDate < gridFrom) gridFrom = c.ScheduledDate < day ? day : c.ScheduledDate;
                if (c.EndsAt > gridUntil) gridUntil = c.EndsAt > day.AddDays(1) ? day.AddDays(1) : c.EndsAt;
            }
            gridFrom = gridFrom.Date.AddHours(gridFrom.Hour);
            gridUntil = gridUntil.Minute == 0 ? gridUntil : gridUntil.Date.AddHours(gridUntil.Hour + 1);

            return View(new OTAvailabilityViewModel
            {
                Date = day,
                Duration = duration,
                Theatres = theatres,
                WeekDays = weekDays,
                Week = week,
                GridFrom = gridFrom,
                GridUntil = gridUntil
            });
        }

        // ── Theatre master ──────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Theatres()
        {
            var now = DateTime.Now;
            var theatres = await _context.OperationTheatres.AsNoTracking().OrderBy(t => t.Code).ToListAsync();
            var ids = theatres.Select(t => t.Id).ToList();
            ViewBag.Upcoming = await _context.OTSchedules.AsNoTracking()
                .Where(o => o.OperationTheatreId != null && ids.Contains(o.OperationTheatreId.Value) && o.Status == OperationTheatreService.Scheduled && o.ScheduledDate >= now)
                .GroupBy(o => o.OperationTheatreId!.Value).Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count);
            ViewBag.Blocks = await _context.OTBlocks.AsNoTracking()
                .Where(b => ids.Contains(b.OperationTheatreId) && b.EndsAt > now)
                .GroupBy(b => b.OperationTheatreId).Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count);
            ViewBag.CanManage = IsManager;
            return View(theatres);
        }

        [HttpGet]
        public async Task<IActionResult> TheatreDetails(int id)
        {
            var theatre = await _context.OperationTheatres.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (theatre == null) return NotFound();
            var now = DateTime.Now;
            ViewBag.Blocks = await _context.OTBlocks.AsNoTracking().Where(b => b.OperationTheatreId == id && b.EndsAt > now).OrderBy(b => b.StartsAt).ToListAsync();
            ViewBag.Cases = await _context.OTSchedules.AsNoTracking().Include(o => o.Patient)
                .Where(o => o.OperationTheatreId == id && o.ScheduledDate >= now.Date && o.ScheduledDate < now.Date.AddDays(14) && o.Status != OperationTheatreService.Cancelled)
                .OrderBy(o => o.ScheduledDate).ToListAsync();
            ViewBag.CanManage = IsManager;
            return View(theatre);
        }

        [HttpGet]
        [Authorize(Roles = ManageRoles)]
        public IActionResult TheatreCreate()
        {
            ViewBag.Types = OperationTheatreService.TheatreTypes;
            return View("TheatreForm", new OperationTheatre());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ManageRoles)]
        public async Task<IActionResult> TheatreCreate([Bind("Code,Name,Location,TheatreType,Features,OpensAt,ClosesAt,Is24Hours,TurnoverMinutes")] OperationTheatre input)
        {
            await ValidateTheatreAsync(input, null);
            if (!ModelState.IsValid)
            {
                ViewBag.Types = OperationTheatreService.TheatreTypes;
                return View("TheatreForm", input);
            }

            input.IsActive = true;
            input.CreatedDate = DateTime.Now;
            _context.OperationTheatres.Add(input);
            await _context.SaveChangesAsync();
            await _auditService.LogActivityAsync(UserId, "CREATE", "OperationTheatre", input.Id.ToString(), null, TheatreSummary(input));
            TempData["SuccessMessage"] = $"Theatre {input.Code} added.";
            return RedirectToAction(nameof(TheatreDetails), new { id = input.Id });
        }

        [HttpGet]
        [Authorize(Roles = ManageRoles)]
        public async Task<IActionResult> TheatreEdit(int id)
        {
            var theatre = await _context.OperationTheatres.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (theatre == null) return NotFound();
            ViewBag.Types = OperationTheatreService.TheatreTypes;
            return View("TheatreForm", theatre);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ManageRoles)]
        public async Task<IActionResult> TheatreEdit(int id, [Bind("Code,Name,Location,TheatreType,Features,OpensAt,ClosesAt,Is24Hours,TurnoverMinutes")] OperationTheatre input)
        {
            var theatre = await _context.OperationTheatres.FirstOrDefaultAsync(t => t.Id == id);
            if (theatre == null) return NotFound();
            input.Id = id;
            input.HospitalId = theatre.HospitalId;
            input.IsActive = theatre.IsActive;
            await ValidateTheatreAsync(input, theatre);
            if (!ModelState.IsValid)
            {
                ViewBag.Types = OperationTheatreService.TheatreTypes;
                return View("TheatreForm", input);
            }

            var old = TheatreSummary(theatre);
            var codeChanged = theatre.Code != input.Code;
            theatre.Code = input.Code;
            theatre.Name = input.Name;
            theatre.Location = input.Location ?? string.Empty;
            theatre.TheatreType = input.TheatreType;
            theatre.Features = input.Features ?? string.Empty;
            theatre.OpensAt = input.OpensAt;
            theatre.ClosesAt = input.ClosesAt;
            theatre.Is24Hours = input.Is24Hours;
            theatre.TurnoverMinutes = input.TurnoverMinutes;

            // Bookings keep showing the theatre code.
            if (codeChanged)
            {
                foreach (var booking in await _context.OTSchedules.IgnoreQueryFilters().Where(o => o.OperationTheatreId == id).ToListAsync())
                {
                    booking.OperationTheatreNumber = theatre.Code;
                }
            }
            await _context.SaveChangesAsync();
            await _auditService.LogActivityAsync(UserId, "UPDATE", "OperationTheatre", id.ToString(), old, TheatreSummary(theatre));
            TempData["SuccessMessage"] = $"Theatre {theatre.Code} updated.";
            return RedirectToAction(nameof(TheatreDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ManageRoles)]
        public async Task<IActionResult> ToggleTheatre(int id)
        {
            var theatre = await _context.OperationTheatres.FirstOrDefaultAsync(t => t.Id == id);
            if (theatre == null) return NotFound();
            if (theatre.IsActive)
            {
                var upcoming = await _context.OTSchedules.IgnoreQueryFilters()
                    .CountAsync(o => o.OperationTheatreId == id && (o.Status == OperationTheatreService.InProgress || (o.Status == OperationTheatreService.Scheduled && o.ScheduledDate >= DateTime.Now)));
                if (upcoming > 0)
                {
                    TempData["ErrorMessage"] = $"{theatre.Code} has {upcoming} upcoming or running booking(s) – reschedule or cancel them before deactivating the theatre.";
                    return RedirectToAction(nameof(TheatreDetails), new { id });
                }
            }

            theatre.IsActive = !theatre.IsActive;
            await _context.SaveChangesAsync();
            await _auditService.LogActivityAsync(UserId, theatre.IsActive ? "ACTIVATE" : "DEACTIVATE", "OperationTheatre", id.ToString(), null, theatre.Code);
            TempData["SuccessMessage"] = $"Theatre {theatre.Code} {(theatre.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(TheatreDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ManageRoles)]
        public async Task<IActionResult> AddBlock(int id, DateTime? startsAt, DateTime? endsAt, string? reason)
        {
            var theatre = await _context.OperationTheatres.FirstOrDefaultAsync(t => t.Id == id);
            if (theatre == null) return NotFound();

            string? error = !startsAt.HasValue || !endsAt.HasValue ? "Enter the start and end of the block."
                : endsAt <= startsAt ? "The end must be after the start."
                : endsAt <= DateTime.Now ? "The block is already over."
                : (endsAt.Value - startsAt.Value).TotalDays > 31 ? "A block can last at most 31 days – deactivate the theatre for longer closures."
                : string.IsNullOrWhiteSpace(reason) ? "Give the reason (e.g. maintenance, deep cleaning)." : null;
            if (error == null)
            {
                var clashes = await _context.OTSchedules.IgnoreQueryFilters().AsNoTracking()
                    .Where(o => o.OperationTheatreId == id && (o.Status == OperationTheatreService.Scheduled || o.Status == OperationTheatreService.InProgress)
                                && o.ScheduledDate < endsAt && o.ScheduledDate > startsAt!.Value.AddMinutes(-OperationTheatreService.MaxDuration))
                    .ToListAsync();
                clashes = clashes.Where(o => OperationTheatreService.OccupiedUntil(o, DateTime.Now) > startsAt!.Value).ToList();
                if (clashes.Count > 0)
                {
                    error = "These bookings fall in that period – reschedule or cancel them first: "
                            + string.Join("; ", clashes.Select(o => $"{o.ScheduledDate:dd-MMM HH:mm} {o.ProcedureName}")) + ".";
                }
            }
            if (error != null)
            {
                TempData["ErrorMessage"] = error;
                return RedirectToAction(nameof(TheatreDetails), new { id });
            }

            var block = new OTBlock { OperationTheatreId = id, StartsAt = startsAt!.Value, EndsAt = endsAt!.Value, Reason = reason!.Trim(), CreatedBy = UserName, CreatedAt = DateTime.Now };
            _context.OTBlocks.Add(block);
            await _context.SaveChangesAsync();
            await _auditService.LogActivityAsync(UserId, "BLOCK", "OperationTheatre", id.ToString(), null, $"{theatre.Code} blocked {block.StartsAt:yyyy-MM-dd HH:mm}–{block.EndsAt:yyyy-MM-dd HH:mm}: {block.Reason}");
            TempData["SuccessMessage"] = $"{theatre.Code} blocked {block.StartsAt:dd-MMM HH:mm}–{block.EndsAt:dd-MMM HH:mm}.";
            return RedirectToAction(nameof(TheatreDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ManageRoles)]
        public async Task<IActionResult> RemoveBlock(int id, int blockId)
        {
            var block = await _context.OTBlocks.Include(b => b.Theatre).FirstOrDefaultAsync(b => b.Id == blockId && b.OperationTheatreId == id);
            if (block == null) return NotFound();
            _context.OTBlocks.Remove(block);
            await _context.SaveChangesAsync();
            await _auditService.LogActivityAsync(UserId, "UNBLOCK", "OperationTheatre", id.ToString(), $"{block.StartsAt:yyyy-MM-dd HH:mm}–{block.EndsAt:yyyy-MM-dd HH:mm}: {block.Reason}", null);
            TempData["SuccessMessage"] = "Block removed.";
            return RedirectToAction(nameof(TheatreDetails), new { id });
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private async Task ValidateAndApplyAsync(OTBookingInput input, OTSchedule schedule, bool isNew)
        {
            if (isNew && input.PatientId > 0 && !await _context.Patients.AnyAsync(p => p.Id == input.PatientId))
                ModelState.AddModelError(nameof(input.PatientId), "Choose the patient.");

            var visitingSurgeon = input.SurgeonName?.Trim() ?? string.Empty;
            if (input.SurgeonDoctorId.HasValue)
            {
                if (!await _context.Doctors.AnyAsync(d => d.Id == input.SurgeonDoctorId.Value && d.IsActive))
                    ModelState.AddModelError(nameof(input.SurgeonDoctorId), "Choose a surgeon from the list.");
                visitingSurgeon = string.Empty;
            }
            else if (visitingSurgeon.Length == 0)
            {
                ModelState.AddModelError(nameof(input.SurgeonDoctorId), "Choose the surgeon, or enter the name of a visiting surgeon.");
            }

            var now = DateTime.Now;
            if (input.ScheduledDate == default)
                ModelState.AddModelError(nameof(input.ScheduledDate), "Enter the start date and time.");
            else if (input.ScheduledDate < now.AddMinutes(input.IsEmergency ? -60 : -5))
                ModelState.AddModelError(nameof(input.ScheduledDate), "The start time is in the past.");
            else if (input.ScheduledDate > now.AddYears(1))
                ModelState.AddModelError(nameof(input.ScheduledDate), "Bookings can be made up to one year ahead.");

            schedule.PatientId = isNew ? input.PatientId : schedule.PatientId;
            schedule.ProcedureName = input.ProcedureName?.Trim() ?? string.Empty;
            schedule.SurgeonDoctorId = input.SurgeonDoctorId;
            schedule.SurgeonName = visitingSurgeon;
            schedule.OperationTheatreId = input.OperationTheatreId > 0 ? input.OperationTheatreId : null;
            schedule.ScheduledDate = input.ScheduledDate;
            schedule.EstimatedDurationMinutes = input.EstimatedDurationMinutes;
            schedule.IsEmergency = input.IsEmergency;
            schedule.Notes = input.Notes?.Trim() ?? string.Empty;

            if (!ModelState.IsValid)
                return;

            if (schedule.SurgeonDoctorId.HasValue)
            {
                var doctor = await _context.Doctors.AsNoTracking().FirstAsync(d => d.Id == schedule.SurgeonDoctorId.Value);
                schedule.SurgeonName = OperationTheatreService.SurgeonDisplayName(doctor);
            }

            var problems = await _operationTheatreService.FindConflictsAsync(schedule, isNew ? null : schedule.Id);
            foreach (var problem in problems)
                ModelState.AddModelError(string.Empty, problem);

            if (problems.Count > 0 && schedule.OperationTheatreId.HasValue)
            {
                // Suggest free times in the chosen theatre on that day (and the next free slot anywhere).
                var day = (await _operationTheatreService.GetDayAvailabilityAsync(input.ScheduledDate.Date, schedule.OperationTheatreId)).FirstOrDefault();
                ViewBag.Suggestions = day?.Free.Where(f => f.LongestCaseMinutes >= input.EstimatedDurationMinutes).ToList() ?? new List<OTFreeWindow>();
                ViewBag.SuggestionTheatre = day?.Theatre;
                ViewBag.NextSlot = await NextFreeSlotAsync(input.EstimatedDurationMinutes, null, input.ScheduledDate > now ? input.ScheduledDate.Date : now.Date);
            }
        }

        private async Task<(int TheatreId, string TheatreCode, DateTime Start)?> NextFreeSlotAsync(int duration, int? theatreId, DateTime? fromDay = null)
        {
            var first = (fromDay ?? DateTime.Today).Date;
            for (var i = 0; i < 14; i++)
            {
                var days = await _operationTheatreService.GetDayAvailabilityAsync(first.AddDays(i), theatreId);
                var best = days.SelectMany(d => d.Free.Where(f => f.LongestCaseMinutes >= duration).Select(f => (d.Theatre, f.From)))
                    .OrderBy(x => x.From).ThenBy(x => x.Theatre.Code).FirstOrDefault();
                if (best.Theatre != null) return (best.Theatre.Id, best.Theatre.Code, best.From);
            }
            return null;
        }

        private static DateTime NextQuarterHour(DateTime t) =>
            new DateTime(t.Year, t.Month, t.Day, t.Hour, 0, 0).AddMinutes(15 * Math.Ceiling(t.Minute / 15.0));

        private async Task LoadFormAsync(OTBookingInput? input = null)
        {
            ViewBag.Patients = await _patientService.GetAllPatientsAsync();
            ViewBag.Doctors = await _context.Doctors.AsNoTracking().Where(d => d.IsActive || (input != null && d.Id == input.SurgeonDoctorId))
                .OrderBy(d => d.FirstName).ThenBy(d => d.LastName).ToListAsync();
            ViewBag.Theatres = await _context.OperationTheatres.AsNoTracking().Where(t => t.IsActive || (input != null && t.Id == input.OperationTheatreId))
                .OrderBy(t => t.Code).ToListAsync();
        }

        private async Task ValidateTheatreAsync(OperationTheatre input, OperationTheatre? existing)
        {
            input.Code = (input.Code ?? string.Empty).Trim().ToUpperInvariant();
            input.Name = (input.Name ?? string.Empty).Trim();
            if (input.Code.Length == 0) ModelState.AddModelError(nameof(input.Code), "Enter the theatre code (e.g. OT-1).");
            if (input.Name.Length == 0) ModelState.AddModelError(nameof(input.Name), "Enter the theatre name.");
            if (!OperationTheatreService.TheatreTypes.Contains(input.TheatreType)) ModelState.AddModelError(nameof(input.TheatreType), "Choose the theatre type.");
            if (!input.Is24Hours && input.ClosesAt <= input.OpensAt) ModelState.AddModelError(nameof(input.ClosesAt), "The closing time must be after the opening time (or tick \"Open 24 hours\").");

            var hospitalId = existing?.HospitalId ?? _hospitalContext.HospitalIdForNewRecords;
            if (input.Code.Length > 0 && await _context.OperationTheatres.IgnoreQueryFilters()
                    .AnyAsync(t => t.HospitalId == hospitalId && t.Code == input.Code && t.Id != input.Id))
                ModelState.AddModelError(nameof(input.Code), "Another theatre of this hospital already uses this code.");
        }

        private static string TheatreSummary(OperationTheatre t) =>
            $"{t.Code} {t.Name} ({t.TheatreType}) {(t.Is24Hours ? "24h" : $"{t.OpensAt:hh\\:mm}-{t.ClosesAt:hh\\:mm}")}, turnover {t.TurnoverMinutes} min";
    }
}
