using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for BloodBankService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class BloodBankService : IBloodBankService
    {
        private readonly ApplicationDbContext _context;

        public BloodBankService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BloodInventory>> GetBloodInventoryAsync()
        {
            return await _context.BloodInventories
                .OrderBy(x => x.BloodGroup)
                .ToListAsync();
        }

        public async Task<BloodInventory> UpsertInventoryAsync(string bloodGroup, int unitsAvailable, int minimumLevel)
        {
            if (string.IsNullOrWhiteSpace(bloodGroup))
                throw new ArgumentException("Blood group is required.", nameof(bloodGroup));

            var existing = await _context.BloodInventories.FirstOrDefaultAsync(x => x.BloodGroup == bloodGroup);
            if (existing == null)
            {
                existing = new BloodInventory
                {
                    BloodGroup = bloodGroup,
                    UnitsAvailable = unitsAvailable,
                    MinimumLevel = minimumLevel,
                    LastUpdatedDate = DateTime.UtcNow
                };
                _context.BloodInventories.Add(existing);
            }
            else
            {
                existing.UnitsAvailable = unitsAvailable;
                existing.MinimumLevel = minimumLevel;
                existing.LastUpdatedDate = DateTime.UtcNow;
                _context.BloodInventories.Update(existing);
            }

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<IEnumerable<BloodIssue>> GetBloodIssuesAsync()
        {
            return await _context.BloodIssues
                .Include(x => x.Patient)
                .OrderByDescending(x => x.IssueDate)
                .ToListAsync();
        }

        public async Task<BloodIssue> IssueBloodAsync(BloodIssue issue)
        {
            if (issue == null)
                throw new ArgumentNullException(nameof(issue));

            var inventory = await _context.BloodInventories.FirstOrDefaultAsync(x => x.BloodGroup == issue.BloodGroup);
            if (inventory == null)
                throw new InvalidOperationException($"No inventory found for blood group {issue.BloodGroup}.");

            if (issue.UnitsIssued <= 0)
                throw new InvalidOperationException("Units issued must be greater than zero.");

            if (inventory.UnitsAvailable < issue.UnitsIssued)
                throw new InvalidOperationException("Insufficient blood units available.");

            inventory.UnitsAvailable -= issue.UnitsIssued;
            inventory.LastUpdatedDate = DateTime.UtcNow;

            var chargeAmount = issue.UnitsIssued * 1500m;
            var bill = new Bill
            {
                PatientId = issue.PatientId,
                BillDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(7),
                TotalAmount = chargeAmount,
                PaidAmount = 0,
                PendingAmount = chargeAmount,
                Status = "Unpaid",
                BillType = "BloodBank",
                Notes = $"Blood issue for group {issue.BloodGroup} ({issue.UnitsIssued} units)."
            };
            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            _context.BillItems.Add(new BillItem
            {
                BillId = bill.Id,
                ItemName = $"Blood Issue - {issue.BloodGroup}",
                ItemType = "BloodBank",
                Quantity = issue.UnitsIssued,
                UnitPrice = 1500m,
                TotalPrice = chargeAmount,
                Description = "Blood bank issue"
            });

            issue.BillId = bill.Id;
            issue.IssueDate = DateTime.UtcNow;
            _context.BloodIssues.Add(issue);

            await _context.SaveChangesAsync();
            return issue;
        }

        public async Task<bool> DeleteBloodIssueAsync(int id)
        {
            var issue = await _context.BloodIssues.FindAsync(id);
            if (issue == null)
                return false;

            _context.BloodIssues.Remove(issue);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
