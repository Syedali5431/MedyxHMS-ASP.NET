using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.EntityFrameworkCore;

// Purpose: Operation theatre bookings: clash prevention (theatre, block times, surgeon, patient, opening hours),
// status workflow, OT billing and theatre availability.
namespace MedyxHMS.Services.Implementations
{
    public class OperationTheatreService : IOperationTheatreService
    {
        public static readonly string[] TheatreTypes = { "General", "Cardiac", "Orthopaedic", "Neuro", "Obstetric", "Ophthalmic", "ENT", "Day care / minor", "Emergency" };
        public const string Scheduled = "Scheduled", InProgress = "In Progress", Completed = "Completed", Cancelled = "Cancelled";
        public const int MinDuration = 15, MaxDuration = 720, MaxTurnover = 240;
        public const decimal BaseCharge = 10000m, ChargePerExtraMinute = 50m;

        private readonly ApplicationDbContext _context;

        public OperationTheatreService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>OT charge: base charge for the first hour plus a charge per additional minute.</summary>
        public static decimal CalculateCharge(int durationMinutes) => BaseCharge + Math.Max(0, durationMinutes - 60) * ChargePerExtraMinute;

        public static string SurgeonDisplayName(Doctor doctor) => $"Dr. {doctor.FirstName} {doctor.LastName}".Trim();

        /// <summary>Until when a case keeps its theatre (and surgeon) busy: an operation still in progress keeps it until now.</summary>
        public static DateTime OccupiedUntil(OTSchedule s, DateTime now) => s.Status == InProgress && s.EndsAt < now ? now : s.EndsAt;

        private static bool IsActive(string status) => status == Scheduled || status == InProgress;

        public async Task<IEnumerable<OTSchedule>> GetSchedulesAsync()
        {
            return await _context.OTSchedules
                .Include(x => x.Patient)
                .Include(x => x.Theatre)
                .OrderByDescending(x => x.ScheduledDate)
                .ToListAsync();
        }

