using MedyxHMS.DTOs;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedyxHMS.Controllers.PatientPortal
{
    [Authorize]
    public class MedicalRecordsController : Controller
    {
        private readonly IPatientPortalService _patientPortalService;

        public MedicalRecordsController(IPatientPortalService patientPortalService)
        {
            _patientPortalService = patientPortalService;
        }

        // GET: /PatientPortal/MedicalRecords/Index
        public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });
            }

            try
            {
                var records = await _patientPortalService.GetPatientMedicalRecordsAsync(userId, startDate, endDate);

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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "PatientPortal" });
            }

            var record = await _patientPortalService.GetMedicalRecordDetailsAsync(id);
            if (record == null)
            {
                return NotFound();
            }

            // Verify patient owns this record
            if (record.PatientId.ToString() != userId)
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
    }
}