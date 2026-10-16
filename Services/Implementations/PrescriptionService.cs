using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for PrescriptionService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly ApplicationDbContext _context;

        public PrescriptionService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Prescription methods
        public async Task<IEnumerable<Prescription>> GetAllPrescriptionsAsync()
        {
            return await _context.Prescriptions
                .Include(p => p.Medicine)
                .Include(p => p.PharmacyBill)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
        }

        public async Task<Prescription> GetPrescriptionByIdAsync(int id)
        {
            return await _context.Prescriptions
                .Include(p => p.Medicine)
                .Include(p => p.PharmacyBill)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        /// <summary>
        /// Dispensing takes the quantity from the pharmacy stock of the bill's hospital. Throws InvalidOperationException
        /// (message for the user) when the medicine belongs to another hospital's pharmacy or the stock is too low.
        /// </summary>
        private async Task TakeFromStockAsync(int medicineId, int quantity, int pharmacyBillId)
        {
            var medicine = await _context.Medicines.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == medicineId)
                ?? throw new InvalidOperationException("Choose a medicine.");
            var billHospital = await _context.PharmacyBills.IgnoreQueryFilters()
                .Where(b => b.Id == pharmacyBillId).Select(b => b.HospitalId).FirstOrDefaultAsync();
            if (billHospital.HasValue && medicine.HospitalId.HasValue && medicine.HospitalId != billHospital)
                throw new InvalidOperationException($"{medicine.Name} is stock of another hospital's pharmacy. Choose the medicine from the pharmacy of the bill's hospital.");
            if (quantity > medicine.StockQuantity)
                throw new InvalidOperationException($"Not enough {medicine.Name} in stock: {medicine.StockQuantity} available, {quantity} prescribed.");
            medicine.StockQuantity -= quantity;
        }

        private async Task ReturnToStockAsync(int medicineId, int quantity)
        {
            var medicine = await _context.Medicines.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == medicineId);
            if (medicine != null && quantity > 0)
                medicine.StockQuantity += quantity;
        }

        public async Task<Prescription> CreatePrescriptionAsync(Prescription prescription)
        {
            prescription.CreatedDate = DateTime.Now;
            prescription.TotalPrice = prescription.Quantity * prescription.UnitPrice;

            await TakeFromStockAsync(prescription.MedicineId, prescription.Quantity, prescription.PharmacyBillId);
            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();
            await RecalculatePharmacyBillAsync(prescription.PharmacyBillId);
            return prescription;
        }

        public async Task<Prescription> UpdatePrescriptionAsync(Prescription prescription)
        {
            var previous = await _context.Prescriptions.AsNoTracking().Where(p => p.Id == prescription.Id)
                .Select(p => new { p.PharmacyBillId, p.MedicineId, p.Quantity }).FirstOrDefaultAsync();
            int? previousBillId = previous?.PharmacyBillId;
            prescription.TotalPrice = prescription.Quantity * prescription.UnitPrice;
            // Stock follows the change: the old quantity goes back, the new quantity is taken.
            if (previous != null)
                await ReturnToStockAsync(previous.MedicineId, previous.Quantity);
            await TakeFromStockAsync(prescription.MedicineId, prescription.Quantity, prescription.PharmacyBillId);
            _context.Prescriptions.Update(prescription);
            await _context.SaveChangesAsync();
            await RecalculatePharmacyBillAsync(prescription.PharmacyBillId);
            if (previousBillId.HasValue && previousBillId.Value != prescription.PharmacyBillId)
                await RecalculatePharmacyBillAsync(previousBillId.Value);
            return prescription;
        }

        public async Task<bool> DeletePrescriptionAsync(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription == null)
                return false;

            var billId = prescription.PharmacyBillId;
            // Medicines not given out go back into stock.
            await ReturnToStockAsync(prescription.MedicineId, prescription.Quantity);
            _context.Prescriptions.Remove(prescription);
            await _context.SaveChangesAsync();
            await RecalculatePharmacyBillAsync(billId);
            return true;
        }

        public async Task<IEnumerable<Prescription>> GetPrescriptionsByPharmacyBillAsync(int pharmacyBillId)
        {
            return await _context.Prescriptions
                .Include(p => p.Medicine)
                .Where(p => p.PharmacyBillId == pharmacyBillId)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Prescription>> GetPrescriptionsByPatientAsync(int patientId)
        {
            return await _context.Prescriptions
                .Include(p => p.Medicine)
                .Include(p => p.PharmacyBill)
                .Where(p => p.PharmacyBill.PatientId == patientId)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Prescription>> GetPrescriptionsByMedicineAsync(int medicineId)
        {
            return await _context.Prescriptions
                .Include(p => p.PharmacyBill)
                .Where(p => p.MedicineId == medicineId)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Prescription>> GetPrescriptionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Prescriptions
                .Include(p => p.Medicine)
                .Include(p => p.PharmacyBill)
                .Where(p => p.CreatedDate >= startDate && p.CreatedDate <= MedyxHMS.Extensions.DateRange.EndOfDay(endDate))
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
        }

        // Medicine methods
        public async Task<IEnumerable<Medicine>> GetMedicinesForNewPrescriptionsAsync()
        {
            var hospitalId = await _context.HospitalForNewRecordsAsync();
            return await _context.Medicines
                .Where(m => m.IsActive && (hospitalId == null || m.HospitalId == hospitalId))
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Medicine>> GetAllMedicinesAsync()
        {
            return await _context.Medicines
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<Medicine?> GetMedicineByIdAsync(int id)
        {
            return await _context.Medicines.FindAsync(id);
        }

        public async Task<Medicine> CreateMedicineAsync(Medicine medicine)
        {
            medicine.CreatedDate = DateTime.Now;
            _context.Medicines.Add(medicine);
            await _context.SaveChangesAsync();
            return medicine;
        }

        public async Task<Medicine> UpdateMedicineAsync(Medicine medicine)
        {
            _context.Medicines.Update(medicine);
            await _context.SaveChangesAsync();
            return medicine;
        }

        public async Task<bool> DeleteMedicineAsync(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null)
                return false;

            _context.Medicines.Remove(medicine);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Medicine>> GetLowStockMedicinesAsync()
        {
            return await _context.Medicines
                .Where(m => m.IsActive && m.StockQuantity <= m.MinStockLevel)
                .OrderBy(m => m.StockQuantity)
                .ToListAsync();
        }

        public async Task<IEnumerable<Medicine>> GetExpiringMedicinesAsync(int daysAhead = 30)
        {
            var expiryDate = DateTime.Now.AddDays(daysAhead);
            return await _context.Medicines
                .Where(m => m.IsActive && m.ExpiryDate <= expiryDate && m.ExpiryDate > DateTime.Now)
                .OrderBy(m => m.ExpiryDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Medicine>> SearchMedicinesAsync(string searchTerm)
        {
            return await _context.Medicines
                .Where(m => m.IsActive && (
                    m.Name.Contains(searchTerm) ||
                    m.GenericName.Contains(searchTerm) ||
                    m.Category.Contains(searchTerm) ||
                    m.Manufacturer.Contains(searchTerm)
                ))
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<bool> UpdateMedicineStockAsync(int medicineId, int quantityUsed)
        {
            var medicine = await _context.Medicines.FindAsync(medicineId);
            if (medicine == null || medicine.StockQuantity < quantityUsed)
                return false;

            medicine.StockQuantity -= quantityUsed;
            _context.Medicines.Update(medicine);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetTotalMedicineStockAsync()
        {
            return await _context.Medicines
                .Where(m => m.IsActive)
                .SumAsync(m => m.StockQuantity);
        }

        // Pharmacy Bill methods
        public async Task<IEnumerable<PharmacyBill>> GetAllPharmacyBillsAsync()
        {
            return await _context.PharmacyBills
                .Include(b => b.Patient)
                .Include(b => b.Prescriptions)
                    .ThenInclude(p => p.Medicine)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();
        }

        public async Task<PharmacyBill?> GetPharmacyBillByIdAsync(int id)
        {
            return await _context.PharmacyBills
                .Include(b => b.Patient)
                .Include(b => b.Prescriptions)
                    .ThenInclude(p => p.Medicine)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<PharmacyBill> CreatePharmacyBillAsync(PharmacyBill bill)
        {
            bill.CreatedDate = DateTime.Now;
            bill.Status = "Pending";

            _context.PharmacyBills.Add(bill);
            await _context.SaveChangesAsync();
            return bill;
        }

        public async Task<PharmacyBill> UpdatePharmacyBillAsync(PharmacyBill bill)
        {
            _context.PharmacyBills.Update(bill);
            await _context.SaveChangesAsync();
            return bill;
        }

        public async Task<bool> DeletePharmacyBillAsync(int id)
        {
            var bill = await _context.PharmacyBills
                .Include(b => b.Prescriptions)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null)
                return false;

            // Remove associated prescriptions
            _context.Prescriptions.RemoveRange(bill.Prescriptions);
            _context.PharmacyBills.Remove(bill);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<PharmacyBill>> GetPharmacyBillsByPatientAsync(int patientId)
        {
            return await _context.PharmacyBills
                .Include(b => b.Patient)
                .Include(b => b.Prescriptions)
                    .ThenInclude(p => p.Medicine)
                .Where(b => b.PatientId == patientId)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PharmacyBill>> GetPharmacyBillsByStatusAsync(string status)
        {
            return await _context.PharmacyBills
                .Include(b => b.Patient)
                .Include(b => b.Prescriptions)
                    .ThenInclude(p => p.Medicine)
                .Where(b => b.Status == status)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PharmacyBill>> GetPharmacyBillsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.PharmacyBills
                .Include(b => b.Patient)
                .Include(b => b.Prescriptions)
                    .ThenInclude(p => p.Medicine)
                .Where(b => b.BillDate >= startDate && b.BillDate <= MedyxHMS.Extensions.DateRange.EndOfDay(endDate))
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();
        }

        public async Task<decimal> GetPharmacyRevenueAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.PharmacyBills
                .Where(b => b.BillDate >= startDate && b.BillDate <= endDate && b.Status == "Paid")
                .SumAsync(b => b.PaidAmount);
        }

        public async Task<int> GetTotalPrescriptionsCountAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Prescriptions
                .Where(p => p.CreatedDate >= startDate && p.CreatedDate <= MedyxHMS.Extensions.DateRange.EndOfDay(endDate))
                .CountAsync();
        }

        /// <summary>
        /// Keeps a pharmacy bill equal to the medicines on it: total = sum of its prescriptions, and the bill is
        /// Pending while the paid amount is below the total (previously adding a medicine left the old total).
        /// </summary>
        private async Task RecalculatePharmacyBillAsync(int pharmacyBillId)
        {
            var bill = await _context.PharmacyBills.FirstOrDefaultAsync(b => b.Id == pharmacyBillId);
            if (bill == null || bill.Status == "Cancelled")
                return;

            var total = await _context.Prescriptions.Where(p => p.PharmacyBillId == pharmacyBillId)
                .SumAsync(p => (decimal?)(p.TotalPrice > 0 ? p.TotalPrice : p.Quantity * p.UnitPrice)) ?? 0m;
            bill.TotalAmount = total;
            bill.Status = total > 0 && bill.PaidAmount >= total ? "Paid" : "Pending";
            await _context.SaveChangesAsync();
        }
    }
}
