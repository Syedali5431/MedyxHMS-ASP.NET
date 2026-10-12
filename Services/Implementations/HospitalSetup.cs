using MedyxHMS.Data;
using MedyxHMS.Models;
using Microsoft.EntityFrameworkCore;

// Purpose: Records every hospital of the group needs from the start (multi-hospital).
namespace MedyxHMS.Services.Implementations
{
    public static class HospitalSetup
    {
        public static readonly string[] BloodGroups = { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };

        /// <summary>
        /// Each hospital keeps its own blood stock: one row per blood group. Missing rows are added with no units
        /// (the stock is then recorded on Blood Bank → Inventory). For one hospital, or for every hospital when
        /// <paramref name="hospitalId"/> is null. Returns the number of rows added.
        /// </summary>
        public static async Task<int> EnsureBloodStockAsync(ApplicationDbContext db, int? hospitalId = null)
        {
            var hospitalIds = hospitalId.HasValue
                ? new List<int> { hospitalId.Value }
                : await db.Hospitals.Select(h => h.Id).ToListAsync();
            var existing = await db.BloodInventories.IgnoreQueryFilters()
                .Where(b => b.HospitalId != null && hospitalIds.Contains(b.HospitalId.Value))
                .Select(b => new { b.HospitalId, b.BloodGroup })
                .ToListAsync();

            var added = 0;
            foreach (var id in hospitalIds)
            {
                foreach (var group in BloodGroups)
                {
                    if (existing.Any(e => e.HospitalId == id && string.Equals(e.BloodGroup, group, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    db.BloodInventories.Add(new BloodInventory
                    {
                        HospitalId = id,
                        BloodGroup = group,
                        UnitsAvailable = 0,
                        UnitsReserved = 0,
                        MinimumLevel = 5,
                        LastUpdatedDate = DateTime.Now,
                        CreatedDate = DateTime.Now
                    });
                    added++;
                }
            }

            if (added > 0)
                await db.SaveChangesAsync();
            return added;
        }
    }
}
