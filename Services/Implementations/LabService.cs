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
        private readonly LabTraceabilityService _traceability;

        public LabService(ApplicationDbContext context, LabTraceabilityService traceability)
        {
            _context = context;
            _traceability = traceability;
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

            labTest.CreatedDate = DateTime.Now;
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

            if (string.IsNullOrWhiteSpace(labResult.OrderNumber))
            {
                // Generated numbers have one-second resolution; add a suffix if that number is already taken.
                var baseNumber = $"LAB-{DateTime.Now:yyyyMMddHHmmss}";
                labResult.OrderNumber = baseNumber;
                for (var n = 2; await _context.LabResults.AnyAsync(r => r.OrderNumber == labResult.OrderNumber); n++)
                    labResult.OrderNumber = $"{baseNumber}-{n}";
            }
            else if (await _context.LabResults.AnyAsync(r => r.OrderNumber == labResult.OrderNumber))
            {
                throw new InvalidOperationException($"Order number '{labResult.OrderNumber}' is already in use. Leave it blank to generate one.");
            }

            labResult.CreatedDate = DateTime.Now;
            labResult.Status = "Ordered";

            // Specimen traceability: accession number for the sample label, default sample type, chain of custody.
            labResult.AccessionNumber = await _traceability.NextAccessionNumberAsync();
            if (string.IsNullOrWhiteSpace(labResult.SampleType))
            {
                var category = await _context.LabTests.Where(t => t.Id == labResult.LabTestId).Select(t => t.Category).FirstOrDefaultAsync();
                labResult.SampleType = LabTraceabilityService.DefaultSampleType(category);
            }
            labResult.SampleStatus = "Awaiting collection";
            // Nothing about collection, receipt or sign-off can be pre-filled through the order form.
            labResult.SignedOffAt = null;
            labResult.SignedOffBy = string.Empty;
            labResult.SignedOffByUserId = string.Empty;
            labResult.SignatureHash = string.Empty;
            labResult.VerifiedBy = string.Empty;
            labResult.CollectedAt = null;
            labResult.CollectedBy = string.Empty;
            labResult.ReceivedAt = null;
            labResult.ReceivedBy = string.Empty;
            labResult.RejectionReason = string.Empty;
            labResult.ResultEnteredAt = null;
            labResult.ResultEnteredBy = string.Empty;

            _context.LabResults.Add(labResult);
            _traceability.AddEvent(labResult, "Ordered", $"Order {labResult.OrderNumber}, accession {labResult.AccessionNumber}, sample {labResult.SampleType}");
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

            // A signed-off (authorised) result is locked; the sign-off must be revoked first.
            if (existingResult.SignedOffAt.HasValue)
                throw new InvalidOperationException("This result has been signed off and is locked.");

            var previousValue = existingResult.ResultValue ?? string.Empty;
            existingResult.ResultValue = labResult.ResultValue ?? existingResult.ResultValue;
            // The edit form does not post these fields; a blank value used to wipe them on every save.
            existingResult.NormalRange = string.IsNullOrEmpty(labResult.NormalRange) ? existingResult.NormalRange : labResult.NormalRange;
            existingResult.Unit = string.IsNullOrEmpty(labResult.Unit) ? existingResult.Unit : labResult.Unit;
            existingResult.Interpretation = labResult.Interpretation ?? existingResult.Interpretation;
            existingResult.Status = labResult.Status ?? existingResult.Status;
            existingResult.PerformedBy = string.IsNullOrEmpty(labResult.PerformedBy) ? existingResult.PerformedBy : labResult.PerformedBy;
            // VerifiedBy is set only by the electronic sign-off.
            existingResult.Notes = labResult.Notes ?? existingResult.Notes;

            if (!string.Equals(previousValue, existingResult.ResultValue ?? string.Empty, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(existingResult.ResultValue))
            {
                var who = _traceability.CurrentUserName;
                existingResult.ResultEnteredAt = DateTime.Now;
                existingResult.ResultEnteredBy = who;
                if (string.IsNullOrWhiteSpace(existingResult.PerformedBy)) existingResult.PerformedBy = who;
                _traceability.AddEvent(existingResult, string.IsNullOrEmpty(previousValue) ? "Result entered" : "Result changed",
                    string.IsNullOrEmpty(previousValue) ? existingResult.ResultValue! : $"{previousValue} -> {existingResult.ResultValue}");
            }

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
                .Where(r => r.OrderDate >= startDate && r.OrderDate <= MedyxHMS.Extensions.DateRange.EndOfDay(endDate))
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
            if (labResult == null || labResult.SignedOffAt.HasValue)
                return false;

            labResult.Status = status;
            if (status == "Completed" && !labResult.ResultDate.HasValue)
                labResult.ResultDate = DateTime.Now;

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
                       r.ResultDate <= MedyxHMS.Extensions.DateRange.EndOfDay(endDate))
                .ToListAsync();

            return completedResults.Sum(r => r.LabTest?.Price ?? 0);
        }
    }
}
