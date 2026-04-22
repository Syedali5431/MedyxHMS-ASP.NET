using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// Purpose: Contains application code for RadiologyController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Staff,Doctor")]
    public class RadiologyController : Controller
    {
        private readonly IRadiologyService _radiologyService;
        private readonly IPatientService _patientService;
        private readonly IAuditService _auditService;
        private readonly IFileService _fileService;

        public RadiologyController(IRadiologyService radiologyService, IPatientService patientService, IAuditService auditService, IFileService fileService)
        {
            _radiologyService = radiologyService;
            _patientService = patientService;
            _auditService = auditService;
            _fileService = fileService;
        }

        // ======== Radiology Test Management ========

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var allTests = await _radiologyService.GetAllRadiologyTestsAsync();
            var tests = allTests
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPages = (int)Math.Ceiling(allTests.Count() / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            return View(tests);
        }

        [HttpGet]
        public IActionResult CreateTest()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateTest(RadiologyTest model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var newTest = await _radiologyService.CreateRadiologyTestAsync(model);
                await _auditService.LogActivityAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, "Create", "RadiologyTest", newTest.Id.ToString(), null, $"Test: {newTest.TestName}");
                TempData["SuccessMessage"] = $"Radiology test '{newTest.TestName}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating radiology test: {ex.Message}");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditTest(int id)
        {
            var test = await _radiologyService.GetRadiologyTestByIdAsync(id);
            if (test == null)
                return NotFound();

            return View(test);
        }

        [HttpPost]
        public async Task<IActionResult> EditTest(int id, RadiologyTest model)
        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var existingTest = await _radiologyService.GetRadiologyTestByIdAsync(id);
                var oldValues = $"Category: {existingTest.Category}, Price: {existingTest.Price}, RequiresContrast: {existingTest.RequiresContrast}";

                var updatedTest = await _radiologyService.UpdateRadiologyTestAsync(model);
                var newValues = $"Category: {updatedTest.Category}, Price: {updatedTest.Price}, RequiresContrast: {updatedTest.RequiresContrast}";

                await _auditService.LogActivityAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, "Update", "RadiologyTest", id.ToString(), oldValues, newValues);
                TempData["SuccessMessage"] = $"Radiology test '{updatedTest.TestName}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating radiology test: {ex.Message}");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTest(int id)
        {
            try
            {
                var test = await _radiologyService.GetRadiologyTestByIdAsync(id);
                if (test == null)
                    return NotFound();

                await _radiologyService.DeleteRadiologyTestAsync(id);
                await _auditService.LogActivityAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, "Delete", "RadiologyTest", id.ToString(), $"Test: {test.TestName}", null);
                TempData["SuccessMessage"] = $"Radiology test '{test.TestName}' deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting radiology test: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ======== Radiology Results Management ========

        public async Task<IActionResult> Results(int page = 1, int pageSize = 10, string status = "All")
        {
            IEnumerable<RadiologyResult> results;

            if (status == "All")
                results = await _radiologyService.GetAllRadiologyResultsAsync();
            else
                results = await _radiologyService.GetRadiologyResultsByStatusAsync(status);

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
            var tests = await _radiologyService.GetActiveRadiologyTestsAsync();

            ViewBag.Patients = patients;
            ViewBag.Tests = tests;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> OrderTest(RadiologyResult model)
        {
            if (!ModelState.IsValid)
            {
                var patients = await _patientService.GetAllPatientsAsync();
                var tests = await _radiologyService.GetActiveRadiologyTestsAsync();
                ViewBag.Patients = patients;
                ViewBag.Tests = tests;
                return View(model);
            }

            try
            {
                model.OrderDate = DateTime.UtcNow;
                var newResult = await _radiologyService.CreateRadiologyResultAsync(model);
                await _auditService.LogActivityAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, "Create", "RadiologyResult", newResult.Id.ToString(), null, $"OrderNumber: {newResult.OrderNumber}");
                TempData["SuccessMessage"] = $"Radiology test ordered successfully! Order #: {newResult.OrderNumber}";
                return RedirectToAction(nameof(Results));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error ordering radiology test: {ex.Message}");
                var patients = await _patientService.GetAllPatientsAsync();
                var tests = await _radiologyService.GetActiveRadiologyTestsAsync();
                ViewBag.Patients = patients;
                ViewBag.Tests = tests;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResultDetails(int id)
        {
            var result = await _radiologyService.GetRadiologyResultByIdAsync(id);
            if (result == null)
                return NotFound();

            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> EditResult(int id)
        {
            var result = await _radiologyService.GetRadiologyResultByIdAsync(id);
            if (result == null)
                return NotFound();

            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> EditResult(int id, RadiologyResult model)
        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var existingResult = await _radiologyService.GetRadiologyResultByIdAsync(id);
                var oldValues = $"Status: {existingResult.Status}, Impression: {existingResult.Impression}";

                var updatedResult = await _radiologyService.UpdateRadiologyResultAsync(model);
                var newValues = $"Status: {updatedResult.Status}, Impression: {updatedResult.Impression}";

                await _auditService.LogActivityAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, "Update", "RadiologyResult", id.ToString(), oldValues, newValues);
                TempData["SuccessMessage"] = "Radiology result updated successfully!";
                return RedirectToAction(nameof(ResultDetails), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating radiology result: {ex.Message}");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateResultStatus(int id, string status)
        {
            try
            {
                var success = await _radiologyService.UpdateRadiologyResultStatusAsync(id, status);
                if (success)
                {
                    await _auditService.LogActivityAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, "Update", "RadiologyResult", id.ToString(), null, $"Status updated to: {status}");
                    return Json(new { success = true, message = "Status updated successfully!" });
                }
                return Json(new { success = false, message = "Radiology result not found!" });
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
                var result = await _radiologyService.GetRadiologyResultByIdAsync(id);
                if (result == null)
                    return NotFound();

                await _radiologyService.DeleteRadiologyResultAsync(id);
                await _auditService.LogActivityAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, "Delete", "RadiologyResult", id.ToString(), $"OrderNumber: {result.OrderNumber}", null);
                TempData["SuccessMessage"] = "Radiology result deleted successfully!";
                return RedirectToAction(nameof(Results));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting radiology result: {ex.Message}";
                return RedirectToAction(nameof(Results));
            }
        }

        // ======== Image Management ========

        [HttpPost]
        public async Task<IActionResult> UploadImage(int radiologyResultId, IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return Json(new { success = false, message = "No image file selected!" });

            try
            {
                var result = await _radiologyService.GetRadiologyResultByIdAsync(radiologyResultId);
                if (result == null)
                    return Json(new { success = false, message = "Radiology result not found!" });

                var imagePath = await _fileService.UploadFileAsync(imageFile, "radiology-images");
                result.ImagePath = imagePath;
                await _radiologyService.UpdateRadiologyResultAsync(result);

                await _auditService.LogActivityAsync(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, "Update", "RadiologyResult", radiologyResultId.ToString(), null, $"Image uploaded: {imagePath}");

                return Json(new { success = true, message = "Image uploaded successfully!", imagePath });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error uploading image: {ex.Message}" });
            }
        }

        // ======== AJAX Methods ========

        [HttpGet]
        public async Task<JsonResult> GetPatientRadiologyResults(int patientId)
        {
            var results = await _radiologyService.GetRadiologyResultsByPatientAsync(patientId);
            return Json(results.Select(r => new
            {
                r.Id,
                r.OrderNumber,
                TestName = r.RadiologyTest?.TestName,
                r.OrderDate,
                r.Status,
                r.Impression
            }));
        }

        [HttpGet]
        public async Task<JsonResult> GetPendingCount()
        {
            var count = await _radiologyService.GetPendingRadiologyTestsCountAsync();
            return Json(new { count });
        }

        [HttpGet]
        public async Task<JsonResult> GetTestsByCategory(string category)
        {
            var tests = await _radiologyService.SearchRadiologyTestsByCategoryAsync(category);
            return Json(tests.Select(t => new 
            { 
                t.Id, 
                t.TestName, 
                t.Price,
                t.RequiresContrast,
                t.SpecialInstructions
            }));
        }
    }
}