        public async Task<OTSchedule> GetScheduleByIdAsync(int id)
        {
            return await _context.OTSchedules
                .Include(x => x.Patient)
                .Include(x => x.Theatre)
                .Include(x => x.Surgeon)
                .Include(x => x.Bill)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<string>> FindConflictsAsync(OTSchedule booking, int? excludeId = null)
        {
            var problems = new List<string>();
            var theatre = booking.OperationTheatreId.HasValue
                ? await _context.OperationTheatres.AsNoTracking().FirstOrDefaultAsync(t => t.Id == booking.OperationTheatreId.Value)
                : null;
            if (theatre == null || !theatre.IsActive)
            {
                problems.Add("Choose an active theatre of this hospital.");
                return problems;
            }

            if (booking.EstimatedDurationMinutes < MinDuration || booking.EstimatedDurationMinutes > MaxDuration)
            {
                problems.Add("The estimated duration must be between 15 minutes and 12 hours.");
                return problems;
            }

            var now = DateTime.Now;
            var start = booking.ScheduledDate;
            var end = booking.EndsAt;
            var turnover = TimeSpan.FromMinutes(theatre.TurnoverMinutes);

            if (!theatre.Is24Hours && !booking.IsEmergency
                && (start < start.Date + theatre.OpensAt || end > start.Date + theatre.ClosesAt))
            {
                problems.Add($"{theatre.Code} is open {theatre.OpensAt:hh\\:mm}–{theatre.ClosesAt:hh\\:mm}. Choose a time within these hours, or tick \"Emergency case\".");
            }

            // Same theatre: cases may not overlap, and the turnover time after each case stays free.
            var earliest = start.AddMinutes(-(MaxDuration + MaxTurnover));
            var theatreCases = await _context.OTSchedules.IgnoreQueryFilters().AsNoTracking()
                .Where(o => o.OperationTheatreId == theatre.Id && o.Id != excludeId
                            && (o.Status == Scheduled || o.Status == InProgress)
                            && o.ScheduledDate < end.Add(turnover) && o.ScheduledDate > earliest)
                .OrderBy(o => o.ScheduledDate)
                .ToListAsync();
            foreach (var other in theatreCases)
            {
                var otherEnd = OccupiedUntil(other, now);
                if (other.ScheduledDate < end.Add(turnover) && start < otherEnd.Add(turnover))
                {
                    problems.Add($"{theatre.Code} is booked {other.ScheduledDate:dd-MMM HH:mm}–{otherEnd:HH:mm} ({other.ProcedureName}); {theatre.TurnoverMinutes} min turnover is kept free after each case.");
                }
            }

            var blocks = await _context.OTBlocks.IgnoreQueryFilters().AsNoTracking()
                .Where(b => b.OperationTheatreId == theatre.Id && b.StartsAt < end && start < b.EndsAt)
                .OrderBy(b => b.StartsAt)
                .ToListAsync();
            foreach (var block in blocks)
            {
                problems.Add($"{theatre.Code} is blocked {block.StartsAt:dd-MMM HH:mm}–{block.EndsAt:dd-MMM HH:mm}: {block.Reason}.");
            }

            // The surgeon and the patient can only be in one operation at a time (in any theatre of the group).
            var surgeonName = booking.SurgeonName?.Trim() ?? string.Empty;
            if (booking.SurgeonDoctorId.HasValue || surgeonName.Length > 0)
            {
                var surgeonCases = _context.OTSchedules.IgnoreQueryFilters().AsNoTracking()
                    .Include(o => o.Theatre)
                    .Where(o => o.Id != excludeId && (o.Status == Scheduled || o.Status == InProgress)
                                && o.ScheduledDate < end && o.ScheduledDate > start.AddMinutes(-MaxDuration));
                surgeonCases = booking.SurgeonDoctorId.HasValue
                    ? surgeonCases.Where(o => o.SurgeonDoctorId == booking.SurgeonDoctorId)
                    : surgeonCases.Where(o => o.SurgeonDoctorId == null && o.SurgeonName == surgeonName);
                foreach (var other in await surgeonCases.ToListAsync())
                {
                    var otherEnd = OccupiedUntil(other, now);
                    if (start < otherEnd)
                    {
                        problems.Add($"The surgeon is already booked {other.ScheduledDate:dd-MMM HH:mm}–{otherEnd:HH:mm} ({other.ProcedureName}, {other.Theatre?.Code ?? other.OperationTheatreNumber}).");
                    }
                }
            }

            var patientCases = await _context.OTSchedules.IgnoreQueryFilters().AsNoTracking()
                .Where(o => o.Id != excludeId && o.PatientId == booking.PatientId && (o.Status == Scheduled || o.Status == InProgress)
                            && o.ScheduledDate < end && o.ScheduledDate > start.AddMinutes(-MaxDuration))
                .ToListAsync();
            foreach (var other in patientCases)
            {
                var otherEnd = OccupiedUntil(other, now);
                if (start < otherEnd)
                {
                    problems.Add($"The patient already has an operation booked {other.ScheduledDate:dd-MMM HH:mm}–{otherEnd:HH:mm} ({other.ProcedureName}).");
                }
            }

            return problems.Distinct().ToList();
        }

        public async Task<OTSchedule> CreateScheduleAsync(OTSchedule schedule)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));

            await ApplyTheatreAndSurgeonAsync(schedule);
            schedule.CreatedDate = DateTime.Now;
            if (string.IsNullOrWhiteSpace(schedule.Status))
                schedule.Status = Scheduled;
            schedule.Notes ??= string.Empty;
            schedule.CancelReason ??= string.Empty;

            // Booking and its bill are saved together.
            await using var transaction = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync() : null;

