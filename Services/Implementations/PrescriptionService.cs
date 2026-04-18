using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

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

        public async Task<Prescription> CreatePrescriptionAsync(Prescription prescription)
        {
            prescription.CreatedDate = DateTime.UtcNow;
            prescription.TotalPrice = prescription.Quantity * prescription.UnitPrice;

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();
            return prescription;
        }

        public async Task<Prescription> UpdatePrescriptionAsync(Prescription prescription)
        {
            _context.Prescriptions.Update(prescription);
            await _context.SaveChangesAsync();
            return prescription;
        }

        public async Task<bool> DeletePrescriptionAsync(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription == null)
                return false;

            _context.Prescriptions.Remove(prescription);
            await _context.SaveChangesAsync();
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
                .Where(p => p.CreatedDate >= startDate && p.CreatedDate <= endDate)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
        }

        // Medicine methods
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
            medicine.CreatedDate = DateTime.UtcNow;
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
            var expiryDate = DateTime.UtcNow.AddDays(daysAhead);
            return await _context.Medicines
                .Where(m => m.IsActive && m.ExpiryDate <= expiryDate && m.ExpiryDate > DateTime.UtcNow)
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
            bill.CreatedDate = DateTime.UtcNow;
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
                .Where(b => b.BillDate >= startDate && b.BillDate <= endDate)
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
                .Where(p => p.CreatedDate >= startDate && p.CreatedDate <= endDate)
                .CountAsync();
        }
    }
}
