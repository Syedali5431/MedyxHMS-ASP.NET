using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public class BillingService : IBillingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISystemNotificationService _systemNotificationService;

        public BillingService(ApplicationDbContext context, ISystemNotificationService systemNotificationService)
        {
            _context = context;
            _systemNotificationService = systemNotificationService;
        }

        public async Task<IEnumerable<Bill>> GetAllBillsAsync()
        {
            return await _context.Bills
                .Include(b => b.Patient)
                .Include(b => b.BillItems)
                .Include(b => b.Payments)
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();
        }

        public async Task<Bill> GetBillByIdAsync(int id)
        {
            return await _context.Bills
                .Include(b => b.Patient)
                .Include(b => b.BillItems)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Bill> CreateBillAsync(Bill bill)
        {
            bill.BillNumber = GenerateBillNumber();
            bill.CreatedDate = DateTime.UtcNow;
            bill.Status = "Unpaid";
            bill.PendingAmount = bill.TotalAmount;

            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            await NotifyInvoiceChangeAsync(bill, "Invoice generated", "InvoiceCreated");

            return bill;
        }

        public async Task<Bill> UpdateBillAsync(Bill bill)
        {
            var existingBill = await _context.Bills.FindAsync(bill.Id);
            if (existingBill == null)
                return null;

            existingBill.TotalAmount = bill.TotalAmount;
            existingBill.PaidAmount = bill.PaidAmount;
            existingBill.PendingAmount = bill.TotalAmount - bill.PaidAmount;
            existingBill.Status = GetBillStatus(existingBill.PendingAmount);
            existingBill.Notes = bill.Notes;

            await _context.SaveChangesAsync();

            await NotifyInvoiceChangeAsync(existingBill, "Invoice updated", "InvoiceUpdated");
            return existingBill;
        }

        private async Task NotifyInvoiceChangeAsync(Bill bill, string title, string notificationType)
        {
            var persistedBill = await _context.Bills
                .Include(b => b.Patient)
                .FirstOrDefaultAsync(b => b.Id == bill.Id);

            if (persistedBill == null)
                return;

            var message = $"{title}: {persistedBill.BillNumber} for patient {persistedBill.Patient?.FirstName} {persistedBill.Patient?.LastName}.";

            if (persistedBill.Patient?.UserId != null)
            {
                await _systemNotificationService.CreateForUserAsync(
                    persistedBill.Patient.UserId,
                    title,
                    message,
                    notificationType,
                    "Bill",
                    persistedBill.Id.ToString(),
                    persistedBill.PatientId);
            }

            await _systemNotificationService.NotifyRolesAsync(
                new[] { "Admin", "SuperAdmin", "Receptionist", "Accountant" },
                title,
                message,
                notificationType,
                "Bill",
                persistedBill.Id.ToString());
        }

        public async Task<bool> DeleteBillAsync(int id)
        {
            var bill = await _context.Bills.FindAsync(id);
            if (bill == null)
                return false;

            _context.Bills.Remove(bill);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Bill>> GetBillsByPatientAsync(int patientId)
        {
            return await _context.Bills
                .Include(b => b.BillItems)
                .Include(b => b.Payments)
                .Where(b => b.PatientId == patientId)
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();
        }

        public async Task<bool> ProcessPaymentAsync(Payment payment)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Add payment
                payment.PaymentDate = DateTime.UtcNow;
                payment.Status = "Completed";
                _context.Payments.Add(payment);

                // Update bill
                var bill = await _context.Bills.FindAsync(payment.BillId);
                if (bill != null)
                {
                    bill.PaidAmount += payment.Amount;
                    bill.PendingAmount = bill.TotalAmount - bill.PaidAmount;
                    bill.Status = GetBillStatus(bill.PendingAmount);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<decimal> GetTotalRevenueAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Payments
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate && p.Status == "Completed")
                .SumAsync(p => p.Amount);
        }

        private string GenerateBillNumber()
        {
            // Generate bill number: BILL + YYYYMMDD + sequential number
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var lastBill = _context.Bills
                .Where(b => b.BillNumber.StartsWith($"BILL{datePart}"))
                .OrderByDescending(b => b.Id)
                .FirstOrDefault();

            int sequentialNumber = 1;
            if (lastBill != null)
            {
                var lastNumber = lastBill.BillNumber.Substring(12); // Remove "BILLYYYYMMDD"
                if (int.TryParse(lastNumber, out int num))
                    sequentialNumber = num + 1;
            }

            return $"BILL{datePart}{sequentialNumber:D4}";
        }

        private string GetBillStatus(decimal pendingAmount)
        {
            if (pendingAmount <= 0)
                return "Paid";
            else if (pendingAmount > 0)
                return "Partially Paid";
            else
                return "Unpaid";
        }
    }
}