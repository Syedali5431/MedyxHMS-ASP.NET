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
    }
}
