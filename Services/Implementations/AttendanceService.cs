using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for AttendanceService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class AttendanceService : IAttendanceService
    {
        private readonly ApplicationDbContext _context;

        public AttendanceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StaffAttendance>> GetAttendanceAsync(DateTime date, string staffId = null)
        {
            var query = _context.StaffAttendances
                .Include(x => x.Staff)
                .ThenInclude(s => s.User)
                .Where(x => x.AttendanceDate == date.Date);

            if (!string.IsNullOrWhiteSpace(staffId))
            {
                query = query.Where(x => x.StaffId == staffId);
            }

            return await query
                .OrderBy(x => x.Staff.FirstName)
                .ThenBy(x => x.Staff.LastName)
                .ToListAsync();
        }

        public async Task<StaffAttendance> GetAttendanceByIdAsync(int id)
        {
            return await _context.StaffAttendances
                .Include(x => x.Staff)
                .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<StaffAttendance> MarkAttendanceAsync(StaffAttendance attendance)
        {
            if (attendance == null)
                throw new ArgumentNullException(nameof(attendance));

            attendance.AttendanceDate = attendance.AttendanceDate.Date;

            var existing = await _context.StaffAttendances
                .FirstOrDefaultAsync(x => x.StaffId == attendance.StaffId && x.AttendanceDate == attendance.AttendanceDate);

            if (existing == null)
            {
                attendance.CreatedDate = DateTime.UtcNow;
                _context.StaffAttendances.Add(attendance);
                await _context.SaveChangesAsync();
                return attendance;
            }

            existing.Status = attendance.Status;
            existing.CheckInTime = attendance.CheckInTime;
            existing.CheckOutTime = attendance.CheckOutTime;
            existing.Notes = attendance.Notes;
            existing.UpdatedDate = DateTime.UtcNow;

            _context.StaffAttendances.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<StaffAttendance> CheckInAsync(string staffId, DateTime checkInTime, string notes = null)
        {
            if (string.IsNullOrWhiteSpace(staffId))
                throw new ArgumentException("Staff ID is required.", nameof(staffId));

            var date = checkInTime.Date;
            var existing = await _context.StaffAttendances
                .FirstOrDefaultAsync(x => x.StaffId == staffId && x.AttendanceDate == date);

            if (existing == null)
            {
                var attendance = new StaffAttendance
                {
                    StaffId = staffId,
                    AttendanceDate = date,
                    CheckInTime = checkInTime,
                    Status = "Present",
                    Notes = notes,
                    CreatedDate = DateTime.UtcNow
                };

                _context.StaffAttendances.Add(attendance);
                await _context.SaveChangesAsync();

                return await GetAttendanceByIdAsync(attendance.Id);
            }

            existing.CheckInTime ??= checkInTime;
            existing.Status = string.IsNullOrWhiteSpace(existing.Status) ? "Present" : existing.Status;
            if (!string.IsNullOrWhiteSpace(notes))
            {
                existing.Notes = string.IsNullOrWhiteSpace(existing.Notes)
                    ? notes
                    : $"{existing.Notes}; {notes}";
            }
            existing.UpdatedDate = DateTime.UtcNow;

            _context.StaffAttendances.Update(existing);
            await _context.SaveChangesAsync();

            return await GetAttendanceByIdAsync(existing.Id);
        }

        public async Task<StaffAttendance> CheckOutAsync(string staffId, DateTime checkOutTime, string notes = null)
        {
            if (string.IsNullOrWhiteSpace(staffId))
                throw new ArgumentException("Staff ID is required.", nameof(staffId));

            var date = checkOutTime.Date;
            var existing = await _context.StaffAttendances
                .FirstOrDefaultAsync(x => x.StaffId == staffId && x.AttendanceDate == date);

            if (existing == null)
                throw new InvalidOperationException("Check-in not found for selected date.");

            existing.CheckOutTime = checkOutTime;
            if (string.IsNullOrWhiteSpace(existing.Status))
                existing.Status = "Present";

            if (!string.IsNullOrWhiteSpace(notes))
            {
                existing.Notes = string.IsNullOrWhiteSpace(existing.Notes)
                    ? notes
                    : $"{existing.Notes}; {notes}";
            }

            existing.UpdatedDate = DateTime.UtcNow;
            _context.StaffAttendances.Update(existing);
            await _context.SaveChangesAsync();

            return await GetAttendanceByIdAsync(existing.Id);
        }

        public async Task<Dictionary<string, int>> GetAttendanceSummaryAsync(DateTime startDate, DateTime endDate)
        {
            var records = await _context.StaffAttendances
                .Where(x => x.AttendanceDate >= startDate.Date && x.AttendanceDate <= endDate.Date)
                .ToListAsync();

            return new Dictionary<string, int>
            {
                ["Present"] = records.Count(x => x.Status == "Present"),
                ["Absent"] = records.Count(x => x.Status == "Absent"),
                ["HalfDay"] = records.Count(x => x.Status == "HalfDay"),
                ["OnLeave"] = records.Count(x => x.Status == "OnLeave")
            };
        }
    }
}
