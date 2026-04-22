using MedyxHMS.Data;
using MedyxHMS.DTOs;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using AuthService = MedyxHMS.Services.Interfaces.IAuthorizationService;
using AppointmentSummaryDto = MedyxHMS.ViewModels.AppointmentSummaryDto;
using PatientDto = MedyxHMS.ViewModels.PatientDto;

// Purpose: Contains application code for AppointmentController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IPatientService _patientService;
        private readonly AuthService _authorizationService;
        private readonly IAuditService _auditService;
        private readonly ApplicationDbContext _context;
        private readonly IExportService _exportService;

        public AppointmentController(
            IAppointmentService appointmentService,
            IPatientService patientService,
            AuthService authorizationService,
            IAuditService auditService,
            ApplicationDbContext context,
            IExportService exportService)
        {
            _appointmentService = appointmentService;
            _patientService = patientService;
            _authorizationService = authorizationService;
            _auditService = auditService;
            _context = context;
            _exportService = exportService;
        }

        // GET: Appointment
        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm, string statusFilter, DateTime? dateFilter, int? doctorFilter)
        {
            if (!await HasPermissionAsync("Appointment", "View"))
            {
                return Forbid();
            }

            var appointments = await _appointmentService.GetAllAppointmentsAsync();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                appointments = appointments.Where(a =>
                    a.Patient.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    a.Patient.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    a.Patient.PatientId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (a.Doctor != null && a.Doctor.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                appointments = appointments.Where(a => a.Status == statusFilter);
            }

            if (dateFilter.HasValue)
            {
                appointments = appointments.Where(a => a.AppointmentDate.Date == dateFilter.Value.Date);
            }

            if (doctorFilter.HasValue)
            {
                appointments = appointments.Where(a => a.DoctorId == doctorFilter.Value);
            }

            var appointmentDtos = appointments.Select(MapToDto).ToList();
            var availableDoctors = await GetAvailableDoctorsAsync();

            var viewModel = new AppointmentIndexViewModel
            {
                Appointments = appointmentDtos.OrderByDescending(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime),
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                DateFilter = dateFilter,
                DoctorFilter = doctorFilter,
                TotalAppointments = appointmentDtos.Count,
                TodayAppointments = appointmentDtos.Count(a => a.AppointmentDate.Date == DateTime.Today),
                UpcomingAppointments = appointmentDtos.Count(a => a.AppointmentDate.Date > DateTime.Today),
                CompletedAppointments = appointmentDtos.Count(a => a.Status == "Completed"),
                AvailableDoctors = availableDoctors
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Export(string format = "csv", string searchTerm = null, string statusFilter = null, DateTime? dateFilter = null, int? doctorFilter = null)
        {
            if (!await HasPermissionAsync("Appointment", "View"))
                return Forbid();

            format = (format ?? "csv").Trim().ToLowerInvariant();
            if (format != "csv" && format != "pdf")
                return BadRequest("Only CSV and PDF exports are supported.");

            var appointments = await _appointmentService.GetAllAppointmentsAsync();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                appointments = appointments.Where(a =>
                    a.Patient.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    a.Patient.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    a.Patient.PatientId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (a.Doctor != null && a.Doctor.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
                appointments = appointments.Where(a => a.Status == statusFilter);

            if (dateFilter.HasValue)
                appointments = appointments.Where(a => a.AppointmentDate.Date == dateFilter.Value.Date);

            if (doctorFilter.HasValue)
                appointments = appointments.Where(a => a.DoctorId == doctorFilter.Value);

            var headers = new[] { "Patient", "Patient ID", "Doctor", "Date", "Time", "Type", "Status", "Symptoms" };
            var rows = appointments
                .OrderByDescending(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .Select(a => (IReadOnlyList<string>)new[]
                {
                    (a.Patient?.FirstName + " " + a.Patient?.LastName).Trim(),
                    a.Patient?.PatientId ?? string.Empty,
                    a.Doctor?.Name ?? string.Empty,
                    a.AppointmentDate.ToString("yyyy-MM-dd"),
                    a.AppointmentTime.ToString(@"hh\:mm"),
                    a.AppointmentType ?? string.Empty,
                    a.Status ?? string.Empty,
                    a.Symptoms ?? string.Empty
                }).ToList();

            var title = "Appointment Management Export";
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            if (format == "csv")
            {
                var bytes = _exportService.BuildCsv(title, headers, rows);
                return File(bytes, "text/csv", $"appointments_{stamp}.csv");
            }

            var pdfBytes = _exportService.BuildPdfTable(title, headers, rows);
            return File(pdfBytes, "application/pdf", $"appointments_{stamp}.pdf");
        }

        // GET: Appointment/Calendar
        [HttpGet]
        public async Task<IActionResult> Calendar(DateTime? date, int? doctorId)
        {
            if (!await HasPermissionAsync("Appointment", "View"))
            {
                return Forbid();
            }

            var currentDate = date ?? DateTime.Today;
            var startOfWeek = currentDate.AddDays(-(int)currentDate.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(6);

            var appointments = await _appointmentService.GetAllAppointmentsAsync();
            var weekAppointments = appointments
                .Where(a => a.AppointmentDate >= startOfWeek && a.AppointmentDate <= endOfWeek)
                .ToList();

            if (doctorId.HasValue)
            {
                weekAppointments = weekAppointments.Where(a => a.DoctorId == doctorId.Value).ToList();
            }

            var appointmentDtos = weekAppointments.Select(MapToDto).ToList();
            var doctors = await GetAvailableDoctorsAsync();

            var viewModel = new AppointmentCalendarViewModel
            {
                CurrentDate = currentDate,
                StartOfWeek = startOfWeek,
                EndOfWeek = endOfWeek,
                WeekAppointments = appointmentDtos,
                Doctors = doctors,
                SelectedDoctorId = doctorId
            };

            return View(viewModel);
        }

        // GET: Appointment/Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!await HasPermissionAsync("Appointment", "View"))
            {
                return Forbid();
            }

            var allAppointments = await _appointmentService.GetAllAppointmentsAsync();
            var appointmentsList = allAppointments.ToList();

            var todayAppointments = appointmentsList
                .Where(a => a.AppointmentDate.Date == DateTime.Today)
                .Select(MapToDto)
                .ToList();

            var upcomingAppointments = appointmentsList
                .Where(a => a.AppointmentDate.Date > DateTime.Today && a.AppointmentDate.Date <= DateTime.Today.AddDays(7))
                .Select(MapToDto)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .Take(10)
                .ToList();

            var viewModel = new AppointmentDashboardViewModel
            {
                TodayAppointments = todayAppointments.Count,
                UpcomingAppointments = upcomingAppointments.Count,
                CompletedToday = todayAppointments.Count(a => a.Status == "Completed"),
                CancelledToday = todayAppointments.Count(a => a.Status == "Cancelled"),
                TodayAppointmentsList = todayAppointments,
                UpcomingAppointmentsList = upcomingAppointments,
                AppointmentsByType = appointmentsList
                    .GroupBy(a => a.AppointmentType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AppointmentsByStatus = appointmentsList
                    .GroupBy(a => a.Status)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return View(viewModel);
        }

        // GET: Appointment/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (!await HasPermissionAsync("Appointment", "View"))
            {
                return Forbid();
            }

            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            var appointmentDto = MapToDto(appointment);
            var patient = await _patientService.GetPatientByIdAsync(appointment.PatientId);
            var doctor = await GetDoctorByIdAsync(appointment.DoctorId);

            // Get related data
            var patientAppointments = await _appointmentService.GetAppointmentsByPatientAsync(appointment.PatientId);
            var doctorAppointments = await _appointmentService.GetAppointmentsByDoctorAsync(appointment.DoctorId);

            var patientRecentAppointments = patientAppointments
                .Where(a => a.Id != id)
                .OrderByDescending(a => a.AppointmentDate)
                .Take(5)
                .Select(MapToSummaryDto)
                .ToList();

            var doctorTodayAppointments = doctorAppointments
                .Where(a => a.AppointmentDate.Date == DateTime.Today && a.Id != id)
                .OrderBy(a => a.AppointmentTime)
                .Select(MapToSummaryDto)
                .ToList();

            var viewModel = new AppointmentDetailsViewModel
            {
                Appointment = appointmentDto,
                Patient = patient != null ? MapPatientToDto(patient) : null,
                Doctor = doctor,
                PatientRecentAppointments = patientRecentAppointments,
                DoctorTodayAppointments = doctorTodayAppointments
            };

            return View(viewModel);
        }

        // GET: Appointment/Create
        [HttpGet]
        public async Task<IActionResult> Create(int? patientId, string patientSearchTerm)
        {
            if (!await HasPermissionAsync("Appointment", "Add"))
            {
                return Forbid();
            }

            var viewModel = new AppointmentCreateViewModel
            {
                Appointment = new AppointmentCreateDto(),
                AvailableDoctors = await GetAvailableDoctorsAsync(),
                RecentPatients = await GetRecentPatientsAsync()
            };

            // Pre-select patient if provided
            if (patientId.HasValue)
            {
                viewModel.Appointment.PatientId = patientId.Value;
            }

            // Handle patient search
            if (!string.IsNullOrEmpty(patientSearchTerm))
            {
                viewModel.PatientSearchTerm = patientSearchTerm;
                viewModel.PatientSearchResults = (await _patientService.SearchPatientsAsync(patientSearchTerm))
                    .Select(MapPatientToDto)
                    .ToList();
            }

            return View(viewModel);
        }

        // POST: Appointment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppointmentCreateViewModel model)
        {
            if (!await HasPermissionAsync("Appointment", "Add"))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                model.AvailableDoctors = await GetAvailableDoctorsAsync();
                model.RecentPatients = await GetRecentPatientsAsync();
                return View(model);
            }

            try
            {
                // Validate appointment doesn't conflict
                if (await HasAppointmentConflict(model.Appointment.DoctorId, model.Appointment.AppointmentDate, model.Appointment.AppointmentTime))
                {
                    ModelState.AddModelError("", "This doctor already has an appointment at the selected time.");
                    model.AvailableDoctors = await GetAvailableDoctorsAsync();
                    model.RecentPatients = await GetRecentPatientsAsync();
                    return View(model);
                }

                var appointment = new Appointment
                {
                    PatientId = model.Appointment.PatientId,
                    DoctorId = model.Appointment.DoctorId,
                    AppointmentDate = model.Appointment.AppointmentDate,
                    AppointmentTime = model.Appointment.AppointmentTime,
                    Status = "Scheduled",
                    AppointmentType = model.Appointment.AppointmentType,
                    Symptoms = model.Appointment.Symptoms,
                    Notes = model.Appointment.Notes,
                    CreatedBy = User.Identity.Name
                };

                var createdAppointment = await _appointmentService.CreateAppointmentAsync(appointment);

                // Log the activity
                await _auditService.LogActivityAsync(
                    User.Identity.Name,
                    "Create",
                    "Appointment",
                    createdAppointment.Id.ToString(),
                    null,
                    $"Created appointment for patient ID {createdAppointment.PatientId} with doctor ID {createdAppointment.DoctorId}"
                );

                TempData["SuccessMessage"] = $"Appointment scheduled successfully for {createdAppointment.AppointmentDate:MMM dd, yyyy} at {createdAppointment.AppointmentTime:hh\\:mm tt}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while creating the appointment. Please try again.");
                await _auditService.LogActivityAsync(
                    User.Identity.Name,
                    "Create",
                    "Appointment",
                    "Failed",
                    null,
                    $"Failed to create appointment: {ex.Message}"
                );

                model.AvailableDoctors = await GetAvailableDoctorsAsync();
                model.RecentPatients = await GetRecentPatientsAsync();
                return View(model);
            }
        }

        // GET: Appointment/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!await HasPermissionAsync("Appointment", "Edit"))
            {
                return Forbid();
            }

            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            var appointmentDto = MapToDto(appointment);

            var updateDto = new AppointmentUpdateDto
            {
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime,
                AppointmentType = appointment.AppointmentType,
                Symptoms = appointment.Symptoms,
                Notes = appointment.Notes
            };

            var viewModel = new AppointmentEditViewModel
            {
                CurrentAppointment = appointmentDto,
                Appointment = updateDto,
                AvailableDoctors = await GetAvailableDoctorsAsync()
            };

            return View(viewModel);
        }

        // POST: Appointment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AppointmentEditViewModel model)
        {
            if (!await HasPermissionAsync("Appointment", "Edit"))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                model.CurrentAppointment = MapToDto(await _appointmentService.GetAppointmentByIdAsync(id));
                model.AvailableDoctors = await GetAvailableDoctorsAsync();
                return View(model);
            }

            try
            {
                var existingAppointment = await _appointmentService.GetAppointmentByIdAsync(id);
                if (existingAppointment == null)
                {
                    return NotFound();
                }

                // Check for conflicts if date/time changed
                if (existingAppointment.DoctorId != model.CurrentAppointment.DoctorId ||
                    existingAppointment.AppointmentDate != model.Appointment.AppointmentDate ||
                    existingAppointment.AppointmentTime != model.Appointment.AppointmentTime)
                {
                    if (await HasAppointmentConflict(model.CurrentAppointment.DoctorId, model.Appointment.AppointmentDate, model.Appointment.AppointmentTime, id))
                    {
                        ModelState.AddModelError("", "This doctor already has an appointment at the selected time.");
                        model.CurrentAppointment = MapToDto(existingAppointment);
                        model.AvailableDoctors = await GetAvailableDoctorsAsync();
                        return View(model);
                    }
                }

                var oldValues = $"Date: {existingAppointment.AppointmentDate:MMM dd, yyyy}, Time: {existingAppointment.AppointmentTime:hh\\:mm tt}, Type: {existingAppointment.AppointmentType}";

                var updatedAppointment = new Appointment
                {
                    Id = id,
                    PatientId = existingAppointment.PatientId,
                    DoctorId = model.CurrentAppointment.DoctorId,
                    AppointmentDate = model.Appointment.AppointmentDate,
                    AppointmentTime = model.Appointment.AppointmentTime,
                    Status = existingAppointment.Status,
                    AppointmentType = model.Appointment.AppointmentType,
                    Symptoms = model.Appointment.Symptoms,
                    Notes = model.Appointment.Notes
                };

                var result = await _appointmentService.UpdateAppointmentAsync(updatedAppointment);
                if (result == null)
                {
                    return NotFound();
                }

                var newValues = $"Date: {result.AppointmentDate:MMM dd, yyyy}, Time: {result.AppointmentTime:hh\\:mm tt}, Type: {result.AppointmentType}";

                // Log the activity
                await _auditService.LogActivityAsync(
                    User.Identity.Name,
                    "Update",
                    "Appointment",
                    id.ToString(),
                    oldValues,
                    newValues
                );

                TempData["SuccessMessage"] = $"Appointment updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating the appointment. Please try again.");
                await _auditService.LogActivityAsync(
                    User.Identity.Name,
                    "Update",
                    "Appointment",
                    id.ToString(),
                    null,
                    $"Failed to update appointment: {ex.Message}"
                );

                model.CurrentAppointment = MapToDto(await _appointmentService.GetAppointmentByIdAsync(id));
                model.AvailableDoctors = await GetAvailableDoctorsAsync();
                return View(model);
            }
        }

        // GET: Appointment/UpdateStatus/5
        [HttpGet]
        public async Task<IActionResult> UpdateStatus(int id)
        {
            if (!await HasPermissionAsync("Appointment", "Edit"))
            {
                return Forbid();
            }

            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            var appointmentDto = MapToDto(appointment);
            var statusViewModel = new AppointmentStatusUpdateViewModel
            {
                Appointment = appointmentDto,
                StatusUpdate = new AppointmentStatusUpdateDto { Status = appointment.Status }
            };

            return View(statusViewModel);
        }

        // POST: Appointment/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, AppointmentStatusUpdateViewModel model)
        {
            if (!await HasPermissionAsync("Appointment", "Edit"))
            {
                return Forbid();
            }

            try
            {
                var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
                if (appointment == null)
                {
                    return NotFound();
                }

                var oldStatus = appointment.Status;
                appointment.Status = model.StatusUpdate.Status;
                appointment.Notes = string.IsNullOrEmpty(model.StatusUpdate.Notes) ?
                    appointment.Notes : $"{appointment.Notes}\n\nStatus Update: {model.StatusUpdate.Notes}";

                var result = await _appointmentService.UpdateAppointmentAsync(appointment);
                if (result == null)
                {
                    return NotFound();
                }

                // Log the activity
                await _auditService.LogActivityAsync(
                    User.Identity.Name,
                    "Update",
                    "Appointment",
                    id.ToString(),
                    $"Status: {oldStatus}",
                    $"Status: {result.Status}"
                );

                TempData["SuccessMessage"] = $"Appointment status updated to {result.Status}.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating the appointment status. Please try again.");
                await _auditService.LogActivityAsync(
                    User.Identity.Name,
                    "Update",
                    "Appointment",
                    id.ToString(),
                    null,
                    $"Failed to update appointment status: {ex.Message}"
                );

                model.Appointment = MapToDto(await _appointmentService.GetAppointmentByIdAsync(id));
                return View(model);
            }
        }

        // POST: Appointment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await HasPermissionAsync("Appointment", "Delete"))
            {
                return Forbid();
            }

            try
            {
                var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
                if (appointment == null)
                {
                    return NotFound();
                }

                var appointmentInfo = $"Patient: {appointment.Patient?.FirstName} {appointment.Patient?.LastName}, Date: {appointment.AppointmentDate:MMM dd, yyyy}";

                var result = await _appointmentService.DeleteAppointmentAsync(id);
                if (!result)
                {
                    return NotFound();
                }

                // Log the activity
                await _auditService.LogActivityAsync(
                    User.Identity.Name,
                    "Delete",
                    "Appointment",
                    id.ToString(),
                    appointmentInfo,
                    "Appointment deleted"
                );

                TempData["SuccessMessage"] = "Appointment deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await _auditService.LogActivityAsync(
                    User.Identity.Name,
                    "Delete",
                    "Appointment",
                    id.ToString(),
                    null,
                    $"Failed to delete appointment: {ex.Message}"
                );

                TempData["ErrorMessage"] = "An error occurred while deleting the appointment. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper Methods
        private AppointmentDto MapToDto(Appointment appointment)
        {
            return new AppointmentDto
            {
                Id = appointment.Id,
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                PatientName = appointment.Patient != null ? $"{appointment.Patient.FirstName} {appointment.Patient.LastName}" : "Unknown",
                DoctorName = appointment.Doctor != null ? appointment.Doctor.Name : "Unknown",
                PatientIdDisplay = appointment.Patient?.PatientId ?? "Unknown",
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime,
                Status = appointment.Status,
                AppointmentType = appointment.AppointmentType,
                Symptoms = appointment.Symptoms,
                Notes = appointment.Notes,
                CreatedDate = appointment.CreatedDate,
                CreatedBy = appointment.CreatedBy
            };
        }

        private AppointmentSummaryDto MapToSummaryDto(Appointment appointment)
        {
            return new AppointmentSummaryDto
            {
                Id = appointment.Id,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime,
                Status = appointment.Status,
                AppointmentType = appointment.AppointmentType,
                PatientName = appointment.Patient != null ? $"{appointment.Patient.FirstName} {appointment.Patient.LastName}" : "Unknown",
                DoctorName = appointment.Doctor != null ? appointment.Doctor.Name : "Unknown"
            };
        }

        private PatientDto MapPatientToDto(Patient patient)
        {
            return new PatientDto
            {
                Id = patient.Id,
                PatientId = patient.PatientId,
                FullName = $"{patient.FirstName} {patient.LastName}",
                Phone = patient.Phone,
                Gender = patient.Gender,
                DateOfBirth = patient.DateOfBirth,
                Age = (int)((DateTime.Now - patient.DateOfBirth).TotalDays / 365.25)
            };
        }

        private async Task<IEnumerable<DoctorDto>> GetAvailableDoctorsAsync()
        {
            // For now, return mock doctors. In a real system, this would come from a Doctor service
            return new List<DoctorDto>
            {
                new DoctorDto { Id = 1, Name = "Dr. Sarah Johnson", Specialization = "General Medicine", Department = "Internal Medicine", IsActive = true },
                new DoctorDto { Id = 2, Name = "Dr. Michael Chen", Specialization = "Cardiology", Department = "Cardiology", IsActive = true },
                new DoctorDto { Id = 3, Name = "Dr. Emily Davis", Specialization = "Pediatrics", Department = "Pediatrics", IsActive = true },
                new DoctorDto { Id = 4, Name = "Dr. Robert Wilson", Specialization = "Orthopedics", Department = "Orthopedics", IsActive = true },
                new DoctorDto { Id = 5, Name = "Dr. Lisa Brown", Specialization = "Dermatology", Department = "Dermatology", IsActive = true }
            };
        }

        private async Task<DoctorDto> GetDoctorByIdAsync(int doctorId)
        {
            var doctors = await GetAvailableDoctorsAsync();
            return doctors.FirstOrDefault(d => d.Id == doctorId);
        }

        private async Task<IEnumerable<PatientDto>> GetRecentPatientsAsync()
        {
            var patients = await _patientService.GetAllPatientsAsync();
            return patients.Take(10).Select(MapPatientToDto);
        }

        private async Task<bool> HasAppointmentConflict(int doctorId, DateTime date, TimeSpan time, int? excludeAppointmentId = null)
        {
            var appointments = await _appointmentService.GetAppointmentsByDoctorAsync(doctorId);
            return appointments.Any(a =>
                a.AppointmentDate.Date == date.Date &&
                a.AppointmentTime == time &&
                a.Status != "Cancelled" &&
                (!excludeAppointmentId.HasValue || a.Id != excludeAppointmentId.Value));
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
    }
}
