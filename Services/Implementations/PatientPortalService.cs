using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MedyxHMS.Services.Implementations
{
    public class PatientPortalService : IPatientPortalService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly IAuthorizationService _authorizationService;

        public PatientPortalService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService,
            IAuthorizationService authorizationService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
            _authorizationService = authorizationService;
        }

        // Patient Account Management
        public async Task<Patient> RegisterPatientAsync(Patient patient, string password)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Create application user
                    var user = new ApplicationUser
                    {
                        Email = patient.User?.Email,
                        UserName = patient.User?.Email,
                        PhoneNumber = patient.Phone,
                        FirstLoginDate = DateTime.UtcNow
                    };

                    var result = await _userManager.CreateAsync(user, password);
                    if (!result.Succeeded)
                    {
                        throw new Exception($"User creation failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }

                    patient.UserId = user.Id;
                    patient.User = user;
                    patient.CreatedDate = DateTime.UtcNow;
                    patient.IsActive = true;

                    // Assign Patient role
                    var patientRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Patient");
                    if (patientRole != null)
                    {
                        await _userManager.AddToRoleAsync(user, patientRole.Name);
                    }

                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync();

                    await _auditService.LogActivityAsync(user.Id, "PATIENT_REGISTRATION", "Patient", patient.Id.ToString());

                    await transaction.CommitAsync();
                    return patient;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Patient registration failed: {ex.Message}", ex);
                }
            }
        }

        public async Task<Patient> GetPatientByIdAsync(string patientId)
        {
            return await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id.ToString() == patientId || p.PatientId == patientId);
        }

        public async Task<Patient> UpdatePatientProfileAsync(Patient patient)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var existingPatient = await _context.Patients
                        .Include(p => p.User)
                        .FirstOrDefaultAsync(p => p.Id == patient.Id);

                    if (existingPatient == null)
                        throw new Exception("Patient not found");

                    existingPatient.FirstName = patient.FirstName;
                    existingPatient.LastName = patient.LastName;
                    existingPatient.Phone = patient.Phone;
                    existingPatient.DateOfBirth = patient.DateOfBirth;
                    existingPatient.Gender = patient.Gender;
                    existingPatient.BloodGroup = patient.BloodGroup;
                    existingPatient.Address = patient.Address;
                    existingPatient.GuardianName = patient.GuardianName;
                    existingPatient.GuardianPhone = patient.GuardianPhone;
                    existingPatient.MaritalStatus = patient.MaritalStatus;
                    existingPatient.Occupation = patient.Occupation;
                    existingPatient.EmergencyContactName = patient.EmergencyContactName;
                    existingPatient.EmergencyContactPhone = patient.EmergencyContactPhone;
                    existingPatient.EmergencyContactRelation = patient.EmergencyContactRelation;

                    _context.Patients.Update(existingPatient);
                    await _context.SaveChangesAsync();

                    await _auditService.LogActivityAsync(existingPatient.UserId, "PROFILE_UPDATE", "Patient", existingPatient.Id.ToString());

                    await transaction.CommitAsync();
                    return existingPatient;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Profile update failed: {ex.Message}", ex);
                }
            }
        }

        public async Task<bool> ChangePatientPasswordAsync(string patientId, string currentPassword, string newPassword)
        {
            var patient = await GetPatientByIdAsync(patientId);
            if (patient == null)
                return false;

            var result = await _userManager.ChangePasswordAsync(patient.User, currentPassword, newPassword);
            if (result.Succeeded)
            {
                await _auditService.LogActivityAsync(patient.UserId, "PASSWORD_CHANGE", "Patient", patientId);
                return true;
            }

            return false;
        }

        public async Task<bool> ResetPatientPasswordAsync(string email, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return false;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                await _auditService.LogActivityAsync(user.Id, "PASSWORD_RESET", "Patient", user.Id);
                return true;
            }

            return false;
        }

        // Patient Dashboard
        public async Task<Dictionary<string, object>> GetPatientDashboardDataAsync(string patientId)
        {
            var upcomingCount = await GetUpcomingAppointmentsCountAsync(patientId);
            var pendingBills = await GetPendingBillsCountAsync(patientId);
            var outstandingAmount = await GetTotalOutstandingAmountAsync(patientId);

            return new Dictionary<string, object>
            {
                { "UpcomingAppointments", upcomingCount },
                { "PendingBills", pendingBills },
                { "OutstandingAmount", outstandingAmount },
                { "DashboardUpdated", DateTime.UtcNow }
            };
        }

        public async Task<int> GetUpcomingAppointmentsCountAsync(string patientId)
        {
            return await _context.Appointments
                .Where(a => a.PatientId.ToString() == patientId && a.AppointmentDate > DateTime.UtcNow && a.Status != "Cancelled")
                .CountAsync();
        }

        public async Task<int> GetPendingBillsCountAsync(string patientId)
        {
            return await _context.Bills
                .Where(b => b.PatientId.ToString() == patientId && (b.Status == "Pending" || b.Status == "Overdue"))
                .CountAsync();
        }

        public async Task<decimal> GetTotalOutstandingAmountAsync(string patientId)
        {
            return await _context.Bills
                .Where(b => b.PatientId.ToString() == patientId && (b.Status == "Pending" || b.Status == "Overdue"))
                .SumAsync(b => b.TotalAmount - b.PaidAmount);
        }

        // Patient Appointments
        public async Task<IEnumerable<Appointment>> GetPatientAppointmentsAsync(string patientId, string filter = "all")
        {
            var query = _context.Appointments
                .Include(a => a.Staff)
                .Where(a => a.PatientId.ToString() == patientId);

            query = filter switch
            {
                "upcoming" => query.Where(a => a.AppointmentDate > DateTime.UtcNow && a.Status != "Cancelled"),
                "past" => query.Where(a => a.AppointmentDate <= DateTime.UtcNow && a.Status != "Cancelled"),
                "cancelled" => query.Where(a => a.Status == "Cancelled"),
                _ => query
            };

            return await query.OrderByDescending(a => a.AppointmentDate).ToListAsync();
        }

        public async Task<Appointment> GetAppointmentDetailsAsync(string appointmentId)
        {
            return await _context.Appointments
                .Include(a => a.Staff)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id.ToString() == appointmentId);
        }

        public async Task<Appointment> BookAppointmentAsync(Appointment appointment)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    appointment.Status = "Pending";
                    appointment.CreatedDate = DateTime.UtcNow;

                    _context.Appointments.Add(appointment);
                    await _context.SaveChangesAsync();

                    await _auditService.LogActivityAsync(appointment.PatientId.ToString(), "APPOINTMENT_BOOKED", "Appointment", appointment.Id.ToString());

                    await transaction.CommitAsync();
                    return appointment;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Appointment booking failed: {ex.Message}", ex);
                }
            }
        }

        public async Task<bool> RescheduleAppointmentAsync(string appointmentId, DateTime newDate, TimeSpan newTime)
        {
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id.ToString() == appointmentId);
            if (appointment == null || (appointment.Status != "Pending" && appointment.Status != "Confirmed"))
                return false;

            appointment.AppointmentDate = newDate.Add(newTime);
            appointment.Status = "Rescheduled";

            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            await _auditService.LogActivityAsync(appointment.PatientId.ToString(), "APPOINTMENT_RESCHEDULED", "Appointment", appointmentId);

            return true;
        }

        public async Task<bool> CancelAppointmentAsync(string appointmentId, string cancelReason)
        {
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id.ToString() == appointmentId);
            if (appointment == null || (appointment.Status != "Pending" && appointment.Status != "Confirmed"))
                return false;

            appointment.Status = "Cancelled";
            appointment.Notes = $"Cancelled by patient. Reason: {cancelReason}";

            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            await _auditService.LogActivityAsync(appointment.PatientId.ToString(), "APPOINTMENT_CANCELLED", "Appointment", appointmentId);

            return true;
        }

        public async Task<IEnumerable<Staff>> GetAvailableDoctorsAsync(DateTime date)
        {
            return await _context.Staff
                .Where(s => s.IsActive && s.StaffRoles.Any(sr => sr.Role.Name == "Doctor"))
                .Include(s => s.User)
                .ToListAsync();
        }

        public async Task<List<TimeSpan>> GetAvailableTimeSlotAsync(string doctorId, DateTime date)
        {
            // Generate time slots (e.g., 30-minute intervals from 9 AM to 5 PM)
            var timeSlots = new List<TimeSpan>();
            var startTime = new TimeSpan(9, 0, 0);
            var endTime = new TimeSpan(17, 0, 0);

            for (var time = startTime; time < endTime; time = time.Add(TimeSpan.FromMinutes(30)))
            {
                // Check if slot is already booked
                var isBooked = await _context.Appointments
                    .AnyAsync(a => a.StaffId.ToString() == doctorId && 
                                   a.AppointmentDate.Date == date.Date &&
                                   a.AppointmentDate.TimeOfDay == time &&
                                   a.Status != "Cancelled");

                if (!isBooked)
                {
                    timeSlots.Add(time);
                }
            }

            return timeSlots;
        }

        // Patient Medical Records
        public async Task<IEnumerable<MedicalRecord>> GetPatientMedicalRecordsAsync(string patientId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.MedicalRecords
                .Where(m => m.PatientId.ToString() == patientId);

            if (startDate.HasValue)
                query = query.Where(m => m.RecordDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(m => m.RecordDate <= endDate.Value);

            return await query
                .Include(m => m.Staff)
                .OrderByDescending(m => m.RecordDate)
                .ToListAsync();
        }

        public async Task<MedicalRecord> GetMedicalRecordDetailsAsync(string recordId)
        {
            return await _context.MedicalRecords
                .Include(m => m.Staff)
                .Include(m => m.Patient)
                .FirstOrDefaultAsync(m => m.Id.ToString() == recordId);
        }

        public async Task<IEnumerable<TestResult>> GetPatientTestResultsAsync(string patientId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.TestResults
                .Where(t => t.PatientId.ToString() == patientId);

            if (startDate.HasValue)
                query = query.Where(t => t.TestDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.TestDate <= endDate.Value);

            return await query.OrderByDescending(t => t.TestDate).ToListAsync();
        }

        public async Task<TestResult> GetTestResultDetailsAsync(string testResultId)
        {
            return await _context.TestResults
                .FirstOrDefaultAsync(t => t.Id.ToString() == testResultId);
        }

        public async Task<byte[]> DownloadTestReportAsync(string testResultId)
        {
            // Implementation would depend on how reports are stored
            // This is a placeholder for future implementation
            throw new NotImplementedException("Test report download functionality not yet implemented");
        }

        // Patient Bills and Payments
        public async Task<IEnumerable<Bill>> GetPatientBillsAsync(string patientId, string filter = "all")
        {
            var query = _context.Bills
                .Where(b => b.PatientId.ToString() == patientId);

            query = filter switch
            {
                "pending" => query.Where(b => b.Status == "Pending"),
                "paid" => query.Where(b => b.Status == "Paid"),
                "overdue" => query.Where(b => b.Status == "Overdue"),
                _ => query
            };

            return await query.OrderByDescending(b => b.BillDate).ToListAsync();
        }

        public async Task<Bill> GetBillDetailsAsync(string billId)
        {
            return await _context.Bills
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id.ToString() == billId);
        }

        public async Task<IEnumerable<Payment>> GetPaymentHistoryAsync(string patientId)
        {
            return await _context.Payments
                .Where(p => p.Bill.PatientId.ToString() == patientId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalOutstandingAsync(string patientId)
        {
            return await _context.Bills
                .Where(b => b.PatientId.ToString() == patientId && (b.Status == "Pending" || b.Status == "Overdue"))
                .SumAsync(b => b.TotalAmount - b.PaidAmount);
        }

        public async Task<int> GetOverdueBillsCountAsync(string patientId)
        {
            return await _context.Bills
                .Where(b => b.PatientId.ToString() == patientId && b.Status == "Overdue")
                .CountAsync();
        }

        // Doctor Information
        public async Task<IEnumerable<Staff>> GetAvailableDoctorsForBookingAsync(string departmentFilter = null)
        {
            IQueryable<Staff> query = _context.Staff
                .Where(s => s.IsActive && s.StaffRoles.Any(sr => sr.Role.Name == "Doctor"))
                .Include(s => s.User);

            if (!string.IsNullOrEmpty(departmentFilter))
            {
                query = query.Where(s => s.Department == departmentFilter);
            }

            return await query.OrderBy(s => s.FirstName).ToListAsync();
        }

        public async Task<Staff> GetDoctorDetailsAsync(string doctorId)
        {
            return await _context.Staff
                .Include(s => s.User)
                .Include(s => s.StaffRoles)
                .ThenInclude(sr => sr.Role)
                .FirstOrDefaultAsync(s => s.Id.ToString() == doctorId);
        }

        public async Task<List<DoctorAvailability>> GetDoctorAvailabilityAsync(string doctorId)
        {
            // This would typically come from a schedule/availability table
            // For now, returning default availability
            var availability = new List<DoctorAvailability>
            {
                new DoctorAvailability { DayOfWeek = "Monday", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), IsAvailable = true },
                new DoctorAvailability { DayOfWeek = "Tuesday", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), IsAvailable = true },
                new DoctorAvailability { DayOfWeek = "Wednesday", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), IsAvailable = true },
                new DoctorAvailability { DayOfWeek = "Thursday", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), IsAvailable = true },
                new DoctorAvailability { DayOfWeek = "Friday", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), IsAvailable = true },
                new DoctorAvailability { DayOfWeek = "Saturday", StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(14, 0, 0), IsAvailable = true },
                new DoctorAvailability { DayOfWeek = "Sunday", StartTime = TimeSpan.Zero, EndTime = TimeSpan.Zero, IsAvailable = false }
            };

            return await Task.FromResult(availability);
        }

        // Patient Notifications
        public async Task<IEnumerable<Notification>> GetPatientNotificationsAsync(string patientId)
        {
            // This would be implemented when notifications table is added to the database
            throw new NotImplementedException("Notifications feature not yet implemented");
        }

        public async Task<bool> MarkNotificationAsReadAsync(string notificationId)
        {
            throw new NotImplementedException("Notifications feature not yet implemented");
        }

        public async Task<bool> DeleteNotificationAsync(string notificationId)
        {
            throw new NotImplementedException("Notifications feature not yet implemented");
        }
    }
}