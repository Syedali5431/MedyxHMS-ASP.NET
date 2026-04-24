using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

// Purpose: Contains application code for ReportService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ReportService> _logger;

        public ReportService(ApplicationDbContext context, ICacheService cacheService, ILogger<ReportService> logger)
        {
            _context = context;
            _cacheService = cacheService;
            _logger = logger;
        }

        #region Department Reports

        public async Task<List<dynamic>> GenerateDepartmentReportAsync(int? departmentId, DateTime startDate, DateTime endDate)
        {
            var cacheKey = $"report:department:{departmentId?.ToString() ?? "all"}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
            var cached = await _cacheService.GetAsync<List<DepartmentReportCacheItem>>(cacheKey);
            if (cached != null)
            {
                return cached
                    .Select(i => (dynamic)new
                    {
                        i.StaffId,
                        i.StaffName,
                        i.AttendanceDays,
                        i.ApprovedLeaves,
                        i.Department
                    })
                    .ToList();
            }

            try
            {
                var rows = await ExecuteStoredProcedureAsync(
                    "sp_GetDepartmentReport",
                    new List<SqlParameter>
                    {
                        new("@StartDate", startDate),
                        new("@EndDate", endDate),
                        new("@DepartmentId", departmentId?.ToString() ?? (object)DBNull.Value)
                    });

                var mapped = rows.Select(r => new DepartmentReportCacheItem
                {
                    StaffId = GetString(r, "StaffId"),
                    StaffName = GetString(r, "StaffName"),
                    Department = GetString(r, "Department"),
                    AttendanceDays = GetInt(r, "PresentDays"),
                    ApprovedLeaves = GetInt(r, "ApprovedLeaves")
                }).ToList();

                await _cacheService.SetAsync(cacheKey, mapped, 10);
                return mapped
                    .Select(i => (dynamic)new
                    {
                        i.StaffId,
                        i.StaffName,
                        i.AttendanceDays,
                        i.ApprovedLeaves,
                        i.Department
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stored procedure path failed for department report. Falling back to LINQ query.");
            }

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
            var cacheKey = $"report:financial:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
            var cached = await _cacheService.GetAsync<Dictionary<string, decimal>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            try
            {
                var rows = await ExecuteStoredProcedureAsync(
                    "sp_GetFinancialReport",
                    new List<SqlParameter>
                    {
                        new("@StartDate", startDate),
                        new("@EndDate", endDate)
                    });

                var result = new Dictionary<string, decimal>
                {
                    ["TotalPayroll"] = rows.Where(r => GetString(r, "TransactionType") == "Payroll").Sum(r => GetDecimal(r, "Amount")),
                    ["TotalBills"] = rows.Where(r => GetString(r, "TransactionType") == "Bills").Sum(r => GetDecimal(r, "Amount")),
                    ["TotalPayments"] = rows.Where(r => GetString(r, "TransactionType") == "Payments").Sum(r => GetDecimal(r, "Amount"))
                };
                result["NetRevenue"] = result["TotalBills"] - result["TotalPayroll"];
                await _cacheService.SetAsync(cacheKey, result, 15);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stored procedure path failed for financial report. Falling back to LINQ query.");
            }

            var payrollTotal = await _context.PayrollRecords
                .Where(pr => pr.CreatedDate >= startDate && pr.CreatedDate <= endDate && pr.Status == "Paid")
                .SumAsync(pr => pr.NetSalary);

            var billsTotal = await _context.Bills
                .Where(b => b.BillDate >= startDate && b.BillDate <= endDate)
                .SumAsync(b => b.TotalAmount);

            var paymentsTotal = await _context.Payments
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .SumAsync(p => p.Amount);

            var fallback = new Dictionary<string, decimal>
            {
                { "TotalPayroll", payrollTotal },
                { "TotalBills", billsTotal },
                { "TotalPayments", paymentsTotal },
                { "NetRevenue", billsTotal - payrollTotal }
            };
            await _cacheService.SetAsync(cacheKey, fallback, 15);
            return fallback;
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
            var cacheKey = $"report:occupancy:{date:yyyyMMdd}";
            var cached = await _cacheService.GetAsync<Dictionary<string, int>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            try
            {
                var rows = await ExecuteStoredProcedureAsync(
                    "sp_GetOccupancyReport",
                    new List<SqlParameter> { new("@ReportDate", date) });

                var first = rows.FirstOrDefault();
                if (first != null)
                {
                    var result = new Dictionary<string, int>
                    {
                        { "TotalBeds", GetInt(first, "TotalBeds") },
                        { "OccupiedBeds", GetInt(first, "OccupiedBeds") },
                        { "AvailableBeds", GetInt(first, "AvailableBeds") }
                    };
                    await _cacheService.SetAsync(cacheKey, result, 10);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stored procedure path failed for occupancy report. Falling back to LINQ query.");
            }

            var totalBeds = await _context.Beds.CountAsync();

            var occupiedBeds = await _context.IPDAdmissions
                .Where(ipd => ipd.AdmissionDate <= date && (ipd.DischargeDate == null || ipd.DischargeDate >= date))
                .CountAsync();

            var availableBeds = totalBeds - occupiedBeds;

            var fallback = new Dictionary<string, int>
            {
                { "TotalBeds", totalBeds },
                { "OccupiedBeds", occupiedBeds },
                { "AvailableBeds", availableBeds }
            };
            await _cacheService.SetAsync(cacheKey, fallback, 10);
            return fallback;
        }

        public async Task<double> GetAverageOccupancyRateAsync(DateTime startDate, DateTime endDate)
        {
            var cacheKey = $"report:occupancy:avg:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
            var cached = await _cacheService.GetAsync<OccupancyAverageCacheItem>(cacheKey);
            if (cached != null)
            {
                return cached.Value;
            }

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
            var averageRate = (averageOccupied / totalBeds) * 100;
            await _cacheService.SetAsync(cacheKey, new OccupancyAverageCacheItem { Value = averageRate }, 10);
            return averageRate;
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

        #region Legacy Reports (R1-R5)

        /// <summary>
        /// R1: Daily Transaction Report - All transactions for a specific date
        /// </summary>
        public async Task<DailyTransactionReportViewModel> GenerateDailyTransactionReportAsync(DateTime reportDate)
        {
            var cacheKey = $"report:daily-transaction:{reportDate:yyyyMMdd}";
            var cached = await _cacheService.GetAsync<DailyTransactionReportViewModel>(cacheKey);
            if (cached != null) return cached;

            var transactions = await _context.Transactions
                .Where(t => t.TransactionDate.Date == reportDate.Date)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            var transactionData = transactions.Select(t => (dynamic)new
            {
                t.Id,
                t.TransactionId,
                t.TransactionType,
                t.Amount,
                t.Description,
                t.ReferenceNumber,
                TransactionDate = t.TransactionDate.ToString("yyyy-MM-dd HH:mm"),
                t.ProcessedBy,
                t.Status
            }).ToList();

            var totalPayments = transactions.Where(t => t.TransactionType == "Payment").Sum(t => t.Amount);
            var totalRefunds = transactions.Where(t => t.TransactionType == "Refund").Sum(t => t.Amount);

            var result = new DailyTransactionReportViewModel
            {
                TransactionData = transactionData,
                ReportDate = reportDate.Date,
                TotalTransactions = transactions.Sum(t => t.Amount),
                TotalPayments = totalPayments,
                TotalRefunds = totalRefunds,
                TransactionCount = transactions.Count
            };

            await _cacheService.SetAsync(cacheKey, result, 10);
            return result;
        }

        /// <summary>
        /// R2: All Transaction Report - Transactions within a date range
        /// </summary>
        public async Task<AllTransactionReportViewModel> GenerateAllTransactionReportAsync(DateTime startDate, DateTime endDate)
        {
            var cacheKey = $"report:all-transactions:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
            var cached = await _cacheService.GetAsync<AllTransactionReportViewModel>(cacheKey);
            if (cached != null) return cached;

            var transactions = await _context.Transactions
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            var transactionData = transactions.Select(t => (dynamic)new
            {
                t.Id,
                t.TransactionId,
                t.TransactionType,
                t.Amount,
                t.Description,
                t.ReferenceNumber,
                TransactionDate = t.TransactionDate.ToString("yyyy-MM-dd"),
                t.ProcessedBy,
                t.Status
            }).ToList();

            var totalPayments = transactions.Where(t => t.TransactionType == "Payment").Sum(t => t.Amount);
            var totalRefunds = transactions.Where(t => t.TransactionType == "Refund").Sum(t => t.Amount);
            var breakdown = transactions.GroupBy(t => t.TransactionType)
                .ToDictionary(g => g.Key ?? "Unknown", g => g.Sum(t => t.Amount));

            var result = new AllTransactionReportViewModel
            {
                TransactionData = transactionData,
                StartDate = startDate,
                EndDate = endDate,
                TotalAmount = transactions.Sum(t => t.Amount),
                TotalPayments = totalPayments,
                TotalRefunds = totalRefunds,
                TransactionCount = transactions.Count,
                BreakdownByType = breakdown
            };

            await _cacheService.SetAsync(cacheKey, result, 15);
            return result;
        }

        /// <summary>
        /// R3: Appointment Report - Appointments within a date range with statistics
        /// </summary>
        public async Task<AppointmentReportViewModel> GenerateAppointmentReportAsync(DateTime startDate, DateTime endDate)
        {
            var cacheKey = $"report:appointments:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
            var cached = await _cacheService.GetAsync<AppointmentReportViewModel>(cacheKey);
            if (cached != null) return cached;

            var appointments = await _context.Appointments
                .Where(a => a.AppointmentDate >= startDate && a.AppointmentDate <= endDate)
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            var appointmentData = appointments.Select(a => (dynamic)new
            {
                a.Id,
                a.AppointmentId,
                PatientName = a.Patient.FirstName + " " + a.Patient.LastName,
                DoctorName = a.Doctor.FirstName + " " + a.Doctor.LastName,
                AppointmentDate = a.AppointmentDate.ToString("yyyy-MM-dd"),
                AppointmentTime = a.AppointmentTime.ToString(@"hh\:mm"),
                a.Status,
                a.AppointmentType,
                a.Priority
            }).ToList();

            var completed = appointments.Count(a => a.Status == "Completed");
            var cancelled = appointments.Count(a => a.Status == "Cancelled");
            var completionRate = appointments.Count > 0 ? ((double)completed / appointments.Count) * 100 : 0;

            var appointmentsByType = appointments.GroupBy(a => a.AppointmentType ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            var appointmentsByDoctor = appointments.GroupBy(a => (a.Doctor.FirstName + " " + a.Doctor.LastName).Trim())
                .ToDictionary(g => g.Key, g => g.Count());

            var result = new AppointmentReportViewModel
            {
                AppointmentData = appointmentData,
                StartDate = startDate,
                EndDate = endDate,
                TotalAppointments = appointments.Count,
                CompletedAppointments = completed,
                CancelledAppointments = cancelled,
                ScheduledAppointments = appointments.Count(a => a.Status == "Scheduled"),
                CompletionRate = (decimal)completionRate,
                AppointmentsByType = appointmentsByType,
                AppointmentsByDoctor = appointmentsByDoctor
            };

            await _cacheService.SetAsync(cacheKey, result, 15);
            return result;
        }

        /// <summary>
        /// R4: OPD Report - Out-patient visits with consultation data
        /// </summary>
        public async Task<OPDReportViewModel> GenerateOPDReportAsync(DateTime startDate, DateTime endDate)
        {
            var cacheKey = $"report:opd:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
            var cached = await _cacheService.GetAsync<OPDReportViewModel>(cacheKey);
            if (cached != null) return cached;

            var visits = await _context.OPDVisits
                .Where(v => v.VisitDate >= startDate && v.VisitDate <= endDate)
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .OrderByDescending(v => v.VisitDate)
                .ToListAsync();

            var visitData = visits.Select(v => (dynamic)new
            {
                v.Id,
                PatientName = v.Patient.FirstName + " " + v.Patient.LastName,
                DoctorName = v.Doctor.FirstName + " " + v.Doctor.LastName,
                VisitDate = v.VisitDate.ToString("yyyy-MM-dd"),
                v.Diagnosis,
                v.ConsultationFee,
                v.PaymentStatus,
                v.CreatedBy
            }).ToList();

            var uniquePatients = visits.Select(v => v.PatientId).Distinct().Count();
            var paidVisits = visits.Count(v => v.PaymentStatus == "Paid");
            var pendingVisits = visits.Count(v => v.PaymentStatus == "Pending");
            var totalFees = visits.Sum(v => v.ConsultationFee);
            var avgFee = visits.Count > 0 ? totalFees / visits.Count : 0;

            var visitsByDoctor = visits.GroupBy(v => (v.Doctor.FirstName + " " + v.Doctor.LastName).Trim())
                .ToDictionary(g => g.Key, g => g.Count());

            var result = new OPDReportViewModel
            {
                OPDVisitData = visitData,
                StartDate = startDate,
                EndDate = endDate,
                TotalVisits = visits.Count,
                UniquePatients = uniquePatients,
                TotalConsultationFees = totalFees,
                AverageConsultationFee = avgFee,
                PaidVisits = paidVisits,
                PendingPaymentVisits = pendingVisits,
                VisitsByDoctor = visitsByDoctor
            };

            await _cacheService.SetAsync(cacheKey, result, 15);
            return result;
        }

        /// <summary>
        /// R5: IPD Report - In-patient admissions with length of stay data
        /// </summary>
        public async Task<IPDReportViewModel> GenerateIPDReportAsync(DateTime startDate, DateTime endDate)
        {
            var cacheKey = $"report:ipd:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
            var cached = await _cacheService.GetAsync<IPDReportViewModel>(cacheKey);
            if (cached != null) return cached;

            var admissions = await _context.IPDAdmissions
                .Where(a => a.AdmissionDate >= startDate && a.AdmissionDate <= endDate)
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Bed)
                .ThenInclude(b => b.Ward)
                .OrderByDescending(a => a.AdmissionDate)
                .ToListAsync();

            var admissionData = admissions.Select(a => (dynamic)new
            {
                a.Id,
                PatientName = a.Patient.FirstName + " " + a.Patient.LastName,
                DoctorName = a.Doctor.FirstName + " " + a.Doctor.LastName,
                WardName = a.Bed?.Ward?.Name ?? "Unknown",
                BedNumber = a.Bed?.BedNumber ?? "N/A",
                AdmissionDate = a.AdmissionDate.ToString("yyyy-MM-dd"),
                DischargeDate = a.DischargeDate.HasValue ? a.DischargeDate.Value.ToString("yyyy-MM-dd") : "Still Admitted",
                LengthOfStay = a.DischargeDate.HasValue 
                    ? (int)(a.DischargeDate.Value - a.AdmissionDate).TotalDays 
                    : (int)(DateTime.UtcNow - a.AdmissionDate).TotalDays,
                a.AdmissionType,
                a.Diagnosis,
                a.Status,
                DailyCharges = a.DailyCharges
            }).ToList();

            var discharged = admissions.Count(a => a.DischargeDate.HasValue);
            var currentlyAdmitted = admissions.Count(a => !a.DischargeDate.HasValue);
            var totalDailyCharges = admissions.Sum(a => a.DailyCharges);
            
            var avgLengthOfStay = 0.0;
            if (admissions.Count > 0)
            {
                var totalDays = admissions.Sum(a => a.DischargeDate.HasValue 
                    ? (int)(a.DischargeDate.Value - a.AdmissionDate).TotalDays 
                    : (int)(DateTime.UtcNow - a.AdmissionDate).TotalDays);
                avgLengthOfStay = (double)totalDays / admissions.Count;
            }

            var admissionsByType = admissions.GroupBy(a => a.AdmissionType ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            var admissionsByWard = admissions.GroupBy(a => a.Bed?.Ward?.Name ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            var result = new IPDReportViewModel
            {
                IPDAdmissionData = admissionData,
                StartDate = startDate,
                EndDate = endDate,
                TotalAdmissions = admissions.Count,
                DischargedPatients = discharged,
                CurrentlyAdmitted = currentlyAdmitted,
                AverageLengthOfStay = avgLengthOfStay,
                TotalDailyCharges = totalDailyCharges,
                AdmissionsByType = admissionsByType,
                AdmissionsByWard = admissionsByWard
            };

            await _cacheService.SetAsync(cacheKey, result, 15);
            return result;
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

        public async Task<GeneratedReport?> GetGeneratedReportByIdAsync(int reportId)
        {
            return await _context.GeneratedReports
                .Include(gr => gr.StaffGenerated)
                .FirstOrDefaultAsync(gr => gr.Id == reportId);
        }

        public async Task<IEnumerable<GeneratedReport>> GetGeneratedReportsAsync(string reportType = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var cacheKey = $"report:generated:{reportType ?? "all"}:{startDate?.ToString("yyyyMMdd") ?? "na"}:{endDate?.ToString("yyyyMMdd") ?? "na"}";
            var cached = await _cacheService.GetAsync<List<GeneratedReport>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var query = _context.GeneratedReports.AsQueryable();

            if (!string.IsNullOrEmpty(reportType))
                query = query.Where(gr => gr.ReportType == reportType);

            if (startDate.HasValue)
                query = query.Where(gr => gr.CreatedDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(gr => gr.CreatedDate <= endDate.Value);

            var reports = await query.OrderByDescending(gr => gr.CreatedDate)
                .Include(gr => gr.StaffGenerated)
                .ToListAsync();

            await _cacheService.SetAsync(cacheKey, reports, 5);
            return reports;
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

        private async Task<List<Dictionary<string, object>>> ExecuteStoredProcedureAsync(string procedureName, List<SqlParameter> parameters)
        {
            var rows = new List<Dictionary<string, object>>();
            var connection = _context.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var command = connection.CreateCommand();
            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[reader.GetName(i)] = value ?? string.Empty;
                }

                rows.Add(row);
            }

            return rows;
        }

        private static string GetString(Dictionary<string, object> row, string key)
        {
            return row.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        }

        private static int GetInt(Dictionary<string, object> row, string key)
        {
            if (!row.TryGetValue(key, out var value) || value == null)
            {
                return 0;
            }

            return int.TryParse(value.ToString(), out var result) ? result : 0;
        }

        private static decimal GetDecimal(Dictionary<string, object> row, string key)
        {
            if (!row.TryGetValue(key, out var value) || value == null)
            {
                return 0m;
            }

            return decimal.TryParse(value.ToString(), out var result) ? result : 0m;
        }

        private sealed class DepartmentReportCacheItem
        {
            public string StaffId { get; set; } = string.Empty;
            public string StaffName { get; set; } = string.Empty;
            public string Department { get; set; } = string.Empty;
            public int AttendanceDays { get; set; }
            public int ApprovedLeaves { get; set; }
        }

        private sealed class OccupancyAverageCacheItem
        {
            public double Value { get; set; }
        }
    }
}
