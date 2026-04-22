using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for LabService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class LabService : ILabService
    {
        private readonly ApplicationDbContext _context;

        public LabService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======== Lab Test Catalog Methods ========

        public async Task<IEnumerable<LabTest>> GetAllLabTestsAsync()
        {
            return await _context.LabTests
                .OrderBy(t => t.TestName)
                .ToListAsync();
        }

        public async Task<LabTest?> GetLabTestByIdAsync(int id)
        {
            return await _context.LabTests.FindAsync(id);
        }

        public async Task<LabTest> CreateLabTestAsync(LabTest labTest)
        {
            if (labTest == null)
                throw new ArgumentNullException(nameof(labTest));

            labTest.CreatedDate = DateTime.UtcNow;
            _context.LabTests.Add(labTest);
            await _context.SaveChangesAsync();
            return labTest;
        }

        public async Task<LabTest?> UpdateLabTestAsync(LabTest labTest)
        {
            if (labTest == null)
                throw new ArgumentNullException(nameof(labTest));

            var existingTest = await _context.LabTests.FindAsync(labTest.Id);
            if (existingTest == null)
                return null;

            existingTest.TestName = labTest.TestName ?? existingTest.TestName;
            existingTest.Category = labTest.Category ?? existingTest.Category;
            existingTest.Description = labTest.Description ?? existingTest.Description;
            existingTest.Price = labTest.Price > 0 ? labTest.Price : existingTest.Price;
            existingTest.NormalRange = labTest.NormalRange ?? existingTest.NormalRange;
            existingTest.PreparationTimeHours = labTest.PreparationTimeHours >= 0 ? labTest.PreparationTimeHours : existingTest.PreparationTimeHours;
            existingTest.IsActive = labTest.IsActive;

            _context.LabTests.Update(existingTest);
            await _context.SaveChangesAsync();
            return existingTest;
        }

        public async Task<bool> DeleteLabTestAsync(int id)
        {
            var labTest = await _context.LabTests.FindAsync(id);
            if (labTest == null)
                return false;

            _context.LabTests.Remove(labTest);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<LabTest>> GetActiveLabTestsAsync()
        {
            return await _context.LabTests
                .Where(t => t.IsActive)
                .OrderBy(t => t.TestName)
                .ToListAsync();
        }

        public async Task<IEnumerable<LabTest>> SearchLabTestsByCategoryAsync(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return new List<LabTest>();

            return await _context.LabTests
                .Where(t => t.Category.Contains(category))
                .OrderBy(t => t.TestName)
                .ToListAsync();
        }

        public async Task<IEnumerable<LabTest>> SearchLabTestsByNameAsync(string testName)
        {
            if (string.IsNullOrWhiteSpace(testName))
                return new List<LabTest>();

            return await _context.LabTests
                .Where(t => t.TestName.Contains(testName) || t.TestCode.Contains(testName))
                .OrderBy(t => t.TestName)
                .ToListAsync();
        }

        // ======== Lab Result Methods ========

        public async Task<IEnumerable<LabResult>> GetAllLabResultsAsync()
        {
            return await _context.LabResults
                .Include(r => r.Patient)
                .Include(r => r.LabTest)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<LabResult?> GetLabResultByIdAsync(int id)
        {
            return await _context.LabResults
                .Include(r => r.Patient)
                .Include(r => r.LabTest)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<LabResult> CreateLabResultAsync(LabResult labResult)
        {
            if (labResult == null)
                throw new ArgumentNullException(nameof(labResult));

            if (!string.IsNullOrEmpty(labResult.OrderNumber) == false)
                labResult.OrderNumber = $"LAB-{DateTime.UtcNow:yyyyMMddHHmmss}";

            labResult.CreatedDate = DateTime.UtcNow;
            labResult.Status = "Ordered";

            _context.LabResults.Add(labResult);
            await _context.SaveChangesAsync();
            return labResult;
        }

        public async Task<LabResult?> UpdateLabResultAsync(LabResult labResult)
        {
            if (labResult == null)
                throw new ArgumentNullException(nameof(labResult));

            var existingResult = await _context.LabResults.FindAsync(labResult.Id);
            if (existingResult == null)
                return null;

            existingResult.ResultValue = labResult.ResultValue ?? existingResult.ResultValue;
            existingResult.NormalRange = labResult.NormalRange ?? existingResult.NormalRange;
            existingResult.Unit = labResult.Unit ?? existingResult.Unit;
            existingResult.Interpretation = labResult.Interpretation ?? existingResult.Interpretation;
            existingResult.Status = labResult.Status ?? existingResult.Status;
            existingResult.PerformedBy = labResult.PerformedBy ?? existingResult.PerformedBy;
            existingResult.VerifiedBy = labResult.VerifiedBy ?? existingResult.VerifiedBy;
            existingResult.Notes = labResult.Notes ?? existingResult.Notes;

            if (labResult.ResultDate.HasValue)
                existingResult.ResultDate = labResult.ResultDate;

            _context.LabResults.Update(existingResult);
            await _context.SaveChangesAsync();
            return existingResult;
        }

        public async Task<bool> DeleteLabResultAsync(int id)
        {
            var labResult = await _context.LabResults.FindAsync(id);
            if (labResult == null)
                return false;

            _context.LabResults.Remove(labResult);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<LabResult>> GetLabResultsByPatientAsync(int patientId)
        {
            return await _context.LabResults
                .Include(r => r.Patient)
                .Include(r => r.LabTest)
                .Where(r => r.PatientId == patientId)
                .OrderByDescending(r => r.OrderDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<LabResult>> GetLabResultsByStatusAsync(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return new List<LabResult>();

            return await _context.LabResults
                .Include(r => r.Patient)
                .Include(r => r.LabTest)
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.OrderDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<LabResult>> GetPendingLabResultsAsync()
        {
            return await _context.LabResults
                .Include(r => r.Patient)
                .Include(r => r.LabTest)
                .Where(r => r.Status == "Ordered" || r.Status == "In Progress")
                .OrderBy(r => r.OrderDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<LabResult>> GetLabResultsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.LabResults
                .Include(r => r.Patient)
                .Include(r => r.LabTest)
                .Where(r => r.OrderDate >= startDate && r.OrderDate <= endDate)
                .OrderByDescending(r => r.OrderDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<LabResult>> GetPatientLabResultsByTestAsync(int patientId, int testId)
        {
            return await _context.LabResults
                .Include(r => r.Patient)
                .Include(r => r.LabTest)
                .Where(r => r.PatientId == patientId && r.LabTestId == testId)
                .OrderByDescending(r => r.OrderDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateLabResultStatusAsync(int labResultId, string status)
        {
            var labResult = await _context.LabResults.FindAsync(labResultId);
            if (labResult == null)
                return false;

            labResult.Status = status;
            if (status == "Completed" && !labResult.ResultDate.HasValue)
                labResult.ResultDate = DateTime.UtcNow;

            _context.LabResults.Update(labResult);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetPendingLabTestsCountAsync()
        {
            return await _context.LabResults
                .Where(r => r.Status == "Ordered" || r.Status == "In Progress")
                .CountAsync();
        }

        public async Task<decimal> GetLabRevenueAsync(DateTime startDate, DateTime endDate)
        {
            var completedResults = await _context.LabResults
                .Include(r => r.LabTest)
                .Where(r => r.Status == "Completed" && 
                       r.ResultDate >= startDate && 
                       r.ResultDate <= endDate)
                .ToListAsync();

            return completedResults.Sum(r => r.LabTest?.Price ?? 0);
        }
    }
}
