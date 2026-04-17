using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public class PayrollService : IPayrollService
    {
        private readonly ApplicationDbContext _context;

        public PayrollService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PayrollRecord>> GetPayrollRecordsAsync(DateTime? month = null, string staffId = null)
        {
            var targetMonth = month.HasValue
                ? new DateTime(month.Value.Year, month.Value.Month, 1)
                : (DateTime?)null;

            var query = _context.PayrollRecords
                .Include(x => x.Staff)
                .ThenInclude(s => s.User)
                .AsQueryable();

            if (targetMonth.HasValue)
            {
                query = query.Where(x => x.PayrollMonth == targetMonth.Value);
            }

            if (!string.IsNullOrWhiteSpace(staffId))
            {
                query = query.Where(x => x.StaffId == staffId);
            }

            return await query
                .OrderByDescending(x => x.PayrollMonth)
                .ThenBy(x => x.Staff.FirstName)
                .ToListAsync();
        }

        public async Task<PayrollRecord> GetPayrollRecordByIdAsync(int id)
        {
            return await _context.PayrollRecords
                .Include(x => x.Staff)
                .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PayrollRecord> GeneratePayrollAsync(string staffId, DateTime payrollMonth, decimal allowances = 0, decimal deductions = 0, string notes = null)
        {
            if (string.IsNullOrWhiteSpace(staffId))
                throw new InvalidOperationException("Staff ID is required.");

            var normalizedMonth = new DateTime(payrollMonth.Year, payrollMonth.Month, 1);

            var staff = await _context.Staff.FirstOrDefaultAsync(x => x.Id == staffId);
            if (staff == null)
                throw new InvalidOperationException("Staff not found.");

            var existing = await _context.PayrollRecords
                .FirstOrDefaultAsync(x => x.StaffId == staffId && x.PayrollMonth == normalizedMonth);
            if (existing != null)
                throw new InvalidOperationException("Payroll already exists for this staff and month.");

            var basicSalary = staff.Salary;
            var netSalary = basicSalary + allowances - deductions;

            var payroll = new PayrollRecord
            {
                StaffId = staffId,
                PayrollMonth = normalizedMonth,
                BasicSalary = basicSalary,
                Allowances = allowances,
                Deductions = deductions,
                NetSalary = netSalary,
                Status = "Processed",
                Notes = notes,
                CreatedDate = DateTime.UtcNow
            };

            _context.PayrollRecords.Add(payroll);
            await _context.SaveChangesAsync();

            return payroll;
        }

        public async Task<bool> MarkPayrollAsPaidAsync(int payrollRecordId, DateTime paymentDate, string notes = null)
        {
            var payroll = await _context.PayrollRecords.FindAsync(payrollRecordId);
            if (payroll == null)
                return false;

            payroll.Status = "Paid";
            payroll.PaymentDate = paymentDate;
            if (!string.IsNullOrWhiteSpace(notes))
            {
                payroll.Notes = string.IsNullOrWhiteSpace(payroll.Notes)
                    ? notes
                    : $"{payroll.Notes}; {notes}";
            }
            payroll.UpdatedDate = DateTime.UtcNow;

            _context.PayrollRecords.Update(payroll);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
