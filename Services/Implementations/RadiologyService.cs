using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public class RadiologyService : IRadiologyService
    {
        private readonly ApplicationDbContext _context;

        public RadiologyService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======== Radiology Test Catalog Methods ========

        public async Task<IEnumerable<RadiologyTest>> GetAllRadiologyTestsAsync()
        {
            return await _context.RadiologyTests
                .OrderBy(t => t.TestName)
                .ToListAsync();
        }

        public async Task<RadiologyTest> GetRadiologyTestByIdAsync(int id)
        {
            return await _context.RadiologyTests.FindAsync(id);
        }

        public async Task<RadiologyTest> CreateRadiologyTestAsync(RadiologyTest radiologyTest)
        {
            if (radiologyTest == null)
                throw new ArgumentNullException(nameof(radiologyTest));

            radiologyTest.CreatedDate = DateTime.UtcNow;
            _context.RadiologyTests.Add(radiologyTest);
            await _context.SaveChangesAsync();
            return radiologyTest;
        }

        public async Task<RadiologyTest> UpdateRadiologyTestAsync(RadiologyTest radiologyTest)
        {
            if (radiologyTest == null)
                throw new ArgumentNullException(nameof(radiologyTest));

            var existingTest = await _context.RadiologyTests.FindAsync(radiologyTest.Id);
            if (existingTest == null)
                return null;

            existingTest.TestName = radiologyTest.TestName ?? existingTest.TestName;
            existingTest.Category = radiologyTest.Category ?? existingTest.Category;
            existingTest.Description = radiologyTest.Description ?? existingTest.Description;
            existingTest.Price = radiologyTest.Price > 0 ? radiologyTest.Price : existingTest.Price;
            existingTest.PreparationTimeHours = radiologyTest.PreparationTimeHours >= 0 ? radiologyTest.PreparationTimeHours : existingTest.PreparationTimeHours;
            existingTest.SpecialInstructions = radiologyTest.SpecialInstructions ?? existingTest.SpecialInstructions;
            existingTest.RequiresContrast = radiologyTest.RequiresContrast;
            existingTest.IsActive = radiologyTest.IsActive;

            _context.RadiologyTests.Update(existingTest);
            await _context.SaveChangesAsync();
            return existingTest;
        }

        public async Task<bool> DeleteRadiologyTestAsync(int id)
        {
            var radiologyTest = await _context.RadiologyTests.FindAsync(id);
            if (radiologyTest == null)
                return false;

            _context.RadiologyTests.Remove(radiologyTest);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<RadiologyTest>> GetActiveRadiologyTestsAsync()
        {
            return await _context.RadiologyTests
                .Where(t => t.IsActive)
                .OrderBy(t => t.TestName)
                .ToListAsync();
        }

        public async Task<IEnumerable<RadiologyTest>> SearchRadiologyTestsByCategoryAsync(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return new List<RadiologyTest>();

            return await _context.RadiologyTests
                .Where(t => t.Category.Contains(category))
                .OrderBy(t => t.TestName)
                .ToListAsync();
        }

        public async Task<IEnumerable<RadiologyTest>> SearchRadiologyTestsByNameAsync(string testName)
        {
            if (string.IsNullOrWhiteSpace(testName))
                return new List<RadiologyTest>();

            return await _context.RadiologyTests
                .Where(t => t.TestName.Contains(testName) || t.TestCode.Contains(testName))
                .OrderBy(t => t.TestName)
                .ToListAsync();
        }

        // ======== Radiology Result Methods ========

        public async Task<IEnumerable<RadiologyResult>> GetAllRadiologyResultsAsync()
        {
            return await _context.RadiologyResults
                .Include(r => r.Patient)
                .Include(r => r.RadiologyTest)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<RadiologyResult> GetRadiologyResultByIdAsync(int id)
        {
            return await _context.RadiologyResults
                .Include(r => r.Patient)
                .Include(r => r.RadiologyTest)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<RadiologyResult> CreateRadiologyResultAsync(RadiologyResult radiologyResult)
        {
            if (radiologyResult == null)
                throw new ArgumentNullException(nameof(radiologyResult));

            if (string.IsNullOrEmpty(radiologyResult.OrderNumber))
                radiologyResult.OrderNumber = $"RAD-{DateTime.UtcNow:yyyyMMddHHmmss}";

            radiologyResult.CreatedDate = DateTime.UtcNow;
            radiologyResult.Status = "Ordered";

            _context.RadiologyResults.Add(radiologyResult);
            await _context.SaveChangesAsync();
            return radiologyResult;
        }

        public async Task<RadiologyResult> UpdateRadiologyResultAsync(RadiologyResult radiologyResult)
        {
            if (radiologyResult == null)
                throw new ArgumentNullException(nameof(radiologyResult));

            var existingResult = await _context.RadiologyResults.FindAsync(radiologyResult.Id);
            if (existingResult == null)
                return null;

            existingResult.Findings = radiologyResult.Findings ?? existingResult.Findings;
            existingResult.Impression = radiologyResult.Impression ?? existingResult.Impression;
            existingResult.Status = radiologyResult.Status ?? existingResult.Status;
            existingResult.PerformedBy = radiologyResult.PerformedBy ?? existingResult.PerformedBy;
            existingResult.VerifiedBy = radiologyResult.VerifiedBy ?? existingResult.VerifiedBy;
            existingResult.ImagePath = radiologyResult.ImagePath ?? existingResult.ImagePath;
            existingResult.Notes = radiologyResult.Notes ?? existingResult.Notes;

            if (radiologyResult.ResultDate.HasValue)
                existingResult.ResultDate = radiologyResult.ResultDate;

            _context.RadiologyResults.Update(existingResult);
            await _context.SaveChangesAsync();
            return existingResult;
        }

        public async Task<bool> DeleteRadiologyResultAsync(int id)
        {
            var radiologyResult = await _context.RadiologyResults.FindAsync(id);
            if (radiologyResult == null)
                return false;

            _context.RadiologyResults.Remove(radiologyResult);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<RadiologyResult>> GetRadiologyResultsByPatientAsync(int patientId)
        {
            return await _context.RadiologyResults
                .Include(r => r.Patient)
                .Include(r => r.RadiologyTest)
                .Where(r => r.PatientId == patientId)
                .OrderByDescending(r => r.OrderDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<RadiologyResult>> GetRadiologyResultsByStatusAsync(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return new List<RadiologyResult>();

            return await _context.RadiologyResults
                .Include(r => r.Patient)
                .Include(r => r.RadiologyTest)
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.OrderDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<RadiologyResult>> GetPendingRadiologyResultsAsync()
        {
            return await _context.RadiologyResults
                .Include(r => r.Patient)
                .Include(r => r.RadiologyTest)
                .Where(r => r.Status == "Ordered" || r.Status == "In Progress")
                .OrderBy(r => r.OrderDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<RadiologyResult>> GetRadiologyResultsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.RadiologyResults
                .Include(r => r.Patient)
                .Include(r => r.RadiologyTest)
                .Where(r => r.OrderDate >= startDate && r.OrderDate <= endDate)
                .OrderByDescending(r => r.OrderDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<RadiologyResult>> GetPatientRadiologyResultsByTestAsync(int patientId, int testId)
        {
            return await _context.RadiologyResults
                .Include(r => r.Patient)
                .Include(r => r.RadiologyTest)
                .Where(r => r.PatientId == patientId && r.RadiologyTestId == testId)
                .OrderByDescending(r => r.OrderDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateRadiologyResultStatusAsync(int radiologyResultId, string status)
        {
            var radiologyResult = await _context.RadiologyResults.FindAsync(radiologyResultId);
            if (radiologyResult == null)
                return false;

            radiologyResult.Status = status;
            if (status == "Completed" && !radiologyResult.ResultDate.HasValue)
                radiologyResult.ResultDate = DateTime.UtcNow;

            _context.RadiologyResults.Update(radiologyResult);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetPendingRadiologyTestsCountAsync()
        {
            return await _context.RadiologyResults
                .Where(r => r.Status == "Ordered" || r.Status == "In Progress")
                .CountAsync();
        }

        public async Task<decimal> GetRadiologyRevenueAsync(DateTime startDate, DateTime endDate)
        {
            var completedResults = await _context.RadiologyResults
                .Include(r => r.RadiologyTest)
                .Where(r => r.Status == "Completed" && 
                       r.ResultDate >= startDate && 
                       r.ResultDate <= endDate)
                .ToListAsync();

            return completedResults.Sum(r => r.RadiologyTest?.Price ?? 0);
        }
    }
}
