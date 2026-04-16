using MedyxHMS.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.ViewModels
{
    public class AppointmentIndexViewModel
    {
        public IEnumerable<AppointmentDto> Appointments { get; set; }
        public string SearchTerm { get; set; }
        public string StatusFilter { get; set; }
        public DateTime? DateFilter { get; set; }
        public int? DoctorFilter { get; set; }
        public int TotalAppointments { get; set; }
        public int TodayAppointments { get; set; }
        public int UpcomingAppointments { get; set; }
        public int CompletedAppointments { get; set; }

        // Filter options
        public List<string> StatusOptions => new List<string>
        {
            "All", "Scheduled", "Confirmed", "Completed", "Cancelled", "No-Show"
        };

        public IEnumerable<DoctorDto> AvailableDoctors { get; set; }
    }

    public class AppointmentCreateViewModel
    {
        public AppointmentCreateDto Appointment { get; set; }

        // Dropdown options
        public List<string> AppointmentTypeOptions => new List<string>
        {
            "OPD Consultation", "Follow-up", "Emergency", "Specialist Consultation",
            "Diagnostic", "Surgery Consultation", "Telemedicine"
        };

        public IEnumerable<DoctorDto> AvailableDoctors { get; set; }
        public IEnumerable<PatientDto> RecentPatients { get; set; }

        // For patient search
        public string PatientSearchTerm { get; set; }
        public IEnumerable<PatientDto> PatientSearchResults { get; set; }
    }

    public class AppointmentEditViewModel
    {
        public AppointmentDto CurrentAppointment { get; set; }
        public AppointmentUpdateDto Appointment { get; set; }

        // Dropdown options
        public List<string> AppointmentTypeOptions => new List<string>
        {
            "OPD Consultation", "Follow-up", "Emergency", "Specialist Consultation",
            "Diagnostic", "Surgery Consultation", "Telemedicine"
        };

        public IEnumerable<DoctorDto> AvailableDoctors { get; set; }
        public PatientDto Patient { get; set; }
        public DoctorDto Doctor { get; set; }
        public bool HasConflict { get; set; }
    }

    public class AppointmentDetailsViewModel
    {
        public AppointmentDto Appointment { get; set; }
        public PatientDto Patient { get; set; }
        public DoctorDto Doctor { get; set; }
        public IEnumerable<AppointmentSummaryDto> PatientRecentAppointments { get; set; }
        public IEnumerable<AppointmentSummaryDto> DoctorTodayAppointments { get; set; }
    }

    public class AppointmentStatusUpdateViewModel
    {
        public AppointmentDto Appointment { get; set; }
        public AppointmentStatusUpdateDto StatusUpdate { get; set; }

        public List<string> AvailableStatuses => new List<string>
        {
            "Scheduled", "Confirmed", "Completed", "Cancelled", "No-Show"
        };

        // Status transition rules
        public List<string> GetValidStatuses(string currentStatus)
        {
            return currentStatus?.ToLower() switch
            {
                "scheduled" => new List<string> { "Confirmed", "Cancelled" },
                "confirmed" => new List<string> { "Completed", "Cancelled", "No-Show" },
                "completed" => new List<string> { "Completed" }, // Final state
                "cancelled" => new List<string> { "Cancelled" }, // Final state
                "no-show" => new List<string> { "No-Show" }, // Final state
                _ => AvailableStatuses
            };
        }
    }

    public class AppointmentCalendarViewModel
    {
        public DateTime CurrentDate { get; set; }
        public DateTime StartOfWeek { get; set; }
        public DateTime EndOfWeek { get; set; }
        public IEnumerable<AppointmentDto> WeekAppointments { get; set; }
        public IEnumerable<DoctorDto> Doctors { get; set; }
        public int? SelectedDoctorId { get; set; }
        public string ViewType { get; set; } = "week";
        public IEnumerable<DoctorDto> AvailableDoctors => Doctors;
        public int? DoctorFilter => SelectedDoctorId;
        public string StatusFilter { get; set; }
        public List<string> StatusOptions => new() { "All", "Scheduled", "Confirmed", "Completed", "Cancelled", "No-Show" };
        public object CalendarEvents => WeekAppointments;

        // Calendar helpers
        public List<DateTime> WeekDays => GetWeekDays();
        public Dictionary<DateTime, List<AppointmentDto>> AppointmentsByDate => GetAppointmentsByDate();

        private List<DateTime> GetWeekDays()
        {
            var days = new List<DateTime>();
            for (int i = 0; i < 7; i++)
            {
                days.Add(StartOfWeek.AddDays(i));
            }
            return days;
        }

        private Dictionary<DateTime, List<AppointmentDto>> GetAppointmentsByDate()
        {
            var dict = new Dictionary<DateTime, List<AppointmentDto>>();
            foreach (var appointment in WeekAppointments ?? new List<AppointmentDto>())
            {
                var date = appointment.AppointmentDate.Date;
                if (!dict.ContainsKey(date))
                {
                    dict[date] = new List<AppointmentDto>();
                }
                dict[date].Add(appointment);
            }
            return dict;
        }
    }

    public class AppointmentDashboardViewModel
    {
        public int TodayAppointments { get; set; }
        public int UpcomingAppointments { get; set; }
        public int CompletedToday { get; set; }
        public int CancelledToday { get; set; }
        public IEnumerable<AppointmentDto> TodayAppointmentsList { get; set; }
        public IEnumerable<AppointmentDto> UpcomingAppointmentsList { get; set; }
        public Dictionary<string, int> AppointmentsByType { get; set; }
        public Dictionary<string, int> AppointmentsByStatus { get; set; }
        public int TotalAppointments => TodayAppointments + UpcomingAppointments + CompletedToday + CancelledToday;
        public decimal TotalAppointmentsChange => 0;
        public decimal CompletionRate => TotalAppointments > 0 ? (decimal)CompletedToday / TotalAppointments * 100 : 0;
        public decimal CompletionRateChange => 0;
        public decimal AverageWaitTime => 0;
        public decimal WaitTimeChange => 0;
        public decimal NoShowRate => 0;
        public decimal NoShowRateChange => 0;
        public IEnumerable<DoctorPerformanceItem> DoctorPerformance { get; set; } = new List<DoctorPerformanceItem>();
        public IEnumerable<RecentAppointmentActivityItem> RecentActivities { get; set; } = new List<RecentAppointmentActivityItem>();
        public IEnumerable<string> StatusChartLabels => (AppointmentsByStatus ?? new Dictionary<string, int>()).Keys;
        public IEnumerable<int> StatusChartData => (AppointmentsByStatus ?? new Dictionary<string, int>()).Values;
        public IEnumerable<string> TypeChartLabels => (AppointmentsByType ?? new Dictionary<string, int>()).Keys;
        public IEnumerable<int> TypeChartData => (AppointmentsByType ?? new Dictionary<string, int>()).Values;
        public IEnumerable<string> WeeklyTrendsLabels { get; set; } = new List<string>();
        public IEnumerable<int> WeeklyTrendsData { get; set; } = new List<int>();
        public IEnumerable<string> PeakHoursLabels { get; set; } = new List<string>();
        public IEnumerable<int> PeakHoursData { get; set; } = new List<int>();
    }

    // Supporting ViewModels
    public class PatientDto
    {
        public int Id { get; set; }
        public string PatientId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
    }

    public class DoctorPerformanceItem
    {
        public string DoctorName { get; set; }
        public string Specialization { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public decimal CompletionRate { get; set; }
        public decimal AverageWaitTime { get; set; }
        public decimal NoShowRate { get; set; }
    }

    public class RecentAppointmentActivityItem
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public string Details { get; set; }
        public string UserName { get; set; }
    }

    public class AppointmentViewModel
    {
        public AppointmentDto Appointment { get; set; }
        public PatientDto Patient { get; set; }
        public DoctorDto Doctor { get; set; }
        public string StatusBadgeClass => Appointment?.Status?.ToLower() switch
        {
            "scheduled" => "badge-warning",
            "confirmed" => "badge-info",
            "completed" => "badge-success",
            "cancelled" => "badge-danger",
            "no-show" => "badge-dark",
            _ => "badge-secondary"
        };
        public string FormattedAppointmentDate => Appointment?.AppointmentDate.ToString("MMM dd, yyyy") ?? "";
        public string FormattedAppointmentTime => Appointment?.AppointmentTime.ToString(@"hh\:mm tt") ?? "";
    }

    public class AppointmentDeleteViewModel
    {
        public AppointmentDto Appointment { get; set; }
        public PatientDto Patient { get; set; }
        public DoctorDto Doctor { get; set; }
        public bool HasFutureAppointments { get; set; }
        public string WarningMessage { get; set; }
    }

    public class AppointmentUpdateStatusViewModel
    {
        public AppointmentDto Appointment { get; set; }
        public AppointmentStatusUpdateDto StatusUpdate { get; set; }
        public PatientDto Patient { get; set; }
        public DoctorDto Doctor { get; set; }
        public List<string> StatusOrder => new() { "Scheduled", "Confirmed", "Completed", "Cancelled", "No-Show" };
        public string NewStatus
        {
            get => StatusUpdate?.Status;
            set
            {
                if (StatusUpdate != null)
                {
                    StatusUpdate.Status = value;
                }
            }
        }
        public string StatusNotes
        {
            get => StatusUpdate?.Notes;
            set
            {
                if (StatusUpdate != null)
                {
                    StatusUpdate.Notes = value;
                }
            }
        }

        public List<string> AvailableStatuses => new List<string>
        {
            "Scheduled", "Confirmed", "Completed", "Cancelled", "No-Show"
        };

        // Status transition rules
        public List<string> GetValidStatuses(string currentStatus)
        {
            return currentStatus?.ToLower() switch
            {
                "scheduled" => new List<string> { "Confirmed", "Cancelled" },
                "confirmed" => new List<string> { "Completed", "Cancelled", "No-Show" },
                "completed" => new List<string> { "Completed" }, // Final state
                "cancelled" => new List<string> { "Cancelled" }, // Final state
                "no-show" => new List<string> { "No-Show" }, // Final state
                _ => AvailableStatuses
            };
        }
    }
}