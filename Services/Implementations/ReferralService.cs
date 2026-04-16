using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public class ReferralService : IReferralService
    {
        private readonly ApplicationDbContext _context;

        public ReferralService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Referral>> GetReferralsAsync()
        {
            return await _context.Referrals
                .Include(x => x.Patient)
                .OrderByDescending(x => x.ReferralDate)
                .ToListAsync();
        }

        public async Task<Referral> GetReferralByIdAsync(int id)
        {
            return await _context.Referrals
                .Include(x => x.Patient)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Referral> CreateReferralAsync(Referral referral)
        {
            if (referral == null)
                throw new ArgumentNullException(nameof(referral));

            referral.CreatedDate = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(referral.Status))
                referral.Status = "Pending";

            _context.Referrals.Add(referral);
            await _context.SaveChangesAsync();

            if (string.Equals(referral.ReferralType, "TPA", StringComparison.OrdinalIgnoreCase) && referral.ApprovedAmount.HasValue && referral.ApprovedAmount.Value > 0)
            {
                var bill = new Bill
                {
                    PatientId = referral.PatientId,
                    BillDate = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(15),
                    TotalAmount = referral.ApprovedAmount.Value,
                    PaidAmount = 0,
                    PendingAmount = referral.ApprovedAmount.Value,
                    Status = "Unpaid",
                    BillType = "TPA",
                    Notes = $"TPA referral: {referral.TpaProvider} / Policy {referral.TpaPolicyNumber}"
                };
                _context.Bills.Add(bill);
                await _context.SaveChangesAsync();

                _context.BillItems.Add(new BillItem
                {
                    BillId = bill.Id,
                    ItemName = "TPA Referral",
                    ItemType = "TPA",
                    Quantity = 1,
                    UnitPrice = referral.ApprovedAmount.Value,
                    TotalPrice = referral.ApprovedAmount.Value,
                    Description = referral.ReferralReason
                });

                referral.BillId = bill.Id;
                _context.Referrals.Update(referral);
                await _context.SaveChangesAsync();
            }

            return referral;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var referral = await _context.Referrals.FindAsync(id);
            if (referral == null)
                return false;

            referral.Status = status;
            _context.Referrals.Update(referral);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
