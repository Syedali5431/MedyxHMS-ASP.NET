using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

// Purpose: Contains application code for LabController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Staff,Doctor,LabTechnician,Pathologist")]
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

        public async Task<IActionResult> Dashboard()
        {
            var results = (await _labService.GetAllLabResultsAsync()).ToList();
            var tests = (await _labService.GetAllLabTestsAsync()).ToList();

            var today = DateTime.Today;
            var todayResults = results.Where(r => r.OrderDate.Date == today).ToList();
            var pendingResults = results.Where(r => r.Status != "Completed" && r.Status != "Cancelled").ToList();
            var completedToday = results.Where(r => r.Status == "Completed" && r.ResultDate.HasValue && r.ResultDate.Value.Date == today).ToList();

            bool IsCritical(LabResult r) =>
                r.Status == "Completed" &&
                !string.IsNullOrWhiteSpace(r.Interpretation) &&
                !r.Interpretation.Equals("Normal", StringComparison.OrdinalIgnoreCase);

            var criticalResults = results.Where(IsCritical).ToList();

            var testsByCategory = tests
                .GroupBy(t => t.Category)
                .ToDictionary(g => g.Key, g => g.Count());

            var resultsByStatus = results
                .GroupBy(r => r.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            var criticalResultsList = criticalResults
                .OrderByDescending(r => r.ResultDate ?? r.OrderDate)
                .Take(10)
                .Select(r => new LabResultDto
                {
                    Id = r.Id,
                    LabTestId = r.LabTestId,
                    PatientName = r.Patient != null ? $"{r.Patient.FirstName} {r.Patient.LastName}" : "Unknown",
                    TestName = r.LabTest != null ? r.LabTest.TestName : "Unknown",
                    TestDate = r.OrderDate,
                    Parameter = r.LabTest != null ? r.LabTest.TestName : string.Empty,
                    Result = r.ResultValue,
                    Unit = r.Unit,
                    ReferenceRange = r.NormalRange,
                    Status = r.Status,
                    Interpretation = r.Interpretation,
                    ResultDate = r.ResultDate ?? r.OrderDate,
                    PerformedBy = r.PerformedBy,
                    Notes = r.Notes
                })
                .ToList();

            var viewModel = new LabDashboardViewModel
            {
                TodayTests = todayResults.Count,
                PendingTests = pendingResults.Count,
                CompletedToday = completedToday.Count,
                CriticalResults = criticalResults.Count,
                CriticalResultsList = criticalResultsList,
                TestsByCategory = testsByCategory,
                ResultsByStatus = resultsByStatus
            };

            return View(viewModel);
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
                model.OrderDate = DateTime.Now;
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

            ViewBag.Events = await _context.LabSampleEvents.AsNoTracking().Where(e => e.LabResultId == id).OrderBy(e => e.OccurredAt).ThenBy(e => e.Id).ToListAsync();
            ViewBag.SignatureValid = LabTraceabilityService.IsSignatureValid(result);
            ViewBag.CanSignOff = User.IsInRole(LabTraceabilityService.SignOffRole);
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> EditResult(int id)
        {
            var result = await _labService.GetLabResultByIdAsync(id);
            if (result == null)
                return NotFound();
            if (result.SignedOffAt.HasValue)
            {
                TempData["ErrorMessage"] = "This result has been signed off and is locked. A pathologist must revoke the sign-off (with a reason) before it can be changed.";
                return RedirectToAction(nameof(ResultDetails), new { id });
            }

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

            if (await _context.LabResults.AnyAsync(r => r.Id == id && r.SignedOffAt != null))
            {
                TempData["ErrorMessage"] = "This result has been signed off and is locked.";
                return RedirectToAction(nameof(ResultDetails), new { id });
            }

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
                return Json(new { success = false, message = "Lab result not found, or it is signed off and locked." });
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

                if (result.SignedOffAt.HasValue)
                {
                    TempData["ErrorMessage"] = "A signed-off result cannot be deleted.";
                    return RedirectToAction(nameof(ResultDetails), new { id });
                }

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

        // ======== Specimen tracking (ISO 15189 traceability) ========

        /// <summary>Worklist by stage: awaiting collection, collected, received (awaiting result), awaiting sign-off, signed off, rejected.</summary>
        [HttpGet]
        public async Task<IActionResult> Samples(string tab = "collect")
        {
            var query = _context.LabResults.AsNoTracking().Include(r => r.Patient).Include(r => r.LabTest)
                .Where(r => r.Status != "Cancelled");
            query = tab switch
            {
                "receive" => query.Where(r => r.SampleStatus == "Collected"),
                "result" => query.Where(r => r.SampleStatus == "Received" && (r.ResultValue == null || r.ResultValue == "") && r.SignedOffAt == null),
                "signoff" => query.Where(r => r.SampleStatus == "Received" && r.ResultValue != null && r.ResultValue != "" && r.SignedOffAt == null),
                "signed" => query.Where(r => r.SignedOffAt != null),
                "rejected" => query.Where(r => r.SampleStatus == "Rejected"),
                _ => query.Where(r => r.SampleStatus == "Awaiting collection")
            };

            ViewBag.Tab = tab;
            ViewBag.CanSignOff = User.IsInRole(LabTraceabilityService.SignOffRole);
            var list = await (tab == "signed" ? query.OrderByDescending(r => r.SignedOffAt) : query.OrderBy(r => r.OrderDate)).Take(300).ToListAsync();
            return View(list);
        }

        /// <summary>Barcode scanners type the code followed by Enter: find the order by accession or order number.</summary>
        [HttpGet]
        public async Task<IActionResult> Scan(string? code)
        {
            code = (code ?? string.Empty).Trim();
            var id = string.IsNullOrEmpty(code) ? null : await _context.LabResults
                .Where(r => r.AccessionNumber == code || r.OrderNumber == code)
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync();
            if (id == null)
            {
                TempData["ErrorMessage"] = string.IsNullOrEmpty(code) ? "Scan or type an accession number." : $"No sample found for \"{code}\".";
                return RedirectToAction(nameof(Samples));
            }

            return RedirectToAction(nameof(ResultDetails), new { id });
        }

        /// <summary>Specimen label (50 x 25 mm) with the accession barcode, for label printers. Read-only; printing is logged by LabelPrinted.</summary>
        [HttpGet]
        public async Task<IActionResult> PrintLabel(int id, int copies = 1)
        {
            var result = await _context.LabResults.AsNoTracking().Include(r => r.Patient).Include(r => r.LabTest).FirstOrDefaultAsync(r => r.Id == id);
            if (result == null)
                return NotFound();
            if (string.IsNullOrEmpty(result.AccessionNumber) || !Code128Barcode.CanEncode(result.AccessionNumber))
            {
                TempData["ErrorMessage"] = "This order has no accession number yet.";
                return RedirectToAction(nameof(ResultDetails), new { id });
            }

            ViewBag.Copies = Math.Clamp(copies, 1, 10);
            return View(result);
        }

        /// <summary>Called by the label page after the print dialog closes: records the label print in the chain of custody.</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LabelPrinted(int id, int copies, [FromServices] LabTraceabilityService traceability)
        {
            var result = await _context.LabResults.FirstOrDefaultAsync(r => r.Id == id);
            if (result == null) return NotFound();
            traceability.AddEvent(result, "Label printed", $"{Math.Clamp(copies, 1, 10)} label(s)");
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CollectSample(int id, string? sampleType, string? returnUrl, [FromServices] LabTraceabilityService traceability)
        {
            var result = await _context.LabResults.FirstOrDefaultAsync(r => r.Id == id);
            if (result == null) return NotFound();
            if (result.SampleStatus != "Awaiting collection" && result.SampleStatus != "Rejected")
            {
                TempData["ErrorMessage"] = $"The sample is already {result.SampleStatus.ToLowerInvariant()}.";
                return BackTo(returnUrl, id);
            }

            if (!string.IsNullOrWhiteSpace(sampleType) && !LabTraceabilityService.SampleTypes.Contains(sampleType))
            {
                TempData["ErrorMessage"] = "Choose a valid sample type.";
                return BackTo(returnUrl, id);
            }

            if (string.IsNullOrEmpty(result.AccessionNumber)) result.AccessionNumber = await traceability.NextAccessionNumberAsync();
            if (!string.IsNullOrWhiteSpace(sampleType)) result.SampleType = sampleType;
            var recollection = result.SampleStatus == "Rejected";
            result.SampleStatus = "Collected";
            result.CollectedAt = DateTime.Now;
            result.CollectedBy = traceability.CurrentUserName;
            result.RejectionReason = string.Empty;
            traceability.AddEvent(result, recollection ? "Re-collected" : "Collected", result.SampleType);
            await _context.SaveChangesAsync();
            await _auditService.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "COLLECT", "LabResult", id.ToString(), null, $"{result.AccessionNumber} {result.SampleType}");
            TempData["SuccessMessage"] = $"Sample {result.AccessionNumber} collected.";
            return BackTo(returnUrl, id);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveSample(int id, string? returnUrl, [FromServices] LabTraceabilityService traceability)
        {
            var result = await _context.LabResults.FirstOrDefaultAsync(r => r.Id == id);
            if (result == null) return NotFound();
            if (result.SampleStatus != "Collected")
            {
                TempData["ErrorMessage"] = "Only a collected sample can be received in the lab.";
                return BackTo(returnUrl, id);
            }

            result.SampleStatus = "Received";
            result.ReceivedAt = DateTime.Now;
            result.ReceivedBy = traceability.CurrentUserName;
            if (result.Status == "Ordered") result.Status = "In Progress";
            traceability.AddEvent(result, "Received in lab");
            await _context.SaveChangesAsync();
            await _auditService.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "RECEIVE", "LabResult", id.ToString(), null, result.AccessionNumber);
            TempData["SuccessMessage"] = $"Sample {result.AccessionNumber} received in the lab.";
            return BackTo(returnUrl, id);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectSample(int id, string? reason, string? returnUrl, [FromServices] LabTraceabilityService traceability)
        {
            var result = await _context.LabResults.FirstOrDefaultAsync(r => r.Id == id);
            if (result == null) return NotFound();
            if ((result.SampleStatus != "Collected" && result.SampleStatus != "Received") || result.SignedOffAt.HasValue || !string.IsNullOrWhiteSpace(result.ResultValue))
            {
                TempData["ErrorMessage"] = "Only a collected or received sample without a result can be rejected.";
                return BackTo(returnUrl, id);
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Give the reason for rejecting the sample (e.g. haemolysed, unlabelled, insufficient).";
                return BackTo(returnUrl, id);
            }

            result.SampleStatus = "Rejected";
            result.RejectionReason = reason.Trim();
            traceability.AddEvent(result, "Rejected", result.RejectionReason);
            await _context.SaveChangesAsync();
            await _auditService.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "REJECT", "LabResult", id.ToString(), null, $"{result.AccessionNumber}: {result.RejectionReason}");
            TempData["SuccessMessage"] = $"Sample {result.AccessionNumber} rejected – a new sample must be collected.";
            return BackTo(returnUrl, id);
        }

        /// <summary>Electronic sign-off by the logged-in pathologist, confirmed by re-entering the password.</summary>
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = LabTraceabilityService.SignOffRole)]
        public async Task<IActionResult> SignOff(int id, string? password, bool confirm, [FromServices] LabTraceabilityService traceability, [FromServices] UserManager<ApplicationUser> userManager)
        {
            var result = await _context.LabResults.FirstOrDefaultAsync(r => r.Id == id);
            if (result == null) return NotFound();

            string? error = null;
            if (result.SignedOffAt.HasValue) error = "This result is already signed off.";
            else if (string.IsNullOrWhiteSpace(result.ResultValue)) error = "Enter the result before signing it off.";
            else if (result.SampleStatus == "Rejected" || result.SampleStatus == "Awaiting collection" || result.SampleStatus == "Collected") error = "The sample must be received in the lab before the result can be signed off.";
            else if (!confirm) error = "Tick the confirmation that you have reviewed the result.";
            if (error != null)
            {
                TempData["ErrorMessage"] = error;
                return RedirectToAction(nameof(ResultDetails), new { id });
            }

            var signer = await userManager.GetUserAsync(User);
            if (signer == null || string.IsNullOrEmpty(password) || !await userManager.CheckPasswordAsync(signer, password))
            {
                if (signer != null) await userManager.AccessFailedAsync(signer);
                TempData["ErrorMessage"] = "Password incorrect – the result was not signed off.";
                return RedirectToAction(nameof(ResultDetails), new { id });
            }

            await userManager.ResetAccessFailedCountAsync(signer);
            var name = $"{signer.FirstName} {signer.LastName}".Trim();
            result.SignedOffAt = DateTime.Now;
            result.SignedOffByUserId = signer.Id;
            result.SignedOffBy = string.IsNullOrWhiteSpace(name) ? signer.UserName ?? signer.Id : name;
            result.VerifiedBy = result.SignedOffBy;
            result.Status = "Completed";
            result.ResultDate ??= DateTime.Now;
            result.SignatureHash = LabTraceabilityService.ComputeSignature(result, signer.Id, result.SignedOffAt.Value);
            traceability.AddEvent(result, "Signed off", $"Electronically signed by {result.SignedOffBy}");
            await _context.SaveChangesAsync();
            await _auditService.LogActivityAsync(signer.Id, "SIGN_OFF", "LabResult", id.ToString(), null, $"{result.AccessionNumber ?? result.OrderNumber}: {result.ResultValue} {result.Unit} signed by {result.SignedOffBy}");
            TempData["SuccessMessage"] = $"Result signed off by {result.SignedOffBy}. It is now locked.";
            return RedirectToAction(nameof(ResultDetails), new { id });
        }

        /// <summary>Withdraws a sign-off so that a result can be corrected; the reason is kept in the chain of custody.</summary>
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = LabTraceabilityService.SignOffRole)]
        public async Task<IActionResult> RevokeSignOff(int id, string? reason, string? password, [FromServices] LabTraceabilityService traceability, [FromServices] UserManager<ApplicationUser> userManager)
        {
            var result = await _context.LabResults.FirstOrDefaultAsync(r => r.Id == id);
            if (result == null) return NotFound();
            if (!result.SignedOffAt.HasValue || string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = !result.SignedOffAt.HasValue ? "This result is not signed off." : "Give the reason for revoking the sign-off.";
                return RedirectToAction(nameof(ResultDetails), new { id });
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null || string.IsNullOrEmpty(password) || !await userManager.CheckPasswordAsync(user, password))
            {
                if (user != null) await userManager.AccessFailedAsync(user);
                TempData["ErrorMessage"] = "Password incorrect – the sign-off was not revoked.";
                return RedirectToAction(nameof(ResultDetails), new { id });
            }

            var previous = $"signed by {result.SignedOffBy} at {result.SignedOffAt:yyyy-MM-dd HH:mm} UTC";
            result.SignedOffAt = null;
            result.SignedOffBy = string.Empty;
            result.SignedOffByUserId = string.Empty;
            result.SignatureHash = string.Empty;
            result.VerifiedBy = string.Empty;
            traceability.AddEvent(result, "Sign-off revoked", $"{reason.Trim()} (was {previous})");
            await _context.SaveChangesAsync();
            await _auditService.LogActivityAsync(user.Id, "REVOKE_SIGN_OFF", "LabResult", id.ToString(), previous, reason.Trim());
            TempData["SuccessMessage"] = "Sign-off revoked. The result can be corrected and signed off again.";
            return RedirectToAction(nameof(ResultDetails), new { id });
        }

        private IActionResult BackTo(string? returnUrl, int id) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(ResultDetails), new { id });

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
