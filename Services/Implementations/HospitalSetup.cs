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

        /// <summary>
        /// Each hospital's pharmacy has its own medicines (stock, price, batch and expiry):
        /// a hospital without medicines gets the default hospital's active medicines with no stock, and prescriptions of
        /// a hospital's pharmacy bills that point at another hospital's medicine are moved to the same medicine of their
        /// own hospital (added without stock when missing). For one hospital, or every hospital when
        /// <paramref name="hospitalId"/> is null. Returns the number of medicines added plus prescriptions moved.
        /// </summary>
        public static async Task<int> EnsureMedicinesAsync(ApplicationDbContext db, int? hospitalId = null)
        {
            var defaultId = await db.Hospitals.Where(h => h.IsActive).OrderByDescending(h => h.IsDefault).ThenBy(h => h.Id)
                .Select(h => (int?)h.Id).FirstOrDefaultAsync();
            if (defaultId == null)
                return 0;

            var hospitalIds = hospitalId.HasValue
                ? new List<int> { hospitalId.Value }
                : await db.Hospitals.OrderBy(h => h.Id).Select(h => h.Id).ToListAsync();
            var medicines = await db.Medicines.IgnoreQueryFilters().ToListAsync();
            static string Key(Medicine m) =>
                $"{m.Name?.Trim().ToUpperInvariant()}|{m.Strength?.Trim().ToUpperInvariant()}|{m.DosageForm?.Trim().ToUpperInvariant()}";

            var changes = 0;
            foreach (var id in hospitalIds)
            {
                var own = medicines.Where(m => m.HospitalId == id).ToList();
                Medicine CopyInto(Medicine source)
                {
                    var copy = new Medicine
                    {
                        HospitalId = id,
                        Name = source.Name,
                        GenericName = source.GenericName,
                        Category = source.Category,
                        DosageForm = source.DosageForm,
                        Strength = source.Strength,
                        Manufacturer = source.Manufacturer,
                        UnitPrice = source.UnitPrice,
                        StockQuantity = 0,
                        MinStockLevel = source.MinStockLevel,
                        ExpiryDate = source.ExpiryDate,
                        BatchNumber = source.BatchNumber,
                        IsActive = source.IsActive,
                        CreatedDate = DateTime.Now
                    };
                    db.Medicines.Add(copy);
                    medicines.Add(copy);
                    own.Add(copy);
                    changes++;
                    return copy;
                }

                if (id != defaultId && own.Count == 0)
                {
                    foreach (var source in medicines.Where(m => m.HospitalId == defaultId && m.IsActive).ToList())
                        CopyInto(source);
                }

                var misplaced = await db.Prescriptions.IgnoreQueryFilters()
                    .Where(p => p.PharmacyBill.HospitalId == id && p.Medicine.HospitalId != id)
                    .ToListAsync();
                if (misplaced.Count == 0)
                {
                    await db.SaveChangesAsync();
                    continue;
                }

                // Copies first (they need their ids), then the prescriptions.
                var target = new Dictionary<int, Medicine>();
                foreach (var medicineId in misplaced.Select(p => p.MedicineId).Distinct())
                {
                    var source = medicines.FirstOrDefault(m => m.Id == medicineId);
                    if (source == null) continue;
                    target[medicineId] = own.FirstOrDefault(m => Key(m) == Key(source)) ?? CopyInto(source);
                }
                await db.SaveChangesAsync();
                foreach (var prescription in misplaced.Where(p => target.ContainsKey(p.MedicineId)))
                {
                    prescription.MedicineId = target[prescription.MedicineId].Id;
                    changes++;
                }
                await db.SaveChangesAsync();
            }
            return changes;
        }
    }
}