            _context.OTSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            var totalCharge = CalculateCharge(schedule.EstimatedDurationMinutes);
            var bill = new Bill
            {
                HospitalId = schedule.HospitalId,
                BillNumber = GenerateBillNumber(),
                PatientId = schedule.PatientId,
                BillDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(10),
                TotalAmount = totalCharge,
                PaidAmount = 0,
                PendingAmount = totalCharge,
                Status = "Unpaid",
                BillType = "OT",
                Notes = $"OT booking for {schedule.ProcedureName}"
            };
            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            _context.BillItems.Add(new BillItem
            {
                BillId = bill.Id,
                ItemName = $"OT Booking - {schedule.ProcedureName}",
                ItemType = "OT",
                Quantity = 1,
                UnitPrice = totalCharge,
                TotalPrice = totalCharge,
                Description = $"OT {schedule.OperationTheatreNumber}, estimated {schedule.EstimatedDurationMinutes} min"
            });

            schedule.BillId = bill.Id;
            await _context.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync();

            return schedule;
        }

        public async Task<string> UpdateScheduleAsync(OTSchedule schedule)
        {
            await ApplyTheatreAndSurgeonAsync(schedule);
            var note = string.Empty;

            // An unpaid OT bill follows the new duration; a bill with payments is left for Billing to adjust.
            if (schedule.BillId.HasValue)
            {
                var bill = await _context.Bills.Include(b => b.BillItems).FirstOrDefaultAsync(b => b.Id == schedule.BillId.Value);
                var line = bill?.BillItems.FirstOrDefault(i => i.ItemType == "OT");
                var charge = CalculateCharge(schedule.EstimatedDurationMinutes);
                if (bill != null && line != null && line.TotalPrice != charge)
                {
                    if (bill.PaidAmount == 0)
                    {
                        line.UnitPrice = charge;
                        line.TotalPrice = charge;
                        line.Quantity = 1;
                        line.ItemName = $"OT Booking - {schedule.ProcedureName}";
                        line.Description = $"OT {schedule.OperationTheatreNumber}, estimated {schedule.EstimatedDurationMinutes} min";
                        bill.TotalAmount = bill.BillItems.Sum(i => i.TotalPrice);
                        bill.PendingAmount = bill.TotalAmount - bill.PaidAmount;
                        note = $" OT bill {bill.BillNumber} updated to {charge:0.00}.";
                    }
                    else
                    {
                        note = $" Bill {bill.BillNumber} already has payments – adjust the OT charge in Billing if needed.";
                    }
                }
            }

            await _context.SaveChangesAsync();
            return note;
        }

        public async Task<(bool Ok, string Message)> ChangeStatusAsync(int id, string status, string? reason)
        {
            var schedule = await _context.OTSchedules.Include(o => o.Theatre).FirstOrDefaultAsync(o => o.Id == id);
            if (schedule == null)
                return (false, "OT booking not found.");

            var allowed = schedule.Status switch
            {
                Scheduled => new[] { InProgress, Completed, Cancelled },
                InProgress => new[] { Completed },
                _ => Array.Empty<string>()
            };
            if (!allowed.Contains(status))
                return (false, $"A booking that is {schedule.Status.ToLowerInvariant()} cannot be set to {(status ?? "?").ToLowerInvariant()}.");

            var now = DateTime.Now;
            var message = $"OT booking set to {status.ToLowerInvariant()}.";
            switch (status)
            {
                case InProgress:
                    if (schedule.ScheduledDate.Date > now.Date)
                        return (false, "An operation planned for a later day cannot be started now – reschedule it first.");
                    if (schedule.OperationTheatreId.HasValue && await _context.OTSchedules.IgnoreQueryFilters()
                            .AnyAsync(o => o.Id != id && o.OperationTheatreId == schedule.OperationTheatreId && o.Status == InProgress))
                        return (false, $"{schedule.Theatre?.Code ?? schedule.OperationTheatreNumber} is still in use by an operation in progress – complete that one first.");
                    schedule.StartedAt = now;
                    break;

                case Completed:
                    if (schedule.Status == Scheduled && schedule.ScheduledDate > now)
                        return (false, "An operation that has not started yet cannot be marked completed.");
                    schedule.StartedAt ??= schedule.ScheduledDate;
                    schedule.CompletedAt = now;
                    break;

                case Cancelled:
                    if (string.IsNullOrWhiteSpace(reason))
                        return (false, "Give a reason for cancelling.");
                    schedule.CancelReason = reason.Trim();
                    message = "OT booking cancelled." + await WithdrawUnpaidBillAsync(schedule);
                    break;
            }

            schedule.Status = status;
            await _context.SaveChangesAsync();
            return (true, message);
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            return (await ChangeStatusAsync(id, status, null)).Ok;
        }

        public async Task<List<OTTheatreDay>> GetDayAvailabilityAsync(DateTime day, int? theatreId = null)
        {
            day = day.Date;
            var theatres = await _context.OperationTheatres.AsNoTracking()
                .Where(t => t.IsActive && (theatreId == null || t.Id == theatreId))
                .OrderBy(t => t.Code)
                .ToListAsync();
            var ids = theatres.Select(t => t.Id).ToList();
            var from = day.AddMinutes(-(MaxDuration + MaxTurnover));
            var to = day.AddDays(1);
            var cases = await _context.OTSchedules.IgnoreQueryFilters().AsNoTracking()
                .Include(o => o.Patient)
                .Where(o => o.OperationTheatreId != null && ids.Contains(o.OperationTheatreId.Value) && o.Status != Cancelled
                            && o.ScheduledDate < to && o.ScheduledDate > from)
                .OrderBy(o => o.ScheduledDate)
                .ToListAsync();
            var blocks = await _context.OTBlocks.IgnoreQueryFilters().AsNoTracking()
                .Where(b => ids.Contains(b.OperationTheatreId) && b.StartsAt < to && b.EndsAt > day)
                .OrderBy(b => b.StartsAt)
                .ToListAsync();

            var now = DateTime.Now;
            var result = new List<OTTheatreDay>();
            foreach (var theatre in theatres)
            {
                var open = theatre.Is24Hours ? day : day + theatre.OpensAt;
                var close = theatre.Is24Hours ? day.AddDays(1) : day + theatre.ClosesAt;
                var theatreCases = cases.Where(c => c.OperationTheatreId == theatre.Id && OccupiedUntil(c, now) > day).ToList();
                var theatreBlocks = blocks.Where(b => b.OperationTheatreId == theatre.Id).ToList();
                var booked = theatreCases.Sum(c =>
                {
                    var s = c.ScheduledDate < open ? open : c.ScheduledDate;
                    var e = c.EndsAt > close ? close : c.EndsAt;
                    return e > s ? (int)(e - s).TotalMinutes : 0;
                });
                result.Add(new OTTheatreDay
                {
                    Theatre = theatre,
                    OpenFrom = open,
                    OpenUntil = close,
                    Cases = theatreCases,
                    Blocks = theatreBlocks,
                    Free = FreeWindows(theatre, open, close, theatreCases, theatreBlocks, now),
                    BookedMinutes = booked
                });
            }
            return result;
        }

        /// <summary>
        /// Free periods of a theatre between opening and closing time (from now on for today). A case needs the
        /// turnover time free after it when another case follows; block times and closing time need no turnover.
        /// </summary>
        public static List<OTFreeWindow> FreeWindows(OperationTheatre theatre, DateTime open, DateTime close, IEnumerable<OTSchedule> cases, IEnumerable<OTBlock> blocks, DateTime now)
        {
            var turnover = TimeSpan.FromMinutes(theatre.TurnoverMinutes);
            var busy = cases.Where(c => IsActive(c.Status))
                .Select(c => (From: c.ScheduledDate, To: OccupiedUntil(c, now).Add(turnover), IsCase: true))
                .Concat(blocks.Select(b => (From: b.StartsAt, To: b.EndsAt, IsCase: false)))
                .OrderBy(b => b.From)
                .ToList();

            var cursor = open;
            if (now > cursor)
            {
                // Bookings start on a quarter of an hour.
                var rounded = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddMinutes(15 * Math.Ceiling(now.Minute / 15.0));
                cursor = rounded > cursor ? rounded : cursor;
            }

            var free = new List<OTFreeWindow>();
            foreach (var (from, to, isCase) in busy)
            {
                if (cursor >= close) break;
                if (from > cursor)
                {
                    var gapEnd = from < close ? from : close;
                    var longest = (int)(gapEnd - cursor).TotalMinutes - (isCase && from < close ? theatre.TurnoverMinutes : 0);
                    if (longest >= MinDuration) free.Add(new OTFreeWindow(cursor, gapEnd, longest));
                }
                if (to > cursor) cursor = to;
            }
            if (cursor < close)
            {
                var longest = (int)(close - cursor).TotalMinutes;
                if (longest >= MinDuration) free.Add(new OTFreeWindow(cursor, close, longest));
            }
            return free;
        }

        private async Task ApplyTheatreAndSurgeonAsync(OTSchedule schedule)
        {
            if (schedule.OperationTheatreId.HasValue)
            {
                var theatre = await _context.OperationTheatres.IgnoreQueryFilters().AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == schedule.OperationTheatreId.Value);
                if (theatre != null)
                {
                    schedule.OperationTheatreNumber = theatre.Code;
                    schedule.HospitalId = theatre.HospitalId ?? schedule.HospitalId;
                }
            }

            if (schedule.SurgeonDoctorId.HasValue)
            {
                var doctor = await _context.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == schedule.SurgeonDoctorId.Value);
                if (doctor != null) schedule.SurgeonName = SurgeonDisplayName(doctor);
            }
            schedule.SurgeonName = schedule.SurgeonName?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Withdraws the OT charge of a cancelled booking when nothing has been paid: the bill keeps its number and
        /// line (now 0.00, marked cancelled) and is closed, so it no longer counts as outstanding or as revenue.
        /// </summary>
        private async Task<string> WithdrawUnpaidBillAsync(OTSchedule schedule)
        {
            if (!schedule.BillId.HasValue)
                return string.Empty;

            var billId = schedule.BillId.Value;
            var bill = await _context.Bills.IgnoreQueryFilters().Include(b => b.BillItems).FirstOrDefaultAsync(b => b.Id == billId);
            if (bill == null)
                return string.Empty;

            if (bill.PaidAmount > 0 || bill.BillItems.Any(i => i.ItemType != "OT"))
                return $" Bill {bill.BillNumber} has payments or other charges – arrange the refund or adjustment in Billing.";

            var withdrawn = bill.TotalAmount;
            foreach (var line in bill.BillItems)
            {
                line.UnitPrice = 0;
                line.TotalPrice = 0;
                if (!line.ItemName.EndsWith("(cancelled)")) line.ItemName = $"{line.ItemName} (cancelled)";
            }
            bill.TotalAmount = 0;
            bill.PendingAmount = 0;
            bill.Status = "Paid";
            bill.Notes = $"{bill.Notes} – OT booking cancelled, charge of {withdrawn:0.00} withdrawn.".Trim(' ', '–');
            return $" The OT charge of {withdrawn:0.00} on bill {bill.BillNumber} was withdrawn.";
        }

        private string GenerateBillNumber()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var lastBill = _context.Bills
                .IgnoreQueryFilters() // bill numbers are unique across all hospitals of the group
                .Where(b => b.BillNumber.StartsWith($"OTBILL{datePart}"))
                .OrderByDescending(b => b.Id)
                .FirstOrDefault();

            var sequentialNumber = 1;
            if (lastBill != null)
            {
                var lastNumber = lastBill.BillNumber.Substring(14);
                if (int.TryParse(lastNumber, out var parsedNumber))
                {
                    sequentialNumber = parsedNumber + 1;
                }
            }

            return $"OTBILL{datePart}{sequentialNumber:D4}";
        }
    }
}
