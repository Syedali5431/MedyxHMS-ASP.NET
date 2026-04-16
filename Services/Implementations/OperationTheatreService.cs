using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public class OperationTheatreService : IOperationTheatreService
    {
        private readonly ApplicationDbContext _context;

        public OperationTheatreService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<OTSchedule>> GetSchedulesAsync()
        {
            return await _context.OTSchedules
                .Include(x => x.Patient)
                .OrderByDescending(x => x.ScheduledDate)
                .ToListAsync();
        }

        public async Task<OTSchedule> GetScheduleByIdAsync(int id)
        {
            return await _context.OTSchedules
                .Include(x => x.Patient)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<OTSchedule> CreateScheduleAsync(OTSchedule schedule)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));

            schedule.CreatedDate = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(schedule.Status))
                schedule.Status = "Scheduled";

            _context.OTSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            var baseCharge = 10000m;
            var durationCharge = Math.Max(0, schedule.EstimatedDurationMinutes - 60) * 50m;
            var totalCharge = baseCharge + durationCharge;

            var bill = new Bill
            {
                PatientId = schedule.PatientId,
                BillDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(10),
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
            _context.OTSchedules.Update(schedule);
            await _context.SaveChangesAsync();

            return schedule;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var schedule = await _context.OTSchedules.FindAsync(id);
            if (schedule == null)
                return false;

            schedule.Status = status;
            _context.OTSchedules.Update(schedule);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
