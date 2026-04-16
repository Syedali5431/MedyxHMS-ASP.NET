using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public class IPDService : IIPDService
    {
        private readonly ApplicationDbContext _context;

        public IPDService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<IPDAdmission>> GetAllIPDAdmissionsAsync()
        {
            return await _context.IPDAdmissions
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Bed)
                    .ThenInclude(b => b.Ward)
                .OrderByDescending(a => a.AdmissionDate)
                .ToListAsync();
        }

        public async Task<IPDAdmission> GetIPDAdmissionByIdAsync(int id)
        {
            return await _context.IPDAdmissions
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Bed)
                    .ThenInclude(b => b.Ward)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IPDAdmission> CreateIPDAdmissionAsync(IPDAdmission admission)
        {
            admission.CreatedDate = DateTime.UtcNow;
            admission.Status = "Admitted";

            // Update bed status if bed is assigned
            if (admission.BedId.HasValue)
            {
                var bed = await _context.Beds.FindAsync(admission.BedId.Value);
                if (bed != null)
                {
                    bed.Status = "Occupied";
                    _context.Beds.Update(bed);
                }
            }

            _context.IPDAdmissions.Add(admission);
            await _context.SaveChangesAsync();
            return admission;
        }

        public async Task<IPDAdmission> UpdateIPDAdmissionAsync(IPDAdmission admission)
        {
            // Handle bed status changes
            var existingAdmission = await _context.IPDAdmissions
                .Include(a => a.Bed)
                .FirstOrDefaultAsync(a => a.Id == admission.Id);

            if (existingAdmission != null)
            {
                // If bed changed, update bed statuses
                if (existingAdmission.BedId != admission.BedId)
                {
                    // Free up old bed
                    if (existingAdmission.BedId.HasValue)
                    {
                        var oldBed = await _context.Beds.FindAsync(existingAdmission.BedId.Value);
                        if (oldBed != null && oldBed.Status == "Occupied")
                        {
                            oldBed.Status = "Available";
                            _context.Beds.Update(oldBed);
                        }
                    }

                    // Occupy new bed
                    if (admission.BedId.HasValue)
                    {
                        var newBed = await _context.Beds.FindAsync(admission.BedId.Value);
                        if (newBed != null)
                        {
                            newBed.Status = "Occupied";
                            _context.Beds.Update(newBed);
                        }
                    }
                }

                // If discharging patient, free up bed
                if (admission.Status == "Discharged" && existingAdmission.Status != "Discharged")
                {
                    if (admission.BedId.HasValue)
                    {
                        var bed = await _context.Beds.FindAsync(admission.BedId.Value);
                        if (bed != null)
                        {
                            bed.Status = "Available";
                            _context.Beds.Update(bed);
                        }
                    }
                }
            }

            _context.IPDAdmissions.Update(admission);
            await _context.SaveChangesAsync();
            return admission;
        }

        public async Task<bool> DeleteIPDAdmissionAsync(int id)
        {
            var admission = await _context.IPDAdmissions.FindAsync(id);
            if (admission == null)
                return false;

            // Free up bed if occupied
            if (admission.BedId.HasValue)
            {
                var bed = await _context.Beds.FindAsync(admission.BedId.Value);
                if (bed != null && bed.Status == "Occupied")
                {
                    bed.Status = "Available";
                    _context.Beds.Update(bed);
                }
            }

            _context.IPDAdmissions.Remove(admission);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<IPDAdmission>> GetIPDAdmissionsByPatientAsync(int patientId)
        {
            return await _context.IPDAdmissions
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Bed)
                    .ThenInclude(b => b.Ward)
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.AdmissionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<IPDAdmission>> GetIPDAdmissionsByDoctorAsync(int doctorId)
        {
            return await _context.IPDAdmissions
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Bed)
                    .ThenInclude(b => b.Ward)
                .Where(a => a.DoctorId == doctorId)
                .OrderByDescending(a => a.AdmissionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<IPDAdmission>> GetIPDAdmissionsByStatusAsync(string status)
        {
            return await _context.IPDAdmissions
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Bed)
                    .ThenInclude(b => b.Ward)
                .Where(a => a.Status == status)
                .OrderByDescending(a => a.AdmissionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<IPDAdmission>> GetCurrentIPDAdmissionsAsync()
        {
            return await _context.IPDAdmissions
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Bed)
                    .ThenInclude(b => b.Ward)
                .Where(a => a.Status == "Admitted")
                .OrderBy(a => a.AdmissionDate)
                .ToListAsync();
        }

        public async Task<bool> DischargePatientAsync(int admissionId, DateTime dischargeDate)
        {
            var admission = await _context.IPDAdmissions
                .Include(a => a.Bed)
                .FirstOrDefaultAsync(a => a.Id == admissionId);

            if (admission == null || admission.Status == "Discharged")
                return false;

            admission.DischargeDate = dischargeDate;
            admission.Status = "Discharged";

            // Free up bed
            if (admission.BedId.HasValue)
            {
                var bed = await _context.Beds.FindAsync(admission.BedId.Value);
                if (bed != null)
                {
                    bed.Status = "Available";
                    _context.Beds.Update(bed);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<IPDAdmission>> GetIPDAdmissionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.IPDAdmissions
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Bed)
                    .ThenInclude(b => b.Ward)
                .Where(a => a.AdmissionDate >= startDate && a.AdmissionDate <= endDate)
                .OrderByDescending(a => a.AdmissionDate)
                .ToListAsync();
        }

        public async Task<int> GetCurrentAdmissionCountAsync()
        {
            return await _context.IPDAdmissions
                .Where(a => a.Status == "Admitted")
                .CountAsync();
        }

        public async Task<decimal> GetIPDRevenueAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.IPDAdmissions
                .Where(a => a.DischargeDate >= startDate && a.DischargeDate <= endDate && a.Status == "Discharged")
                .SumAsync(a => (a.DischargeDate.Value - a.AdmissionDate).Days * a.DailyCharges);
        }
    }
}