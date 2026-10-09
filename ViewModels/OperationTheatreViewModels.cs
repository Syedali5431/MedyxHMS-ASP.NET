using System.ComponentModel.DataAnnotations;
using MedyxHMS.Models;

// Purpose: Form and availability models for operation theatre bookings.
namespace MedyxHMS.ViewModels
{
    public class OTBookingInput
    {
        public int Id { get; set; }

        [Display(Name = "Patient")]
        [Range(1, int.MaxValue, ErrorMessage = "Choose the patient.")]
        public int PatientId { get; set; }

        [Display(Name = "Procedure")]
        [Required(ErrorMessage = "Enter the procedure.")]
        [StringLength(200)]
        public string ProcedureName { get; set; } = string.Empty;

        [Display(Name = "Surgeon")]
        public int? SurgeonDoctorId { get; set; }

        [Display(Name = "Visiting surgeon (not in the doctors list)")]
        [StringLength(100)]
        public string? SurgeonName { get; set; }

        [Display(Name = "Theatre")]
        [Range(1, int.MaxValue, ErrorMessage = "Choose the theatre.")]
        public int OperationTheatreId { get; set; }

        [Display(Name = "Start")]
        public DateTime ScheduledDate { get; set; }

        [Display(Name = "Estimated duration (minutes)")]
        [Range(15, 720, ErrorMessage = "The estimated duration must be between 15 minutes and 12 hours.")]
        public int EstimatedDurationMinutes { get; set; } = 60;

        [Display(Name = "Emergency case (may be booked outside opening hours)")]
        public bool IsEmergency { get; set; }

        public string? Notes { get; set; }
    }

    /// <summary>A free period of a theatre and the longest case that fits into it (turnover included).</summary>
    public record OTFreeWindow(DateTime From, DateTime To, int LongestCaseMinutes);

    /// <summary>One theatre on one day: opening period, cases, block times and free periods.</summary>
    public class OTTheatreDay
    {
        public OperationTheatre Theatre { get; init; } = null!;
        public DateTime OpenFrom { get; init; }
        public DateTime OpenUntil { get; init; }
        public List<OTSchedule> Cases { get; init; } = new();
        public List<OTBlock> Blocks { get; init; } = new();
        public List<OTFreeWindow> Free { get; init; } = new();
        public int BookedMinutes { get; init; }
        public int OpenMinutes => (int)(OpenUntil - OpenFrom).TotalMinutes;
    }

    public class OTAvailabilityViewModel
    {
        public DateTime Date { get; init; }
        public int Duration { get; init; }
        public List<OTTheatreDay> Theatres { get; init; } = new();
        /// <summary>Next seven days: theatre id → (cases, booked minutes, open minutes) per day.</summary>
        public List<DateTime> WeekDays { get; init; } = new();
        public Dictionary<int, List<(int Cases, int Booked, int Open)>> Week { get; init; } = new();
        public DateTime GridFrom { get; init; }
        public DateTime GridUntil { get; init; }
    }
}
