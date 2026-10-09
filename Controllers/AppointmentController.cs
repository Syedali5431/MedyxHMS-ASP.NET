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
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

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
            var allDtos = appointmentsList.Select(MapToDto).ToList();

            var todayAppointments = allDtos
                .Where(a => a.AppointmentDate.Date == DateTime.Today)
                .ToList();

            var upcomingAppointments = allDtos
                .Where(a => a.AppointmentDate.Date > DateTime.Today && a.AppointmentDate.Date <= DateTime.Today.AddDays(7))
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .Take(10)
                .ToList();

            bool IsNoShow(string status) =>
                (status ?? string.Empty).Replace("-", " ").Trim().Equals("No Show", StringComparison.OrdinalIgnoreCase);

            var doctors = await GetAvailableDoctorsAsync();
            var specializationByDoctorId = doctors.ToDictionary(d => d.Id, d => d.Specialization);

            var doctorPerformance = allDtos
                .GroupBy(a => new { a.DoctorId, a.DoctorName })
                .Select(g =>
                {
                    var total = g.Count();
                    var completed = g.Count(a => a.Status == "Completed");
                    var noShow = g.Count(a => IsNoShow(a.Status));
                    return new DoctorPerformanceItem
                    {
                        DoctorName = g.Key.DoctorName,
                        Specialization = specializationByDoctorId.TryGetValue(g.Key.DoctorId, out var spec) ? spec : string.Empty,
                        TotalAppointments = total,
                        CompletedAppointments = completed,
                        CompletionRate = total > 0 ? Math.Round((decimal)completed / total * 100, 1) : 0,
                        AverageWaitTime = 0,
                        NoShowRate = total > 0 ? Math.Round((decimal)noShow / total * 100, 1) : 0
                    };
                })
                .OrderByDescending(d => d.TotalAppointments)
                .Take(10)
                .ToList();

            var recentActivities = allDtos
                .OrderByDescending(a => a.UpdatedDate ?? a.CreatedDate)
                .Take(10)
                .Select(a => new RecentAppointmentActivityItem
                {
                    Timestamp = a.UpdatedDate ?? a.CreatedDate,
                    Action = a.UpdatedDate.HasValue ? "Updated" : "Created",
                    PatientName = a.PatientName,
                    DoctorName = a.DoctorName,
                    Details = $"{a.AppointmentType} — {a.Status}",
                    UserName = string.IsNullOrWhiteSpace(a.UpdatedBy) ? a.CreatedBy : a.UpdatedBy
                })
                .ToList();

            var orderedDays = new[]
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
                DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
            };
            var weeklyTrends = allDtos.GroupBy(a => a.AppointmentDate.DayOfWeek).ToDictionary(g => g.Key, g => g.Count());
            var peakHours = allDtos.GroupBy(a => a.AppointmentTime.Hours).ToDictionary(g => g.Key, g => g.Count());

            var viewModel = new AppointmentDashboardViewModel
            {
                TodayAppointments = todayAppointments.Count,
                UpcomingAppointments = upcomingAppointments.Count,
                CompletedToday = todayAppointments.Count(a => a.Status == "Completed"),
                CancelledToday = todayAppointments.Count(a => a.Status == "Cancelled"),
                TodayAppointmentsList = todayAppointments,
                UpcomingAppointmentsList = upcomingAppointments,
                AppointmentsByType = allDtos
                    .GroupBy(a => a.AppointmentType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AppointmentsByStatus = allDtos
                    .GroupBy(a => a.Status)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TotalAppointments = allDtos.Count,
                TotalCompleted = allDtos.Count(a => a.Status == "Completed"),
                TotalNoShow = allDtos.Count(a => IsNoShow(a.Status)),
                DoctorPerformance = doctorPerformance,
                RecentActivities = recentActivities,
                WeeklyTrendsLabels = orderedDays.Select(d => d.ToString().Substring(0, 3)).ToList(),
                WeeklyTrendsData = orderedDays.Select(d => weeklyTrends.TryGetValue(d, out var c) ? c : 0).ToList(),
                PeakHoursLabels = Enumerable.Range(0, 24).Select(h => $"{h:D2}:00").ToList(),
                PeakHoursData = Enumerable.Range(0, 24).Select(h => peakHours.TryGetValue(h, out var c) ? c : 0).ToList()
            };

            return View(viewModel);
        }

        // GET: /Appointment/ThermalSlip/5 – appointment slip with token number for receipt printers.
        [HttpGet]
        public async Task<IActionResult> ThermalSlip(int id, int? w, [FromServices] IReceiptPrintService receiptPrint)
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

            // Token = position in the doctor's (not cancelled) appointments of that day, by time.
            var day = appointment.AppointmentDate.Date;
            var sameDay = await _context.Appointments
                .Where(a => a.DoctorId == appointment.DoctorId && a.AppointmentDate >= day && a.AppointmentDate < day.AddDays(1) && a.Status != "Cancelled")
                .OrderBy(a => a.AppointmentTime).ThenBy(a => a.Id)
                .Select(a => a.Id)
                .ToListAsync();
            var token = sameDay.IndexOf(appointment.Id) + 1;

            var vm = await receiptPrint.CreateAsync("Appointment Slip", appointment.HospitalId, w, User.Identity?.Name);
            if (token > 0)
            {
                vm.HighlightLabel = "Token No.";
                vm.Highlight = token.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            vm.Lines.Add(new ReceiptLine("Appointment", $"#{appointment.Id}"));
            vm.Lines.Add(new ReceiptLine("Date", appointment.AppointmentDate.ToString("dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture)));
            vm.Lines.Add(new ReceiptLine("Time", DateTime.Today.Add(appointment.AppointmentTime).ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture)));
            vm.Lines.Add(new ReceiptLine("Doctor", appointment.Doctor != null ? "Dr. " + appointment.Doctor.Name : "-"));
            if (!string.IsNullOrWhiteSpace(appointment.Doctor?.Specialization)) vm.Lines.Add(new ReceiptLine("Dept.", appointment.Doctor!.Specialization));
            vm.Lines.Add(new ReceiptLine("Patient", appointment.Patient != null ? $"{appointment.Patient.FirstName} {appointment.Patient.LastName}".Trim() : "Unknown"));
            if (!string.IsNullOrWhiteSpace(appointment.Patient?.PatientId)) vm.Lines.Add(new ReceiptLine("Patient ID", appointment.Patient!.PatientId));
            if (!string.IsNullOrWhiteSpace(appointment.AppointmentType)) vm.Lines.Add(new ReceiptLine("Type", appointment.AppointmentType));
            vm.Lines.Add(new ReceiptLine("Status", string.IsNullOrWhiteSpace(appointment.Status) ? "-" : appointment.Status));
            vm.Note = "Please arrive 15 minutes before your appointment time and bring this slip.";
            vm.BackUrl = Url.Action(nameof(Details), new { id });
            return View("ThermalReceipt", vm);
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

            // The patient is picked with a search box that fills a hidden PatientId (0 when nothing is
            // picked), and unset int/date fields bind as 0 / 0001-01-01, so check them explicitly.
            if (model.Appointment.PatientId <= 0)
                ModelState.AddModelError("Appointment.PatientId", "Please select a patient.");
            if (model.Appointment.DoctorId <= 0)
                ModelState.AddModelError("Appointment.DoctorId", "Please select a doctor.");
            if (model.Appointment.AppointmentDate == default)
                ModelState.AddModelError("Appointment.AppointmentDate", "Please choose an appointment date.");
            // A posted id may refer to a patient or doctor that no longer exists.
            if (model.Appointment.PatientId > 0 && !await _context.Patients.AnyAsync(p => p.Id == model.Appointment.PatientId))
                ModelState.AddModelError("Appointment.PatientId", "Please select a patient.");
            if (model.Appointment.DoctorId > 0 && !await _context.Doctors.AnyAsync(d => d.Id == model.Appointment.DoctorId))
                ModelState.AddModelError("Appointment.DoctorId", "Please select a doctor.");

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
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "Create",
                    "Appointment",
                    createdAppointment.Id.ToString(),
                    null,
                    $"Created appointment for patient ID {createdAppointment.PatientId} with doctor ID {createdAppointment.DoctorId}"
                );

                TempData["SuccessMessage"] = $"Appointment scheduled successfully for {createdAppointment.AppointmentDate:MMM dd, yyyy} at {DateTime.Today.Add(createdAppointment.AppointmentTime):hh\\:mm tt}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Drop the entity whose save failed so the audit write below does not retry it.
                _context.ChangeTracker.Clear();
                ModelState.AddModelError("", "An error occurred while creating the appointment. Please try again.");
                await _auditService.LogActivityAsync(
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
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
                Id = appointment.Id,
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                Status = appointment.Status,
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
                AvailableDoctors = await GetAvailableDoctorsAsync(),
                Patient = MapPatientToDto(appointment.Patient)
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
                var reloaded = await _appointmentService.GetAppointmentByIdAsync(id);
                model.CurrentAppointment = MapToDto(reloaded);
                model.AvailableDoctors = await GetAvailableDoctorsAsync();
                model.Patient = reloaded?.Patient != null ? MapPatientToDto(reloaded.Patient) : null;
                return View(model);
            }

            try
            {
                var existingAppointment = await _appointmentService.GetAppointmentByIdAsync(id);
                if (existingAppointment == null)
                {
                    return NotFound();
                }

                // Check for conflicts if date/time changed (compare the day only: portal bookings also store the time in
                // AppointmentDate, while this form posts the date alone; the time is AppointmentTime).
                if (existingAppointment.DoctorId != model.Appointment.DoctorId ||
                    existingAppointment.AppointmentDate.Date != model.Appointment.AppointmentDate.Date ||
                    existingAppointment.AppointmentTime != model.Appointment.AppointmentTime)
                {
                    if (await HasAppointmentConflict(model.Appointment.DoctorId, model.Appointment.AppointmentDate, model.Appointment.AppointmentTime, id))
                    {
                        ModelState.AddModelError("", "This doctor already has an appointment at the selected time.");
                        model.CurrentAppointment = MapToDto(existingAppointment);
                        model.AvailableDoctors = await GetAvailableDoctorsAsync();
                        model.Patient = MapPatientToDto(existingAppointment.Patient);
                        return View(model);
                    }
                }

                var oldValues = $"Date: {existingAppointment.AppointmentDate:MMM dd, yyyy}, Time: {DateTime.Today.Add(existingAppointment.AppointmentTime):hh\\:mm tt}, Type: {existingAppointment.AppointmentType}";

                var updatedAppointment = new Appointment
                {
                    Id = id,
                    PatientId = existingAppointment.PatientId,
                    DoctorId = model.Appointment.DoctorId,
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

                var newValues = $"Date: {result.AppointmentDate:MMM dd, yyyy}, Time: {DateTime.Today.Add(result.AppointmentTime):hh\\:mm tt}, Type: {result.AppointmentType}";

                // Log the activity
                await _auditService.LogActivityAsync(
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
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
                // Drop the entity whose save failed so the audit write below does not retry it.
                _context.ChangeTracker.Clear();
                ModelState.AddModelError("", "An error occurred while updating the appointment. Please try again.");
                await _auditService.LogActivityAsync(
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "Update",
                    "Appointment",
                    id.ToString(),
                    null,
                    $"Failed to update appointment: {ex.Message}"
                );

                var reloaded = await _appointmentService.GetAppointmentByIdAsync(id);
                model.CurrentAppointment = MapToDto(reloaded);
                model.AvailableDoctors = await GetAvailableDoctorsAsync();
                model.Patient = reloaded?.Patient != null ? MapPatientToDto(reloaded.Patient) : null;
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

            return View(BuildUpdateStatusViewModel(appointment));
        }

        // POST: Appointment/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, AppointmentUpdateStatusViewModel model)
        {
            if (!await HasPermissionAsync("Appointment", "Edit"))
            {
                return Forbid();
            }

            // The form posts NewStatus/StatusNotes, which write through to StatusUpdate.
            if (string.IsNullOrWhiteSpace(model.StatusUpdate?.Status))
            {
                var current = await _appointmentService.GetAppointmentByIdAsync(id);
                if (current == null)
                {
                    return NotFound();
                }

                ModelState.AddModelError("NewStatus", "Please select a status.");
                return View(BuildUpdateStatusViewModel(current));
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
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
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
                // Drop the entity whose save failed so the audit write below does not retry it.
                _context.ChangeTracker.Clear();
                ModelState.AddModelError("", "An error occurred while updating the appointment status. Please try again.");
                await _auditService.LogActivityAsync(
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "Update",
                    "Appointment",
                    id.ToString(),
                    null,
                    $"Failed to update appointment status: {ex.Message}"
                );

                var reloaded = await _appointmentService.GetAppointmentByIdAsync(id);
                if (reloaded == null)
                {
                    return NotFound();
                }

                var retryModel = BuildUpdateStatusViewModel(reloaded);
                retryModel.StatusUpdate.Status = model.StatusUpdate?.Status;
                retryModel.StatusUpdate.Notes = model.StatusUpdate?.Notes;
                return View(retryModel);
            }
        }

        private AppointmentUpdateStatusViewModel BuildUpdateStatusViewModel(Appointment appointment)
        {
            return new AppointmentUpdateStatusViewModel
            {
                Appointment = MapToDto(appointment),
                StatusUpdate = new AppointmentStatusUpdateDto { Status = appointment.Status },
                Patient = appointment.Patient != null ? MapPatientToDto(appointment.Patient) : new PatientDto(),
                Doctor = appointment.Doctor != null
                    ? new DoctorDto
                    {
                        Id = appointment.Doctor.Id,
                        Name = appointment.Doctor.Name,
                        Specialization = appointment.Doctor.Specialization,
                        IsActive = appointment.Doctor.IsActive
                    }
                    : new DoctorDto()
            };
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
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
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
                // Drop the entity whose save failed so the audit write below does not retry it.
                _context.ChangeTracker.Clear();
                await _auditService.LogActivityAsync(
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
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
                HospitalId = appointment.HospitalId,
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
            return await _context.Doctors
                .Include(d => d.Department)
                .OrderBy(d => d.FirstName)
                .ThenBy(d => d.LastName)
                .Select(d => new DoctorDto
                {
                    Id = d.Id,
                    Name = $"Dr. {d.FirstName} {d.LastName}",
                    Specialization = d.Specialization,
                    Department = d.Department != null ? d.Department.Name : string.Empty,
                    IsActive = d.IsActive
                })
                .ToListAsync();
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
            // Checked across all hospitals of the group: a doctor cannot be booked at two branches at the same time.
            var day = date.Date;
            var nextDay = day.AddDays(1);
            return await _context.Appointments.IgnoreQueryFilters().AnyAsync(a =>
                a.DoctorId == doctorId &&
                a.AppointmentDate >= day && a.AppointmentDate < nextDay &&
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

            var permission = module.Equals("Appointment", StringComparison.OrdinalIgnoreCase)
                ? action switch
                {
                    "View" => "ViewAppointments",
                    "Add" => "AddAppointments",
                    "Edit" => "EditAppointments",
                    "Delete" => "DeleteAppointments",
                    _ => $"{module}.{action}"
                }
                : $"{module}.{action}";

            return await _authorizationService.HasPermissionAsync(userId, permission);
        }
    }
}
