using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for WardService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class WardService : IWardService
    {
        private readonly ApplicationDbContext _context;

        public WardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Ward>> GetAllWardsAsync()
        {
            return await _context.Wards
                .Include(w => w.Beds)
                .OrderBy(w => w.Name)
                .ToListAsync();
        }

        public async Task<Ward> GetWardByIdAsync(int id)
        {
            return await _context.Wards
                .Include(w => w.Beds)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<Ward> CreateWardAsync(Ward ward)
        {
            ward.CreatedDate = DateTime.UtcNow;
            ward.OccupiedBeds = 0; // New ward starts with no occupied beds

            _context.Wards.Add(ward);
            await _context.SaveChangesAsync();
            return ward;
        }

        public async Task<Ward> UpdateWardAsync(Ward ward)
        {
            _context.Wards.Update(ward);
            await _context.SaveChangesAsync();
            return ward;
        }

        public async Task<bool> DeleteWardAsync(int id)
        {
            var ward = await _context.Wards
                .Include(w => w.Beds)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (ward == null)
                return false;

            // Check if ward has occupied beds
            if (ward.Beds.Any(b => b.Status == "Occupied"))
                return false; // Cannot delete ward with occupied beds

            _context.Wards.Remove(ward);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Ward>> GetActiveWardsAsync()
        {
            return await _context.Wards
                .Include(w => w.Beds)
                .Where(w => w.IsActive)
                .OrderBy(w => w.Name)
                .ToListAsync();
        }

        public async Task<int> GetAvailableBedCountAsync(int wardId)
        {
            return await _context.Beds
                .Where(b => b.WardId == wardId && b.Status == "Available" && b.IsActive)
                .CountAsync();
        }

        public async Task<int> GetOccupiedBedCountAsync(int wardId)
        {
            return await _context.Beds
                .Where(b => b.WardId == wardId && b.Status == "Occupied")
                .CountAsync();
        }

        public async Task<double> GetWardOccupancyRateAsync(int wardId)
        {
            var ward = await _context.Wards.FindAsync(wardId);
            if (ward == null || ward.TotalBeds == 0)
                return 0;

            var occupiedBeds = await GetOccupiedBedCountAsync(wardId);
            return (double)occupiedBeds / ward.TotalBeds * 100;
        }
    }
}
