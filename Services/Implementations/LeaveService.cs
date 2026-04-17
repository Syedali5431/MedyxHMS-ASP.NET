using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public class LeaveService : ILeaveService
    {
        private readonly ApplicationDbContext _context;

        public LeaveService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LeaveType>> GetLeaveTypesAsync(bool activeOnly = false)
        {
            var query = _context.LeaveTypes.AsQueryable();
            if (activeOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            return await query
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<LeaveType> CreateLeaveTypeAsync(LeaveType leaveType)
        {
            if (leaveType == null)
                throw new ArgumentNullException(nameof(leaveType));

            if (string.IsNullOrWhiteSpace(leaveType.Name))
                throw new InvalidOperationException("Leave type name is required.");

            leaveType.CreatedDate = DateTime.UtcNow;
            _context.LeaveTypes.Add(leaveType);
            await _context.SaveChangesAsync();
            return leaveType;
        }

        public async Task<LeaveType> UpdateLeaveTypeAsync(LeaveType leaveType)
        {
            if (leaveType == null)
                throw new ArgumentNullException(nameof(leaveType));

            var existing = await _context.LeaveTypes.FindAsync(leaveType.Id);
            if (existing == null)
                return null;

            existing.Name = leaveType.Name;
            existing.Description = leaveType.Description;
            existing.DefaultDaysPerYear = leaveType.DefaultDaysPerYear;
            existing.IsActive = leaveType.IsActive;

            _context.LeaveTypes.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<IEnumerable<LeaveRequest>> GetLeaveRequestsAsync(string staffId = null, string status = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.LeaveRequests
                .Include(x => x.Staff)
                .ThenInclude(s => s.User)
                .Include(x => x.LeaveType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(staffId))
            {
                query = query.Where(x => x.StaffId == staffId);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.StartDate.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.EndDate.Date <= endDate.Value.Date);
            }

            return await query
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<LeaveRequest> GetLeaveRequestByIdAsync(int id)
        {
            return await _context.LeaveRequests
                .Include(x => x.Staff)
                .ThenInclude(s => s.User)
                .Include(x => x.LeaveType)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<LeaveRequest> CreateLeaveRequestAsync(LeaveRequest leaveRequest)
        {
            if (leaveRequest == null)
                throw new ArgumentNullException(nameof(leaveRequest));

            if (leaveRequest.StartDate.Date > leaveRequest.EndDate.Date)
                throw new InvalidOperationException("Start date cannot be after end date.");

            leaveRequest.StartDate = leaveRequest.StartDate.Date;
            leaveRequest.EndDate = leaveRequest.EndDate.Date;
            leaveRequest.TotalDays = (leaveRequest.EndDate - leaveRequest.StartDate).Days + 1;
            leaveRequest.Status = "Pending";
            leaveRequest.CreatedDate = DateTime.UtcNow;

            var balance = await EnsureBalanceAsync(leaveRequest.StaffId, leaveRequest.LeaveTypeId, leaveRequest.StartDate.Year);
            if (balance.RemainingDays < leaveRequest.TotalDays)
                throw new InvalidOperationException("Requested leave exceeds available balance.");

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();
            return leaveRequest;
        }

        public async Task<bool> UpdateLeaveRequestStatusAsync(int requestId, string status, string approverId, string remarks = null)
        {
            var request = await _context.LeaveRequests.FindAsync(requestId);
            if (request == null)
                return false;

            var normalizedStatus = status?.Trim();
            if (normalizedStatus != "Approved" && normalizedStatus != "Rejected" && normalizedStatus != "Cancelled")
                throw new InvalidOperationException("Invalid leave request status.");

            if (request.Status == normalizedStatus)
                return true;

            if (request.Status == "Approved" && normalizedStatus != "Approved")
                throw new InvalidOperationException("Changing an approved request is not supported.");

            if (normalizedStatus == "Approved")
            {
                var balance = await EnsureBalanceAsync(request.StaffId, request.LeaveTypeId, request.StartDate.Year);
                if (balance.RemainingDays < request.TotalDays)
                    throw new InvalidOperationException("Insufficient leave balance for approval.");

                balance.UsedDays += request.TotalDays;
                balance.RemainingDays = Math.Max(0, balance.AllocatedDays - balance.UsedDays);
                balance.UpdatedDate = DateTime.UtcNow;
                _context.LeaveBalances.Update(balance);
            }

            request.Status = normalizedStatus;
            request.ApproverId = approverId;
            request.ApproverRemarks = remarks;
            request.ApprovedDate = normalizedStatus == "Approved" ? DateTime.UtcNow : null;
            request.UpdatedDate = DateTime.UtcNow;

            _context.LeaveRequests.Update(request);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<LeaveBalance>> GetLeaveBalancesAsync(string staffId = null, int? year = null)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var query = _context.LeaveBalances
                .Include(x => x.Staff)
                .ThenInclude(s => s.User)
                .Include(x => x.LeaveType)
                .Where(x => x.Year == targetYear)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(staffId))
            {
                query = query.Where(x => x.StaffId == staffId);
            }

            return await query
                .OrderBy(x => x.Staff.FirstName)
                .ThenBy(x => x.LeaveType.Name)
                .ToListAsync();
        }

        private async Task<LeaveBalance> EnsureBalanceAsync(string staffId, int leaveTypeId, int year)
        {
            var existing = await _context.LeaveBalances
                .FirstOrDefaultAsync(x => x.StaffId == staffId && x.LeaveTypeId == leaveTypeId && x.Year == year);

            if (existing != null)
                return existing;

            var leaveType = await _context.LeaveTypes.FindAsync(leaveTypeId);
            if (leaveType == null)
                throw new InvalidOperationException("Leave type not found.");

            var balance = new LeaveBalance
            {
                StaffId = staffId,
                LeaveTypeId = leaveTypeId,
                Year = year,
                AllocatedDays = leaveType.DefaultDaysPerYear,
                UsedDays = 0,
                RemainingDays = leaveType.DefaultDaysPerYear,
                CreatedDate = DateTime.UtcNow
            };

            _context.LeaveBalances.Add(balance);
            await _context.SaveChangesAsync();
            return balance;
        }
    }
}
