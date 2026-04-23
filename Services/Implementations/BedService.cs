using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for BedService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class BedService : IBedService
    {
        private readonly ApplicationDbContext _context;

        public BedService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Bed>> GetAllBedsAsync()
        {
            return await _context.Beds
                .Include(b => b.Ward)
                .OrderBy(b => b.Ward.Name)
                .ThenBy(b => b.BedNumber)
                .ToListAsync();
        }

        public async Task<Bed> GetBedByIdAsync(int id)
        {
            return await _context.Beds
                .Include(b => b.Ward)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Bed> CreateBedAsync(Bed bed)
        {
            bed.CreatedDate = DateTime.UtcNow;
            _context.Beds.Add(bed);
            await _context.SaveChangesAsync();
            return bed;
        }

        public async Task<Bed> UpdateBedAsync(Bed bed)
        {
            _context.Beds.Update(bed);
            await _context.SaveChangesAsync();
            return bed;
        }

        public async Task<bool> DeleteBedAsync(int id)
        {
            var bed = await _context.Beds.FindAsync(id);
            if (bed == null)
                return false;

            // Check if bed is occupied
            if (bed.Status == "Occupied")
                return false; // Cannot delete occupied bed

            _context.Beds.Remove(bed);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Bed>> GetBedsByWardAsync(int wardId)
        {
            return await _context.Beds
                .Include(b => b.Ward)
                .Where(b => b.WardId == wardId)
                .OrderBy(b => b.BedNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Bed>> GetAvailableBedsAsync()
        {
            return await _context.Beds
                .Include(b => b.Ward)
                .Where(b => b.Status == "Available" && b.IsActive)
                .OrderBy(b => b.Ward.Name)
                .ThenBy(b => b.BedNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Bed>> GetBedsByStatusAsync(string status)
        {
            return await _context.Beds
                .Include(b => b.Ward)
                .Where(b => b.Status == status && b.IsActive)
                .OrderBy(b => b.Ward.Name)
                .ThenBy(b => b.BedNumber)
                .ToListAsync();
        }

        public async Task<bool> UpdateBedStatusAsync(int bedId, string status)
        {
            var bed = await _context.Beds.FindAsync(bedId);
            if (bed == null)
                return false;

            bed.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Bed> GetBedByBedNumberAsync(string bedNumber, int wardId)
        {
            return await _context.Beds
                .Include(b => b.Ward)
                .FirstOrDefaultAsync(b => b.BedNumber == bedNumber && b.WardId == wardId);
        }

        public async Task<(bool Success, string Error)> AssignBedAsync(int bedId, int patientId, string requestingRole)
        {
            var bed = await _context.Beds.Include(b => b.Ward).FirstOrDefaultAsync(b => b.Id == bedId);
            if (bed == null)
                return (false, "Bed not found.");

            if (bed.Status != "Available")
                return (false, $"Bed {bed.BedNumber} is not available (current status: {bed.Status}).");

            // ICU beds require Admin/SuperAdmin approval
            if (bed.RequiresAdminApproval &&
                !string.Equals(requestingRole, "Admin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(requestingRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "ICU bed assignment requires Admin approval. Please contact an Admin.");
            }

            bed.PatientId = patientId;
            bed.Status = "Occupied";
            bed.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(bool Success, string Error)> ReleaseBedAsync(int bedId)
        {
            var bed = await _context.Beds.FindAsync(bedId);
            if (bed == null)
                return (false, "Bed not found.");

            if (bed.Status != "Occupied")
                return (false, $"Bed {bed.BedNumber} is not currently occupied.");

            // On discharge, status moves to Cleaning (not directly Available)
            bed.PatientId = null;
            bed.Status = "Cleaning";
            bed.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(bool Success, string Error)> TransferBedAsync(int fromBedId, int toBedId)
        {
            var fromBed = await _context.Beds.FindAsync(fromBedId);
            var toBed   = await _context.Beds.FindAsync(toBedId);

            if (fromBed == null) return (false, "Source bed not found.");
            if (toBed   == null) return (false, "Target bed not found.");
            if (fromBed.Status != "Occupied") return (false, "Source bed is not occupied.");
            if (toBed.Status   != "Available") return (false, "Target bed is not available.");

            var now = DateTime.UtcNow;
            toBed.PatientId   = fromBed.PatientId;
            toBed.Status      = "Occupied";
            toBed.LastUpdated = now;

            fromBed.PatientId   = null;
            fromBed.Status      = "Cleaning";
            fromBed.LastUpdated = now;

            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<BedManagementSummary> GetBedManagementSummaryAsync()
        {
            var beds = await _context.Beds.Where(b => b.IsActive).ToListAsync();
            return new BedManagementSummary
            {
                TotalBeds       = beds.Count,
                AvailableBeds   = beds.Count(b => b.Status == "Available"),
                OccupiedBeds    = beds.Count(b => b.Status == "Occupied"),
                CleaningBeds    = beds.Count(b => b.Status == "Cleaning"),
                MaintenanceBeds = beds.Count(b => b.Status == "Maintenance")
            };
        }
    }
}
