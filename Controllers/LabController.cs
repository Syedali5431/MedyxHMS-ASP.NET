using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

// Purpose: Contains application code for LabController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Staff,Doctor,LabTechnician")]
    public class LabController : Controller
    {
        private readonly ILabService _labService;
        private readonly IPatientService _patientService;
        private readonly IAuditService _auditService;
        private readonly ApplicationDbContext _context;
        private readonly ISystemNotificationService _notificationService;

        public LabController(ILabService labService, IPatientService patientService, IAuditService auditService, ApplicationDbContext context, ISystemNotificationService notificationService)
        {
            _labService = labService;
            _patientService = patientService;
            _auditService = auditService;
            _context = context;
            _notificationService = notificationService;
        }

        // ======== Lab Test Management ========

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? search = null, DateTime? from = null, DateTime? to = null)
        {
            if (pageSize < 1) pageSize = 10;
            if (page < 1) page = 1;
            var allTests = await _labService.GetAllLabTestsAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                allTests = allTests.Where(t =>
                    (t.TestName != null && t.TestName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (t.TestCode != null && t.TestCode.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (t.Category != null && t.Category.Contains(search, StringComparison.OrdinalIgnoreCase))).ToList();
            }
            if (from.HasValue)
                allTests = allTests.Where(t => t.CreatedDate >= from.Value).ToList();
            if (to.HasValue)
                allTests = allTests.Where(t => t.CreatedDate <= to.Value.AddDays(1).AddTicks(-1)).ToList();

            var tests = allTests
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPages = (int)Math.Ceiling(allTests.Count() / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;
            ViewBag.FromDate = from?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to?.ToString("yyyy-MM-dd");

            return View(tests);
        }

        [HttpGet]
        public IActionResult CreateTest()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateTest(LabTest model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var newTest = await _labService.CreateLabTestAsync(model);
                await _auditService.LogActivityAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, "Create", "LabTest", newTest.Id.ToString(), null, $"Test: {newTest.TestName}");
                TempData["SuccessMessage"] = $"Lab test '{newTest.TestName}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating lab test: {ex.Message}");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditTest(int id)
        {
            var test = await _labService.GetLabTestByIdAsync(id);
            if (test == null)
                return NotFound();

            return View(test);
        }

        [HttpPost]
        public async Task<IActionResult> EditTest(int id, LabTest model)
        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var existingTest = await _labService.GetLabTestByIdAsync(id);
                var oldValues = $"Category: {existingTest.Category}, Price: {existingTest.Price}";

                var updatedTest = await _labService.UpdateLabTestAsync(model);
                var newValues = $"Category: {updatedTest.Category}, Price: {updatedTest.Price}";

                await _auditService.LogActivityAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, "Update", "LabTest", id.ToString(), oldValues, newValues);
                TempData["SuccessMessage"] = $"Lab test '{updatedTest.TestName}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating lab test: {ex.Message}");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTest(int id)
        {
            try
            {
                var test = await _labService.GetLabTestByIdAsync(id);
                if (test == null)
                    return NotFound();

                await _labService.DeleteLabTestAsync(id);
                await _auditService.LogActivityAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, "Delete", "LabTest", id.ToString(), $"Test: {test.TestName}", null);
                TempData["SuccessMessage"] = $"Lab test '{test.TestName}' deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting lab test: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ======== Lab Results Management ========

        public async Task<IActionResult> Results(int page = 1, int pageSize = 10, string status = "All")
        {
            if (pageSize < 1) pageSize = 10;
            if (page < 1) page = 1;
            IEnumerable<LabResult> results;

            if (status == "All")
                results = await _labService.GetAllLabResultsAsync();
            else
                results = await _labService.GetLabResultsByStatusAsync(status);

            var paginatedResults = results
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPages = (int)Math.Ceiling(results.Count() / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentStatus = status;

            return View(paginatedResults);
        }

        [HttpGet]
        public async Task<IActionResult> OrderTest()
        {
            var patients = await _patientService.GetAllPatientsAsync();
            var tests = await _labService.GetActiveLabTestsAsync();

            ViewBag.Patients = patients;
            ViewBag.Tests = tests;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> OrderTest(LabResult model)
        {
            if (!ModelState.IsValid)
            {
                var patients = await _patientService.GetAllPatientsAsync();
                var tests = await _labService.GetActiveLabTestsAsync();
                ViewBag.Patients = patients;
                ViewBag.Tests = tests;
                return View(model);
            }

            try
            {
                model.OrderDate = DateTime.UtcNow;
                var newResult = await _labService.CreateLabResultAsync(model);
                await _auditService.LogActivityAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, "Create", "LabResult", newResult.Id.ToString(), null, $"OrderNumber: {newResult.OrderNumber}");
                TempData["SuccessMessage"] = $"Lab test ordered successfully! Order #: {newResult.OrderNumber}";
                return RedirectToAction(nameof(Results));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error ordering lab test: {ex.Message}");
                var patients = await _patientService.GetAllPatientsAsync();
                var tests = await _labService.GetActiveLabTestsAsync();
                ViewBag.Patients = patients;
                ViewBag.Tests = tests;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResultDetails(int id)
        {
            var result = await _labService.GetLabResultByIdAsync(id);
            if (result == null)
                return NotFound();

            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> EditResult(int id)
        {
            var result = await _labService.GetLabResultByIdAsync(id);
            if (result == null)
                return NotFound();

            ViewBag.NoteHistory = await _context.LabNoteHistories
                .Where(h => h.LabResultId == id)
                .OrderByDescending(h => h.UpdatedAtUtc)
                .Take(20)
                .ToListAsync();

            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> EditResult(int id, LabResult model)
        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var existingResult = await _labService.GetLabResultByIdAsync(id);
                var oldValues = $"Status: {existingResult.Status}, Value: {existingResult.ResultValue}";

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var oldNotes = existingResult.Notes ?? "";
                var newNotes = model.Notes ?? "";

                // Save note history if notes changed
                if (!string.Equals(oldNotes, newNotes, StringComparison.Ordinal))
                {
                    _context.LabNoteHistories.Add(new LabNoteHistory
                    {
                        LabResultId = existingResult.Id,
                        Notes = newNotes,
                        UpdatedBy = currentUserId,
                        UpdatedAtUtc = DateTime.UtcNow
                    });

                    // Notify patient
                    var patient = await _patientService.GetPatientByIdAsync(existingResult.PatientId);
                    if (patient?.UserId != null)
                    {
                        await _notificationService.CreateForUserAsync(
                            patient.UserId,
                            "Lab report notes updated",
                            $"Notes for your lab order #{existingResult.OrderNumber} have been updated by the lab.",
                            "LabNote",
                            nameof(LabResult),
                            existingResult.Id.ToString());
                    }

                    await _context.SaveChangesAsync();
                }

                var updatedResult = await _labService.UpdateLabResultAsync(model);
                var newValues = $"Status: {updatedResult.Status}, Value: {updatedResult.ResultValue}";

                await _auditService.LogActivityAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, "Update", "LabResult", id.ToString(), oldValues, newValues);
                TempData["SuccessMessage"] = "Lab result updated successfully!";
                return RedirectToAction(nameof(ResultDetails), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating lab result: {ex.Message}");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateResultStatus(int id, string status)
        {
            try
            {
                var success = await _labService.UpdateLabResultStatusAsync(id, status);
                if (success)
                {
                    await _auditService.LogActivityAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, "Update", "LabResult", id.ToString(), null, $"Status updated to: {status}");
                    return Json(new { success = true, message = "Status updated successfully!" });
                }
                return Json(new { success = false, message = "Lab result not found!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteResult(int id)
        {
            try
            {
                var result = await _labService.GetLabResultByIdAsync(id);
                if (result == null)
                    return NotFound();

                await _labService.DeleteLabResultAsync(id);
                await _auditService.LogActivityAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, "Delete", "LabResult", id.ToString(), $"OrderNumber: {result.OrderNumber}", null);
                TempData["SuccessMessage"] = "Lab result deleted successfully!";
                return RedirectToAction(nameof(Results));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting lab result: {ex.Message}";
                return RedirectToAction(nameof(Results));
            }
        }

        // ======== AJAX Methods ========

        [HttpGet]
        public async Task<JsonResult> GetPatientLabResults(int patientId)
        {
            var results = await _labService.GetLabResultsByPatientAsync(patientId);
            return Json(results.Select(r => new
            {
                r.Id,
                r.OrderNumber,
                TestName = r.LabTest?.TestName,
                r.OrderDate,
                r.Status,
                r.ResultValue
            }));
        }

        [HttpGet]
        public async Task<JsonResult> GetPendingCount()
        {
            var count = await _labService.GetPendingLabTestsCountAsync();
            return Json(new { count });
        }

        [HttpGet]
        public async Task<JsonResult> GetTestsByCategory(string category)
        {
            var tests = await _labService.SearchLabTestsByCategoryAsync(category);
            return Json(tests.Select(t => new { t.Id, t.TestName, t.Price }));
        }
    }
}
