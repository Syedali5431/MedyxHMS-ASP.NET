using MedyxHMS.DTOs;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// Purpose: Contains application code for AppointmentsController and its related runtime behavior.
namespace MedyxHMS.Controllers.PatientPortal
{
    [Area("PatientPortal")]
    [Authorize(Roles = "Patient")]
    [Route("PatientPortal/[controller]/[action]")]
    public class AppointmentsController : Controller
    {
        private readonly IPatientPortalService _patientPortalService;

        public AppointmentsController(IPatientPortalService patientPortalService)
        {
            _patientPortalService = patientPortalService;
        }

        // GET: /PatientPortal/Appointments/Index
        public async Task<IActionResult> Index(string filter = "all", int page = 1, int pageSize = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return LocalRedirect("/PatientPortal/Account/Login");
            }

            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue)
            {
                return LocalRedirect("/PatientPortal/Account/Login");
            }

            try
            {
                var appointments = await _patientPortalService.GetPatientAppointmentsAsync(patientId.Value.ToString(), filter);

                var viewModel = new PatientPortalAppointmentsViewModel
                {
                    Filter = filter,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalRecords = appointments.Count(),
                    Appointments = appointments
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(a => new PatientPortalAppointmentDto
                        {
                            Id = a.Id.ToString(),
                            AppointmentId = a.AppointmentId,
                            AppointmentDate = a.AppointmentDate,
                            DoctorName = a.Doctor != null ? $"{a.Doctor.FirstName} {a.Doctor.LastName}" : string.Empty,
                            Department = a.Doctor?.Specialization,
                            Status = a.Status,
                            Symptoms = a.Symptoms,
                            Notes = a.Notes,
                            CreatedDate = a.CreatedDate
                        })
                        .ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading appointments: {ex.Message}";
                return View(new PatientPortalAppointmentsViewModel());
            }
        }

