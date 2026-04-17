using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public class FrontOfficeService : IFrontOfficeService
    {
        private readonly ApplicationDbContext _context;

        public FrontOfficeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<VisitorLog>> GetVisitorsAsync(DateTime? date = null)
        {
            var targetDate = date?.Date;
            var query = _context.VisitorLogs.AsQueryable();
            if (targetDate.HasValue)
            {
                query = query.Where(x => x.VisitDate == targetDate.Value);
            }

            return await query
                .OrderByDescending(x => x.CheckInTime)
                .ToListAsync();
        }

        public async Task<VisitorLog> AddVisitorAsync(VisitorLog visitor)
        {
            if (visitor == null)
                throw new ArgumentNullException(nameof(visitor));

            visitor.VisitDate = (visitor.VisitDate == default ? DateTime.Today : visitor.VisitDate.Date);
            if (visitor.CheckInTime == default)
                visitor.CheckInTime = DateTime.UtcNow;

            _context.VisitorLogs.Add(visitor);
            await _context.SaveChangesAsync();
            return visitor;
        }

        public async Task<bool> CheckOutVisitorAsync(int visitorId, DateTime checkOutTime, string notes = null)
        {
            var visitor = await _context.VisitorLogs.FindAsync(visitorId);
            if (visitor == null)
                return false;

            visitor.CheckOutTime = checkOutTime;
            visitor.Status = "CheckedOut";
            if (!string.IsNullOrWhiteSpace(notes))
            {
                visitor.Notes = string.IsNullOrWhiteSpace(visitor.Notes) ? notes : $"{visitor.Notes}; {notes}";
            }

            _context.VisitorLogs.Update(visitor);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ComplaintRecord>> GetComplaintsAsync(string status = null)
        {
            var query = _context.ComplaintRecords.AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            return await query
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<ComplaintRecord> AddComplaintAsync(ComplaintRecord complaint)
        {
            if (complaint == null)
                throw new ArgumentNullException(nameof(complaint));

            complaint.CreatedDate = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(complaint.Status))
                complaint.Status = "Open";

            _context.ComplaintRecords.Add(complaint);
            await _context.SaveChangesAsync();
            return complaint;
        }

        public async Task<bool> UpdateComplaintStatusAsync(int complaintId, string status, string resolutionNotes = null)
        {
            var complaint = await _context.ComplaintRecords.FindAsync(complaintId);
            if (complaint == null)
                return false;

            complaint.Status = status;
            complaint.ResolutionNotes = resolutionNotes;
            if (status == "Resolved" || status == "Closed")
                complaint.ResolvedDate = DateTime.UtcNow;

            _context.ComplaintRecords.Update(complaint);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<DispatchReceiveRecord>> GetDispatchReceiveRecordsAsync(string recordType = null, DateTime? date = null)
        {
            var query = _context.DispatchReceiveRecords.AsQueryable();
            if (!string.IsNullOrWhiteSpace(recordType))
            {
                query = query.Where(x => x.RecordType == recordType);
            }

            if (date.HasValue)
            {
                var day = date.Value.Date;
                query = query.Where(x => x.RecordDate.Date == day);
            }

            return await query
                .OrderByDescending(x => x.RecordDate)
                .ToListAsync();
        }

        public async Task<DispatchReceiveRecord> AddDispatchReceiveRecordAsync(DispatchReceiveRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            if (string.IsNullOrWhiteSpace(record.RecordType))
                throw new InvalidOperationException("Record type is required.");

            if (record.RecordDate == default)
                record.RecordDate = DateTime.UtcNow;

            _context.DispatchReceiveRecords.Add(record);
            await _context.SaveChangesAsync();
            return record;
        }
    }
}
