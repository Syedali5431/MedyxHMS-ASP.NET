using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for OPDService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class OPDService : IOPDService
    {
        private readonly ApplicationDbContext _context;

        public OPDService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<OPDVisit>> GetAllOPDVisitsAsync()
        {
            return await _context.OPDVisits
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .OrderByDescending(v => v.VisitDate)
                .ToListAsync();
        }

        public async Task<OPDVisit> GetOPDVisitByIdAsync(int id)
        {
            return await _context.OPDVisits
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<OPDVisit> CreateOPDVisitAsync(OPDVisit visit)
        {
            visit.CreatedDate = DateTime.UtcNow;
            _context.OPDVisits.Add(visit);
            await _context.SaveChangesAsync();
            return visit;
        }

        public async Task<OPDVisit> UpdateOPDVisitAsync(OPDVisit visit)
        {
            _context.OPDVisits.Update(visit);
            await _context.SaveChangesAsync();
            return visit;
        }

        public async Task<bool> DeleteOPDVisitAsync(int id)
        {
            var visit = await _context.OPDVisits.FindAsync(id);
            if (visit == null)
                return false;

            _context.OPDVisits.Remove(visit);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<OPDVisit>> GetOPDVisitsByPatientAsync(int patientId)
        {
            return await _context.OPDVisits
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .Where(v => v.PatientId == patientId)
                .OrderByDescending(v => v.VisitDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<OPDVisit>> GetOPDVisitsByDoctorAsync(int doctorId)
        {
            return await _context.OPDVisits
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .Where(v => v.DoctorId == doctorId)
                .OrderByDescending(v => v.VisitDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<OPDVisit>> GetOPDVisitsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.OPDVisits
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .Where(v => v.VisitDate >= startDate && v.VisitDate <= endDate)
                .OrderByDescending(v => v.VisitDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<OPDVisit>> GetTodayOPDVisitsAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            return await _context.OPDVisits
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .Where(v => v.VisitDate >= today && v.VisitDate < tomorrow)
                .OrderBy(v => v.VisitDate)
                .ToListAsync();
        }

        public async Task<int> GetOPDVisitCountAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.OPDVisits
                .Where(v => v.VisitDate >= startDate && v.VisitDate <= endDate)
                .CountAsync();
        }

        public async Task<decimal> GetOPDRevenueAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.OPDVisits
                .Where(v => v.VisitDate >= startDate && v.VisitDate <= endDate && v.PaymentStatus == "Paid")
                .SumAsync(v => v.ConsultationFee);
        }
    }
}
