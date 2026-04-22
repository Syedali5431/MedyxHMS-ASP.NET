using MedyxHMS.DTOs;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// Purpose: Contains application code for MedicalRecordsController and its related runtime behavior.
namespace MedyxHMS.Controllers.PatientPortal
{
    [Authorize(Roles = "Patient")]
    public class MedicalRecordsController : Controller
    {
        private readonly IPatientPortalService _patientPortalService;
        private readonly IPrescriptionService _prescriptionService;
        private readonly ILabService _labService;
        private readonly IRadiologyService _radiologyService;
        private readonly IExportService _exportService;

        public MedicalRecordsController(
            IPatientPortalService patientPortalService,
            IPrescriptionService prescriptionService,
            ILabService labService,
            IRadiologyService radiologyService,
            IExportService exportService)
        {
            _patientPortalService = patientPortalService;
            _prescriptionService = prescriptionService;
            _labService = labService;
            _radiologyService = radiologyService;
            _exportService = exportService;
        }

        // GET: /PatientPortal/MedicalRecords/Index
        public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 10)
        {
            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue)
            {
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });
            }

            try
            {
                var records = await _patientPortalService.GetPatientMedicalRecordsAsync(patientId.Value.ToString(), startDate, endDate);

                var viewModel = new PatientPortalMedicalRecordsViewModel
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalRecords = records.Count(),
                    MedicalRecords = records
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(r => new PatientPortalMedicalRecordDto
                        {
                            Id = r.Id.ToString(),
                            RecordDate = r.RecordDate,
                            DoctorName = $"{r.Staff?.FirstName} {r.Staff?.LastName}",
                            Department = r.Staff?.Department,
                            Diagnosis = r.Diagnosis,
                            Treatment = r.Treatment,
                            Prescription = r.Prescription?.ToString(),
                            Notes = r.Notes
                        })
                        .ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading medical records: {ex.Message}";
                return View(new PatientPortalMedicalRecordsViewModel());
            }
        }

        // GET: /PatientPortal/MedicalRecords/Details/5
        public async Task<IActionResult> Details(string id)
        {
            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue)
            {
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });
            }

            var record = await _patientPortalService.GetMedicalRecordDetailsAsync(id);
            if (record == null)
            {
                return NotFound();
            }

            // Verify patient owns this record
            if (record.PatientId != patientId.Value)
            {
                return Forbid();
            }

            var viewModel = new PatientPortalMedicalRecordDetailsViewModel
            {
                MedicalRecord = new PatientPortalMedicalRecordDto
                {
                    Id = record.Id.ToString(),
                    RecordDate = record.RecordDate,
                    DoctorName = $"{record.Staff?.FirstName} {record.Staff?.LastName}",
                    Department = record.Staff?.Department,
                    Diagnosis = record.Diagnosis,
                    Treatment = record.Treatment,
                    Prescription = record.Prescription?.ToString(),
                    Notes = record.Notes
                },
                Doctor = new PatientPortalDoctorDto
                {
                    Id = record.Staff?.Id.ToString(),
                    FirstName = record.Staff?.FirstName,
                    LastName = record.Staff?.LastName,
                    Department = record.Staff?.Department
                }
            };

            return View(viewModel);
        }

        // GET: /PatientPortal/MedicalRecords/Prescriptions
        public async Task<IActionResult> Prescriptions(int page = 1, int pageSize = 10)
        {
            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue)
            {
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });
            }

            try
            {
                var prescriptions = await _prescriptionService.GetPrescriptionsByPatientAsync(patientId.Value);

                var viewModel = new PatientPortalPrescriptionsViewModel
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalRecords = prescriptions.Count(),
                    Prescriptions = prescriptions
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(p => new PatientPortalPrescriptionDto
                        {
                            Id = p.Id,
                            MedicineName = p.Medicine?.Name ?? string.Empty,
                            Dosage = p.Dosage,
                            Frequency = p.Frequency,
                            Duration = p.Duration,
                            Quantity = p.Quantity,
                            UnitPrice = p.UnitPrice,
                            TotalPrice = p.TotalPrice,
                            Instructions = p.Instructions,
                            PrescribedDate = p.CreatedDate,
                            PharmacyBillNumber = p.PharmacyBill?.BillNumber ?? string.Empty,
                            BillStatus = p.PharmacyBill?.Status ?? string.Empty
                        })
                        .ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading prescriptions: {ex.Message}";
                return View(new PatientPortalPrescriptionsViewModel());
            }
        }

        // GET: /MedicalRecords/LabResults
        public async Task<IActionResult> LabResults(DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 10)
        {
            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue)
            {
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });
            }

            try
            {
                var results = await _labService.GetLabResultsByPatientAsync(patientId.Value);

                if (startDate.HasValue)
                {
                    results = results.Where(r => (r.ResultDate ?? r.OrderDate) >= startDate.Value).ToList();
                }

                if (endDate.HasValue)
                {
                    results = results.Where(r => (r.ResultDate ?? r.OrderDate) <= endDate.Value).ToList();
                }

                var viewModel = new PatientPortalTestResultsViewModel
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalRecords = results.Count(),
                    TestTypeFilter = "Pathology",
                    TestResults = results
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(r => new PatientPortalTestResultDto
                        {
                            Id = r.Id.ToString(),
                            TestName = r.LabTest?.TestName ?? string.Empty,
                            TestType = "Pathology",
                            TestDate = r.ResultDate ?? r.OrderDate,
                            Result = r.ResultValue,
                            Units = r.Unit,
                            ReferenceRange = r.NormalRange,
                            Status = r.Interpretation ?? r.Status,
                            Notes = r.Notes,
                            ReportPath = string.Empty
                        })
                        .ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading pathology results: {ex.Message}";
                return View(new PatientPortalTestResultsViewModel { TestTypeFilter = "Pathology" });
            }
        }

        // GET: /MedicalRecords/RadiologyResults
        public async Task<IActionResult> RadiologyResults(DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 10)
        {
            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue)
            {
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });
            }

            try
            {
                var results = await _radiologyService.GetRadiologyResultsByPatientAsync(patientId.Value);

                if (startDate.HasValue)
                {
                    results = results.Where(r => (r.ResultDate ?? r.OrderDate) >= startDate.Value).ToList();
                }

                if (endDate.HasValue)
                {
                    results = results.Where(r => (r.ResultDate ?? r.OrderDate) <= endDate.Value).ToList();
                }

                var viewModel = new PatientPortalTestResultsViewModel
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalRecords = results.Count(),
                    TestTypeFilter = "Radiology",
                    TestResults = results
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(r => new PatientPortalTestResultDto
                        {
                            Id = r.Id.ToString(),
                            TestName = r.RadiologyTest?.TestName ?? string.Empty,
                            TestType = "Radiology",
                            TestDate = r.ResultDate ?? r.OrderDate,
                            Result = r.Findings,
                            Units = string.Empty,
                            ReferenceRange = string.Empty,
                            Status = r.Status,
                            Notes = string.IsNullOrWhiteSpace(r.Impression) ? r.Notes : $"Impression: {r.Impression}" + (string.IsNullOrWhiteSpace(r.Notes) ? string.Empty : $" | Notes: {r.Notes}"),
                            ReportPath = r.ImagePath
                        })
                        .ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading radiology results: {ex.Message}";
                return View(new PatientPortalTestResultsViewModel { TestTypeFilter = "Radiology" });
            }
        }

        private async Task<int?> ResolveCurrentPatientIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var patient = await _patientPortalService.GetPatientByIdAsync(userId);
            if (patient != null)
            {
                return patient.Id;
            }

            return int.TryParse(userId, out var parsedPatientId) ? parsedPatientId : null;
        }

        [HttpGet]
        public async Task<IActionResult> DownloadReport(DateTime? startDate = null, DateTime? endDate = null)
        {
            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue)
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });

            var records = await _patientPortalService.GetPatientMedicalRecordsAsync(patientId.Value.ToString(), startDate, endDate);

            var headers = new[] { "Date", "Doctor", "Department", "Diagnosis", "Treatment" };
            var rows = records.Select(r => (IReadOnlyList<string>)new[]
            {
                r.RecordDate.ToString("yyyy-MM-dd"),
                (r.Staff?.FirstName + " " + r.Staff?.LastName).Trim(),
                r.Staff?.Department ?? string.Empty,
                r.Diagnosis ?? string.Empty,
                r.Treatment ?? string.Empty
            }).ToList();

            var bytes = _exportService.BuildPdfTable("Patient Medical Report", headers, rows);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            return File(bytes, "application/pdf", $"medical_report_{stamp}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPrescriptionsReport()
        {
            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue)
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });

            var prescriptions = await _prescriptionService.GetPrescriptionsByPatientAsync(patientId.Value);
            var headers = new[] { "Date", "Medicine", "Dosage", "Frequency", "Duration (days)", "Qty", "Amount" };
            var rows = prescriptions.Select(p => (IReadOnlyList<string>)new[]
            {
                p.CreatedDate.ToString("yyyy-MM-dd"),
                p.Medicine?.Name ?? string.Empty,
                p.Dosage ?? string.Empty,
                p.Frequency ?? string.Empty,
                p.Duration.ToString(),
                p.Quantity.ToString(),
                p.TotalPrice.ToString("0.00")
            }).ToList();

            var bytes = _exportService.BuildPdfTable("Patient Prescription Report", headers, rows);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            return File(bytes, "application/pdf", $"prescriptions_report_{stamp}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadLabResultsReport(DateTime? startDate = null, DateTime? endDate = null)
        {
            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue)
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });

            var results = await _labService.GetLabResultsByPatientAsync(patientId.Value);
            if (startDate.HasValue)
                results = results.Where(r => (r.ResultDate ?? r.OrderDate) >= startDate.Value).ToList();
            if (endDate.HasValue)
                results = results.Where(r => (r.ResultDate ?? r.OrderDate) <= endDate.Value).ToList();

            var headers = new[] { "Date", "Test", "Result", "Units", "Reference Range", "Status" };
            var rows = results.Select(r => (IReadOnlyList<string>)new[]
            {
                (r.ResultDate ?? r.OrderDate).ToString("yyyy-MM-dd"),
                r.LabTest?.TestName ?? string.Empty,
                r.ResultValue ?? string.Empty,
                r.Unit ?? string.Empty,
                r.NormalRange ?? string.Empty,
                r.Interpretation ?? r.Status ?? string.Empty
            }).ToList();

            var bytes = _exportService.BuildPdfTable("Patient Pathology Report", headers, rows);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            return File(bytes, "application/pdf", $"pathology_report_{stamp}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadRadiologyResultsReport(DateTime? startDate = null, DateTime? endDate = null)
        {
            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue)
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });

            var results = await _radiologyService.GetRadiologyResultsByPatientAsync(patientId.Value);
            if (startDate.HasValue)
                results = results.Where(r => (r.ResultDate ?? r.OrderDate) >= startDate.Value).ToList();
            if (endDate.HasValue)
                results = results.Where(r => (r.ResultDate ?? r.OrderDate) <= endDate.Value).ToList();

            var headers = new[] { "Date", "Test", "Findings", "Impression", "Status" };
            var rows = results.Select(r => (IReadOnlyList<string>)new[]
            {
                (r.ResultDate ?? r.OrderDate).ToString("yyyy-MM-dd"),
                r.RadiologyTest?.TestName ?? string.Empty,
                r.Findings ?? string.Empty,
                r.Impression ?? string.Empty,
                r.Status ?? string.Empty
            }).ToList();

            var bytes = _exportService.BuildPdfTable("Patient Radiology Report", headers, rows);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            return File(bytes, "application/pdf", $"radiology_report_{stamp}.pdf");
        }
    }
}