        // GET: /PatientPortal/Appointments/Details/5
        public async Task<IActionResult> Details(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return LocalRedirect("/PatientPortal/Account/Login");
            }

            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue)
            {
                return LocalRedirect("/PatientPortal/Account/Login");
            }

            var appointment = await _patientPortalService.GetAppointmentDetailsAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            // Verify patient owns this appointment
            if (appointment.PatientId != patientId.Value)
            {
                return Forbid();
            }

            var viewModel = new PatientPortalAppointmentDetailsViewModel
            {
                Appointment = new PatientPortalAppointmentDto
                {
                    Id = appointment.Id.ToString(),
                    AppointmentId = appointment.AppointmentId,
                    AppointmentDate = appointment.AppointmentDate,
                    DoctorName = appointment.Doctor != null ? $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}" : string.Empty,
                    Department = appointment.Doctor?.Specialization,
                    Status = appointment.Status,
                    Symptoms = appointment.Symptoms,
                    Notes = appointment.Notes,
                    CreatedDate = appointment.CreatedDate
                },
                Doctor = new PatientPortalDoctorDto
                {
                    Id = appointment.Doctor?.Id.ToString(),
                    FirstName = appointment.Doctor?.FirstName,
                    LastName = appointment.Doctor?.LastName,
                    Department = appointment.Doctor?.Specialization,
                    Designation = "Doctor"
                }
            };

            return View(viewModel);
        }

        // GET: /PatientPortal/Appointments/Book
        public async Task<IActionResult> Book()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return LocalRedirect("/PatientPortal/Account/Login");
            }

            try
            {
                var doctors = await _patientPortalService.GetAvailableDoctorsForBookingAsync();

                var viewModel = new PatientPortalBookAppointmentViewModel
                {
                    AvailableDoctors = doctors.Select(d => new PatientPortalDoctorDto
                    {
                        Id = d.Id.ToString(),
                        FirstName = d.FirstName,
                        LastName = d.LastName,
                        Department = d.Department != null ? d.Department.Name : string.Empty,
                        Designation = "Doctor",
                        Specialization = d.Specialization
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading doctors: {ex.Message}";
                return View(new PatientPortalBookAppointmentViewModel());
            }
        }

        // POST: /PatientPortal/Appointments/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(PatientPortalBookAppointmentViewModel viewModel)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return LocalRedirect("/PatientPortal/Account/Login");
            }

            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue)
            {
                return LocalRedirect("/PatientPortal/Account/Login");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var appointmentTime = TimeSpan.TryParse(viewModel.SelectedTime, out var parsedTime)
                        ? parsedTime
                        : new TimeSpan(9, 0, 0);

                    var appointment = new Appointment
                    {
                        PatientId = patientId.Value,
                        DoctorId = viewModel.Appointment.DoctorId,
                        // AppointmentTime is the field the staff side reads for the time-of-day;
                        // the patient portal's own appointment list reads the time embedded in
                        // AppointmentDate instead, so set both consistently.
                        AppointmentDate = viewModel.SelectedDate.Date.Add(appointmentTime),
                        AppointmentTime = appointmentTime,
                        Symptoms = viewModel.Appointment.Symptoms,
                        Notes = viewModel.Appointment.Notes,
                        Priority = viewModel.Appointment.Priority ?? "Normal"
                    };

                    var result = await _patientPortalService.BookAppointmentAsync(appointment);
                    if (result != null)
                    {
                        TempData["SuccessMessage"] = "Appointment booked successfully!";
                        return RedirectToAction("Index");
                    }

                    TempData["ErrorMessage"] = "Failed to book appointment";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error booking appointment: {ex.Message}";
                }
            }

            return View(viewModel);
        }

        // GET: /PatientPortal/Appointments/Reschedule/5
        public async Task<IActionResult> Reschedule(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return LocalRedirect("/PatientPortal/Account/Login");
            }

            var patientId = await ResolveCurrentPatientIdAsync();
            if (!patientId.HasValue)
            {
                return LocalRedirect("/PatientPortal/Account/Login");
            }

            var appointment = await _patientPortalService.GetAppointmentDetailsAsync(id);
            if (appointment == null || !appointment.CanReschedule)
            {
                return NotFound();
            }

            if (appointment.PatientId != patientId.Value)
            {
                return Forbid();
            }

            var viewModel = new PatientPortalAppointmentDetailsViewModel
            {
                Appointment = new PatientPortalAppointmentDto
                {
                    Id = appointment.Id.ToString(),
                    AppointmentId = appointment.AppointmentId,
                    AppointmentDate = appointment.AppointmentDate,
                    DoctorName = appointment.Doctor != null ? $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}" : string.Empty,
                    Status = appointment.Status
                }
            };

            return View(viewModel);
        }

        // POST: /PatientPortal/Appointments/Reschedule/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(string id, PatientPortalAppointmentDetailsViewModel viewModel)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return LocalRedirect("/PatientPortal/Account/Login");
            }

            try
            {
                var result = await _patientPortalService.RescheduleAppointmentAsync(
                    id,
                    (viewModel.Appointment.AppointmentDate ?? DateTime.Now).Date,
                    (viewModel.Appointment.AppointmentDate ?? DateTime.Now).TimeOfDay);

                if (result)
                {
                    TempData["SuccessMessage"] = "Appointment rescheduled successfully!";
                    return RedirectToAction("Index");
                }

                TempData["ErrorMessage"] = "Failed to reschedule appointment";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error rescheduling appointment: {ex.Message}";
            }

            return RedirectToAction("Details", new { id });
        }

        // POST: /PatientPortal/Appointments/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(string id, string reason)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return LocalRedirect("/PatientPortal/Account/Login");
            }

            try
            {
                var result = await _patientPortalService.CancelAppointmentAsync(id, reason);
                if (result)
                {
                    TempData["SuccessMessage"] = "Appointment cancelled successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to cancel appointment";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error cancelling appointment: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // GET: /PatientPortal/Appointments/GetAvailableSlots
        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(string doctorId, DateTime date)
        {
            try
            {
                var timeSlots = await _patientPortalService.GetAvailableTimeSlotAsync(doctorId, date);
                return Json(new { success = true, timeSlots = timeSlots.Select(t => t.ToString(@"hh\:mm")) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<int?> ResolveCurrentPatientIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var patient = await _patientPortalService.GetPatientByIdAsync(userId);
            return patient?.Id;
        }
    }
}
