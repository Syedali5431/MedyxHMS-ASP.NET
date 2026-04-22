using MedyxHMS.Data;
using MedyxHMS.DTOs;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using AuthService = MedyxHMS.Services.Interfaces.IAuthorizationService;
using PatientDto = MedyxHMS.DTOs.PatientDto;
using AppointmentSummaryDto = MedyxHMS.ViewModels.AppointmentSummaryDto;

// Purpose: Contains application code for PatientController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize]
    public class PatientController : Controller
    {
        private readonly IPatientService _patientService;
        private readonly AuthService _authorizationService;
        private readonly IAuditService _auditService;
        private readonly IExportService _exportService;
        private readonly ApplicationDbContext _context;

        public PatientController(
            IPatientService patientService,
            AuthService authorizationService,
            IAuditService auditService,
            IExportService exportService,
            ApplicationDbContext context)
        {
            _patientService = patientService;
            _authorizationService = authorizationService;
            _auditService = auditService;
            _exportService = exportService;
            _context = context;
        }

        // GET: Patient
        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm)
        {
            // Check permissions
            if (!await HasPermissionAsync("Patient", "View"))
            {
                return Forbid();
            }

            var patients = string.IsNullOrWhiteSpace(searchTerm)
                ? await _patientService.GetAllPatientsAsync()
                : await _patientService.SearchPatientsAsync(searchTerm);

            var patientDtos = patients.Select(MapToDto).ToList();

            var viewModel = new PatientIndexViewModel
            {
                Patients = patientDtos,
                SearchTerm = searchTerm,
                TotalPatients = patientDtos.Count,
                ActivePatients = patientDtos.Count(p => p.IsActive),
                InactivePatients = patientDtos.Count(p => !p.IsActive)
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Export(string format = "csv", string searchTerm = null)
        {
            if (!await HasPermissionAsync("Patient", "View"))
                return Forbid();

            format = (format ?? "csv").Trim().ToLowerInvariant();
            if (format != "csv" && format != "pdf")
                return BadRequest("Only CSV and PDF exports are supported.");

            var patients = string.IsNullOrWhiteSpace(searchTerm)
                ? await _patientService.GetAllPatientsAsync()
                : await _patientService.SearchPatientsAsync(searchTerm);

            var headers = new[] { "Patient ID", "Name", "Email", "Phone", "Gender", "Blood Group", "Status", "Created" };
            var rows = patients.Select(p => (IReadOnlyList<string>)new[]
            {
                p.PatientId ?? string.Empty,
                (p.FirstName + " " + p.LastName).Trim(),
                p.Email ?? string.Empty,
                p.Phone ?? string.Empty,
                p.Gender ?? string.Empty,
                p.BloodGroup ?? string.Empty,
                p.IsActive ? "Active" : "Inactive",
                p.CreatedDate.ToString("yyyy-MM-dd")
            }).ToList();

            var title = "Patient Management Export";
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            if (format == "csv")
            {
                var bytes = _exportService.BuildCsv(title, headers, rows);
                return File(bytes, "text/csv", $"patients_{stamp}.csv");
            }

            var pdfBytes = _exportService.BuildPdfTable(title, headers, rows);
            return File(pdfBytes, "application/pdf", $"patients_{stamp}.pdf");
        }

        // GET: Patient/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (!await HasPermissionAsync("Patient", "View"))
            {
                return Forbid();
            }

            var patient = await _patientService.GetPatientByIdAsync(id);
            if (patient == null)
            {
                return NotFound();
            }

            var patientDto = MapToDto(patient);

            // For now, we'll create empty collections for related data
            // These will be populated when we implement the related services
            var viewModel = new PatientDetailsViewModel
            {
                Patient = patientDto,
                RecentAppointments = new List<AppointmentSummaryDto>(),
                RecentBills = new List<BillSummaryDto>(),
                TotalAppointments = 0,
                TotalBills = 0,
                TotalAmountPaid = 0
            };

            return View(viewModel);
        }

        // GET: Patient/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!await HasPermissionAsync("Patient", "Add"))
            {
                return Forbid();
            }

            var viewModel = new PatientCreateViewModel
            {
                Patient = new PatientCreateDto()
            };

            return View(viewModel);
        }

        // POST: Patient/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PatientCreateViewModel model)
        {
            if (!await HasPermissionAsync("Patient", "Add"))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var patient = new Patient
                {
                    FirstName = model.Patient.FirstName,
                    LastName = model.Patient.LastName,
                    Email = model.Patient.Email,
                    Phone = model.Patient.Phone,
                    DateOfBirth = model.Patient.DateOfBirth,
                    Gender = model.Patient.Gender,
                    Address = model.Patient.Address,
                    City = model.Patient.City,
                    State = model.Patient.State,
                    Country = model.Patient.Country,
                    PostalCode = model.Patient.PostalCode,
                    BloodGroup = model.Patient.BloodGroup,
                    EmergencyContactName = model.Patient.EmergencyContactName,
                    EmergencyContactPhone = model.Patient.EmergencyContactPhone,
                    MedicalHistory = model.Patient.MedicalHistory,
                    Allergies = model.Patient.Allergies,
                    IsActive = true
                };

                var createdPatient = await _patientService.CreatePatientAsync(patient);

                // Log the activity
                await _auditService.LogActivityAsync(
                    User.Identity.Name,
                    "Create",
                    "Patient",
                    createdPatient.Id.ToString(),
                    null,
                    $"Created patient: {createdPatient.FirstName} {createdPatient.LastName} ({createdPatient.PatientId})"
                );

                TempData["SuccessMessage"] = $"Patient {createdPatient.FirstName} {createdPatient.LastName} has been created successfully with ID: {createdPatient.PatientId}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while creating the patient. Please try again.");
                await _auditService.LogActivityAsync(
                    User.Identity.Name,
                    "Create",
                    "Patient",
                    "Failed",
                    null,
                    $"Failed to create patient: {ex.Message}"
                );
                return View(model);
            }
        }

        // GET: Patient/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!await HasPermissionAsync("Patient", "Edit"))
            {
                return Forbid();
            }

            var patient = await _patientService.GetPatientByIdAsync(id);
            if (patient == null)
            {
                return NotFound();
            }

            var patientDto = MapToDto(patient);

            var updateDto = new PatientUpdateDto
            {
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                Phone = patient.Phone,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                Address = patient.Address,
                City = patient.City,
                State = patient.State,
                Country = patient.Country,
                PostalCode = patient.PostalCode,
                BloodGroup = patient.BloodGroup,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactPhone = patient.EmergencyContactPhone,
                MedicalHistory = patient.MedicalHistory,
                Allergies = patient.Allergies,
                IsActive = patient.IsActive
            };

            var viewModel = new PatientEditViewModel
            {
                CurrentPatient = patientDto,
                Patient = updateDto
            };

            return View(viewModel);
        }

        // POST: Patient/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PatientEditViewModel model)
        {
            if (!await HasPermissionAsync("Patient", "Edit"))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                // Re-populate the current patient data
                var patient = await _patientService.GetPatientByIdAsync(id);
                if (patient != null)
                {
                    model.CurrentPatient = MapToDto(patient);
                }
                return View(model);
            }

            try
            {
                var existingPatient = await _patientService.GetPatientByIdAsync(id);
                if (existingPatient == null)
                {
                    return NotFound();
                }

                var oldValues = $"Name: {existingPatient.FirstName} {existingPatient.LastName}, Email: {existingPatient.Email}";

                var updatedPatient = new Patient
                {
                    Id = id,
                    FirstName = model.Patient.FirstName,
                    LastName = model.Patient.LastName,
                    Email = model.Patient.Email,
                    Phone = model.Patient.Phone,
                    DateOfBirth = model.Patient.DateOfBirth,
                    Gender = model.Patient.Gender,
                    Address = model.Patient.Address,
                    City = model.Patient.City,
                    State = model.Patient.State,
                    Country = model.Patient.Country,
                    PostalCode = model.Patient.PostalCode,
                    BloodGroup = model.Patient.BloodGroup,
                    EmergencyContactName = model.Patient.EmergencyContactName,
                    EmergencyContactPhone = model.Patient.EmergencyContactPhone,
                    MedicalHistory = model.Patient.MedicalHistory,
                    Allergies = model.Patient.Allergies,
                    IsActive = model.Patient.IsActive
                };

                var result = await _patientService.UpdatePatientAsync(updatedPatient);
                if (result == null)
                {
                    return NotFound();
                }

                var newValues = $"Name: {result.FirstName} {result.LastName}, Email: {result.Email}";

                // Log the activity
                await _auditService.LogActivityAsync(
                    User.Identity?.Name,
                    "Update",
                    "Patient",
                    id.ToString(),
                    oldValues,
                    newValues
                );

                TempData["SuccessMessage"] = $"Patient {result.FirstName} {result.LastName} has been updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating the patient. Please try again.");
                await _auditService.LogActivityAsync(
                    User.Identity?.Name,
                    "Update",
                    "Patient",
                    id.ToString(),
                    null,
                    $"Failed to update patient: {ex.Message}"
                );

                // Re-populate the current patient data
                var patient = await _patientService.GetPatientByIdAsync(id);
                if (patient != null)
                {
                    model.CurrentPatient = MapToDto(patient);
                }
                return View(model);
            }
        }

        // GET: Patient/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await HasPermissionAsync("Patient", "Delete"))
            {
                return Forbid();
            }

            var patient = await _patientService.GetPatientByIdAsync(id);
            if (patient == null)
            {
                return NotFound();
            }

            var patientDto = MapToDto(patient);

            // Check for related data that might prevent deletion
            var hasActiveAppointments = patient.Appointments?.Any(a =>
                a.Status == "Scheduled" || a.Status == "Confirmed") ?? false;
            var hasUnpaidBills = patient.Bills?.Any(b => b.Status != "Paid") ?? false;

            var warningMessage = "";
            if (hasActiveAppointments && hasUnpaidBills)
            {
                warningMessage = "This patient has active appointments and unpaid bills. Deleting this patient will affect billing and appointment records.";
            }
            else if (hasActiveAppointments)
            {
                warningMessage = "This patient has active appointments. Deleting this patient will cancel all future appointments.";
            }
            else if (hasUnpaidBills)
            {
                warningMessage = "This patient has unpaid bills. Deleting this patient will affect billing records.";
            }

            var viewModel = new PatientDeleteViewModel
            {
                Patient = patientDto,
                HasActiveAppointments = hasActiveAppointments,
                HasUnpaidBills = hasUnpaidBills,
                WarningMessage = warningMessage
            };

            return View(viewModel);
        }

        // POST: Patient/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!await HasPermissionAsync("Patient", "Delete"))
            {
                return Forbid();
            }

            try
            {
                var patient = await _patientService.GetPatientByIdAsync(id);
                if (patient == null)
                {
                    return NotFound();
                }

                var patientName = $"{patient.FirstName} {patient.LastName}";

                var result = await _patientService.DeletePatientAsync(id);
                if (!result)
                {
                    return NotFound();
                }

                // Log the activity
                await _auditService.LogActivityAsync(
                    User.Identity?.Name,
                    "Delete",
                    "Patient",
                    id.ToString(),
                    $"Name: {patientName}, ID: {patient.PatientId}",
                    "Patient marked as inactive (soft delete)"
                );

                TempData["SuccessMessage"] = $"Patient {patientName} has been deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await _auditService.LogActivityAsync(
                    User.Identity?.Name,
                    "Delete",
                    "Patient",
                    id.ToString(),
                    null,
                    $"Failed to delete patient: {ex.Message}"
                );

                TempData["ErrorMessage"] = "An error occurred while deleting the patient. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper method to map Patient entity to PatientDto
        private PatientDto MapToDto(Patient patient)
        {
            return new PatientDto
            {
                Id = patient.Id,
                PatientId = patient.PatientId,
                FullName = $"{patient.FirstName} {patient.LastName}",
                Email = patient.Email,
                Phone = patient.Phone,
                DateOfBirth = patient.DateOfBirth,
                Age = CalculateAge(patient.DateOfBirth),
                Gender = patient.Gender,
                BloodGroup = patient.BloodGroup,
                Address = patient.Address,
                City = patient.City,
                State = patient.State,
                Country = patient.Country,
                PostalCode = patient.PostalCode,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactPhone = patient.EmergencyContactPhone,
                MedicalHistory = patient.MedicalHistory,
                Allergies = patient.Allergies,
                IsActive = patient.IsActive,
                CreatedDate = patient.CreatedDate,
                LastVisitDate = patient.LastVisitDate
            };
        }

        // Helper method to calculate age from date of birth
        private int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }

        private async Task<bool> HasPermissionAsync(string module, string action)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            return await _authorizationService.HasPermissionAsync(userId, $"{module}.{action}");
        }

        // ======== Insurance Management ========

        private static bool CanManageInsurance(System.Security.Principal.IPrincipal user) =>
            user.IsInRole("Patient") || user.IsInRole("Receptionist") ||
            user.IsInRole("Admin") || user.IsInRole("SuperAdmin");

        [HttpGet]
        [Authorize(Roles = "Patient,Receptionist,Admin,SuperAdmin")]
        public async Task<IActionResult> InsuranceList(int patientId)
        {
            var patient = await _patientService.GetPatientByIdAsync(patientId);
            if (patient == null) return NotFound();

            var insurances = await _context.PatientInsurances
                .Where(i => i.PatientId == patientId)
                .OrderByDescending(i => i.IsActive)
                .ThenByDescending(i => i.ValidTo)
                .ToListAsync();

            ViewBag.PatientName = $"{patient.FirstName} {patient.LastName}";
            ViewBag.PatientId = patientId;
            return View(insurances);
        }

        [HttpGet]
        [Authorize(Roles = "Patient,Receptionist,Admin,SuperAdmin")]
        public async Task<IActionResult> InsuranceCreate(int patientId)
        {
            var patient = await _patientService.GetPatientByIdAsync(patientId);
            if (patient == null) return NotFound();

            var ins = new PatientInsurance { PatientId = patientId, IsActive = true };
            ViewBag.PatientName = $"{patient.FirstName} {patient.LastName}";
            return View(ins);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient,Receptionist,Admin,SuperAdmin")]
        public async Task<IActionResult> InsuranceCreate(PatientInsurance model)
        {
            ModelState.Remove("Patient");
            if (!ModelState.IsValid)
                return View(model);

            _context.PatientInsurances.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Insurance details added.";
            return RedirectToAction(nameof(InsuranceList), new { patientId = model.PatientId });
        }

        [HttpGet]
        [Authorize(Roles = "Patient,Receptionist,Admin,SuperAdmin")]
        public async Task<IActionResult> InsuranceEdit(int id)
        {
            var ins = await _context.PatientInsurances.FindAsync(id);
            if (ins == null) return NotFound();

            var patient = await _patientService.GetPatientByIdAsync(ins.PatientId);
            ViewBag.PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "";
            return View(ins);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient,Receptionist,Admin,SuperAdmin")]
        public async Task<IActionResult> InsuranceEdit(PatientInsurance model)
        {
            ModelState.Remove("Patient");
            if (!ModelState.IsValid)
                return View(model);

            _context.PatientInsurances.Update(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Insurance details updated.";
            return RedirectToAction(nameof(InsuranceList), new { patientId = model.PatientId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Receptionist,Admin,SuperAdmin")]
        public async Task<IActionResult> InsuranceDelete(int id)
        {
            var ins = await _context.PatientInsurances.FindAsync(id);
            if (ins == null) return NotFound();

            var patientId = ins.PatientId;
            _context.PatientInsurances.Remove(ins);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Insurance record deleted.";
            return RedirectToAction(nameof(InsuranceList), new { patientId });
        }
    }
}
