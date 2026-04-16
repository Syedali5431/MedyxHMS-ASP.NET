using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.DTOs
{
    public class AppointmentDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public string PatientIdDisplay { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string Status { get; set; }
        public string AppointmentType { get; set; }
        public string Symptoms { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string StatusBadgeClass => GetStatusBadgeClass();
        public string FormattedDateTime => $"{AppointmentDate:MMM dd, yyyy} at {AppointmentTime:hh\\:mm tt}";

        private string GetStatusBadgeClass()
        {
            return Status?.ToLower() switch
            {
                "scheduled" => "badge-warning",
                "confirmed" => "badge-success",
                "completed" => "badge-primary",
                "cancelled" => "badge-danger",
                "no-show" => "badge-secondary",
                _ => "badge-light"
            };
        }
    }

    public class AppointmentCreateDto
    {
        [Required(ErrorMessage = "Patient is required")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Doctor is required")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Appointment date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Appointment Date")]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Appointment time is required")]
        [DataType(DataType.Time)]
        [Display(Name = "Appointment Time")]
        public TimeSpan AppointmentTime { get; set; }

        [Required(ErrorMessage = "Appointment type is required")]
        [Display(Name = "Appointment Type")]
        public string AppointmentType { get; set; }

        [StringLength(500, ErrorMessage = "Symptoms cannot exceed 500 characters")]
        public string Symptoms { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string Notes { get; set; }
    }

    public class AppointmentUpdateDto
    {
        [Required(ErrorMessage = "Appointment date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Appointment Date")]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Appointment time is required")]
        [DataType(DataType.Time)]
        [Display(Name = "Appointment Time")]
        public TimeSpan AppointmentTime { get; set; }

        [Required(ErrorMessage = "Appointment type is required")]
        [Display(Name = "Appointment Type")]
        public string AppointmentType { get; set; }

        [StringLength(500, ErrorMessage = "Symptoms cannot exceed 500 characters")]
        public string Symptoms { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string Notes { get; set; }
    }

    public class AppointmentStatusUpdateDto
    {
        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string Notes { get; set; }
    }

    // Supporting DTOs
    public class DoctorDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Specialization { get; set; }
        public string Department { get; set; }
        public bool IsActive { get; set; }
    }

    public class AppointmentSummaryDto
    {
        public int Id { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string Status { get; set; }
        public string AppointmentType { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
    }
}