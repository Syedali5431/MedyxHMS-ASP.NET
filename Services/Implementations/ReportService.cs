using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Department Reports

        public async Task<List<dynamic>> GenerateDepartmentReportAsync(int? departmentId, DateTime startDate, DateTime endDate)
        {
            var staffList = await _context.Staff
                .Where(s => !departmentId.HasValue || s.Id == departmentId.ToString())
                .ToListAsync();
            
            var report = new List<dynamic>();

            foreach (var staff in staffList)
            {
                var attendanceCount = await _context.StaffAttendances
                    .Where(sa => sa.StaffId == staff.Id && sa.AttendanceDate >= startDate && sa.AttendanceDate <= endDate)
                    .CountAsync();

                var leaveCount = await _context.LeaveRequests
                    .Where(lr => lr.StaffId == staff.Id && lr.Status == "Approved" && lr.StartDate >= startDate && lr.EndDate <= endDate)
                    .CountAsync();

                report.Add(new
                {
                    StaffId = staff.Id,
                    StaffName = staff.FirstName + " " + staff.LastName,
                    AttendanceDays = attendanceCount,
                    ApprovedLeaves = leaveCount
                });
            }

            return report;
        }

        #endregion

        #region Financial Reports

        public async Task<Dictionary<string, decimal>> GenerateFinancialReportAsync(DateTime startDate, DateTime endDate)
        {
            var payrollTotal = await _context.PayrollRecords
                .Where(pr => pr.CreatedDate >= startDate && pr.CreatedDate <= endDate && pr.Status == "Paid")
                .SumAsync(pr => pr.NetSalary);

            var billsTotal = await _context.Bills
                .Where(b => b.BillDate >= startDate && b.BillDate <= endDate)
                .SumAsync(b => b.TotalAmount);

            var paymentsTotal = await _context.Payments
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .SumAsync(p => p.Amount);

            return new Dictionary<string, decimal>
            {
                { "TotalPayroll", payrollTotal },
                { "TotalBills", billsTotal },
                { "TotalPayments", paymentsTotal },
                { "NetRevenue", billsTotal - payrollTotal }
            };
        }

        public async Task<decimal> GetTotalRevenueByDepartmentAsync(int departmentId, DateTime startDate, DateTime endDate)
        {
            var revenue = await _context.Bills
                .Where(b => b.CreatedDate >= startDate && b.CreatedDate <= endDate)
                .SumAsync(b => b.TotalAmount);

            return revenue;
        }

        #endregion

        #region Occupancy Reports

        public async Task<Dictionary<string, int>> GenerateOccupancyReportAsync(DateTime date)
        {
            var totalBeds = await _context.Beds.CountAsync();
            
            var occupiedBeds = await _context.IPDAdmissions
                .Where(ipd => ipd.AdmissionDate <= date && (ipd.DischargeDate == null || ipd.DischargeDate >= date))
                .CountAsync();

            var availableBeds = totalBeds - occupiedBeds;

            return new Dictionary<string, int>
            {
                { "TotalBeds", totalBeds },
                { "OccupiedBeds", occupiedBeds },
                { "AvailableBeds", availableBeds }
            };
        }

        public async Task<double> GetAverageOccupancyRateAsync(DateTime startDate, DateTime endDate)
        {
            var totalBeds = await _context.Beds.CountAsync();
            if (totalBeds == 0) return 0;

            var occupancyByDay = new Dictionary<DateTime, int>();
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                var occupiedBeds = await _context.IPDAdmissions
                    .Where(ipd => ipd.AdmissionDate <= currentDate && (ipd.DischargeDate == null || ipd.DischargeDate >= currentDate))
                    .CountAsync();

                occupancyByDay[currentDate] = occupiedBeds;
                currentDate = currentDate.AddDays(1);
            }

            if (occupancyByDay.Count == 0) return 0;

            var averageOccupied = occupancyByDay.Values.Average();
            return (averageOccupied / totalBeds) * 100;
        }

        #endregion

        #region Staff Reports

        public async Task<List<dynamic>> GenerateStaffAttendanceReportAsync(string staffId, DateTime startDate, DateTime endDate)
        {
            var attendanceRecords = await _context.StaffAttendances
                .Where(sa => sa.StaffId == staffId && sa.AttendanceDate >= startDate && sa.AttendanceDate <= endDate)
                .OrderByDescending(sa => sa.AttendanceDate)
                .ToListAsync();

            var report = new List<dynamic>();
            var presentCount = attendanceRecords.Count(a => a.Status == "Present");
            var absentCount = attendanceRecords.Count(a => a.Status == "Absent");
            var halfDayCount = attendanceRecords.Count(a => a.Status == "HalfDay");

            report.Add(new
            {
                TotalDays = attendanceRecords.Count,
                PresentDays = presentCount,
                AbsentDays = absentCount,
                HalfDays = halfDayCount,
                AttendancePercentage = attendanceRecords.Count > 0 ? ((presentCount + (halfDayCount * 0.5)) / attendanceRecords.Count) * 100 : 0
            });

            return report;
        }

        public async Task<List<dynamic>> GeneratePayrollReportAsync(DateTime month)
        {
            var payrollRecords = await _context.PayrollRecords
                .Where(pr => pr.PayrollMonth.Year == month.Year && pr.PayrollMonth.Month == month.Month)
                .Include(pr => pr.Staff)
                .ToListAsync();

            var report = new List<dynamic>();

            foreach (var record in payrollRecords)
            {
                report.Add(new
                {
                    StaffId = record.StaffId,
                    StaffName = record.Staff?.FirstName + " " + record.Staff?.LastName,
                    BasicSalary = record.BasicSalary,
                    Allowances = record.Allowances,
                    Deductions = record.Deductions,
                    NetSalary = record.NetSalary,
                    Status = record.Status
                });
            }

            return report;
        }

        public async Task<Dictionary<string, int>> GetStaffDepartmentDistributionAsync()
        {
            var distribution = await _context.Staff
                .Where(s => s.Department != null)
                .GroupBy(s => s.Department)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Department ?? "Unknown", x => x.Count);

            return distribution;
        }

        #endregion

        #region General Report Management

        public async Task<GeneratedReport> SaveReportAsync(GeneratedReport report)
        {
            if (report.Id == 0)
            {
                _context.GeneratedReports.Add(report);
            }
            else
            {
                _context.GeneratedReports.Update(report);
            }

            await _context.SaveChangesAsync();
            return report;
        }

        public async Task<IEnumerable<GeneratedReport>> GetGeneratedReportsAsync(string reportType = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.GeneratedReports.AsQueryable();

            if (!string.IsNullOrEmpty(reportType))
                query = query.Where(gr => gr.ReportType == reportType);

            if (startDate.HasValue)
                query = query.Where(gr => gr.CreatedDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(gr => gr.CreatedDate <= endDate.Value);

            return await query.OrderByDescending(gr => gr.CreatedDate)
                .Include(gr => gr.StaffGenerated)
                .ToListAsync();
        }

        public async Task<bool> DeleteGeneratedReportAsync(int reportId)
        {
            var report = await _context.GeneratedReports.FindAsync(reportId);
            if (report == null) return false;

            // Delete file if exists
            if (!string.IsNullOrEmpty(report.FilePath) && File.Exists(report.FilePath))
            {
                File.Delete(report.FilePath);
            }

            _context.GeneratedReports.Remove(report);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Report Scheduling

        public async Task<ReportSchedule> CreateReportScheduleAsync(ReportSchedule schedule)
        {
            schedule.NextRunDate = CalculateNextRunDate(schedule);
            _context.ReportSchedules.Add(schedule);
            await _context.SaveChangesAsync();
            return schedule;
        }

        public async Task<IEnumerable<ReportSchedule>> GetReportSchedulesAsync(bool activeOnly = true)
        {
            var query = _context.ReportSchedules.AsQueryable();

            if (activeOnly)
                query = query.Where(rs => rs.IsActive);

            return await query.OrderBy(rs => rs.NextRunDate)
                .Include(rs => rs.StaffCreated)
                .ToListAsync();
        }

        public async Task<bool> UpdateReportScheduleAsync(ReportSchedule schedule)
        {
            var existing = await _context.ReportSchedules.FindAsync(schedule.Id);
            if (existing == null) return false;

            existing.ReportName = schedule.ReportName;
            existing.RecurrencePattern = schedule.RecurrencePattern;
            existing.DayOfWeek = schedule.DayOfWeek;
            existing.DayOfMonth = schedule.DayOfMonth;
            existing.TimeOfDay = schedule.TimeOfDay;
            existing.IsActive = schedule.IsActive;
            existing.EmailRecipients = schedule.EmailRecipients;

            if (schedule.IsActive)
            {
                existing.NextRunDate = CalculateNextRunDate(schedule);
            }

            _context.ReportSchedules.Update(existing);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteReportScheduleAsync(int scheduleId)
        {
            var schedule = await _context.ReportSchedules.FindAsync(scheduleId);
            if (schedule == null) return false;

            _context.ReportSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Helper Methods

        private DateTime? CalculateNextRunDate(ReportSchedule schedule)
        {
            var timeOfDay = TimeSpan.Parse(schedule.TimeOfDay ?? "08:00");
            var now = DateTime.UtcNow;
            var nextRun = now.Date.Add(timeOfDay);

            return schedule.RecurrencePattern switch
            {
                "Daily" => nextRun < now ? nextRun.AddDays(1) : nextRun,
                "Weekly" => GetNextWeeklyDate(now, schedule.DayOfWeek ?? 1, timeOfDay),
                "Monthly" => GetNextMonthlyDate(now, schedule.DayOfMonth ?? 1, timeOfDay),
                "Quarterly" => GetNextQuarterlyDate(now, schedule.DayOfMonth ?? 1, timeOfDay),
                "Yearly" => GetNextYearlyDate(now, schedule.DayOfMonth ?? 1, timeOfDay),
                _ => nextRun
            };
        }

        private DateTime GetNextWeeklyDate(DateTime now, int dayOfWeek, TimeSpan timeOfDay)
        {
            var daysUntilTarget = ((dayOfWeek - (int)now.DayOfWeek + 7) % 7);
            var nextDate = now.AddDays(daysUntilTarget).Date.Add(timeOfDay);
            return nextDate <= now ? nextDate.AddDays(7) : nextDate;
        }

        private DateTime GetNextMonthlyDate(DateTime now, int dayOfMonth, TimeSpan timeOfDay)
        {
            var nextDate = new DateTime(now.Year, now.Month, Math.Min(dayOfMonth, DateTime.DaysInMonth(now.Year, now.Month))).Add(timeOfDay);
            return nextDate <= now ? nextDate.AddMonths(1) : nextDate;
        }

        private DateTime GetNextQuarterlyDate(DateTime now, int dayOfMonth, TimeSpan timeOfDay)
        {
            var nextQuarter = ((now.Month - 1) / 3 + 1) * 3 + 1;
            var nextYear = now.Year + (nextQuarter > 12 ? 1 : 0);
            var nextMonth = nextQuarter % 13;
            var nextDate = new DateTime(nextYear, nextMonth, Math.Min(dayOfMonth, DateTime.DaysInMonth(nextYear, nextMonth))).Add(timeOfDay);
            return nextDate;
        }

        private DateTime GetNextYearlyDate(DateTime now, int dayOfMonth, TimeSpan timeOfDay)
        {
            var nextDate = new DateTime(now.Year, 1, Math.Min(dayOfMonth, DateTime.DaysInMonth(now.Year, 1))).Add(timeOfDay);
            return nextDate <= now ? nextDate.AddYears(1) : nextDate;
        }

        #endregion
    }
}
