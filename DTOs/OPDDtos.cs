using System.ComponentModel.DataAnnotations;

// Purpose: Contains application code for OPDDtos and its related runtime behavior.
namespace MedyxHMS.DTOs
{
    // OPD (Outpatient Department) and IPD (Inpatient Department) DTOs

    public class OPDVisitDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public DateTime VisitDate { get; set; }
        public string Symptoms { get; set; }
        public string Diagnosis { get; set; }
        public string Treatment { get; set; }
        public string Prescription { get; set; }
        public string Notes { get; set; }
        public decimal ConsultationFee { get; set; }
        public string PaymentStatus { get; set; } // Paid, Pending, Waived
        public string Department { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string FormattedVisitDate => VisitDate.ToString("MMM dd, yyyy HH:mm");
        public string PaymentStatusBadgeClass => PaymentStatus switch
        {
            "Paid" => "badge-success",
            "Pending" => "badge-warning",
            "Waived" => "badge-info",
            _ => "badge-secondary"
        };
        public string StatusBadgeClass => Status switch
        {
            "Completed" => "badge-success",
            "In Progress" => "badge-info",
            "Pending" => "badge-warning",
            "Cancelled" => "badge-danger",
            _ => "badge-secondary"
        };
    }

    public class OPDVisitCreateDto
    {
        [Required(ErrorMessage = "Patient ID is required")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Doctor ID is required")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Visit date is required")]
        public DateTime VisitDate { get; set; }

        [Required(ErrorMessage = "Symptoms are required")]
        [StringLength(1000, ErrorMessage = "Symptoms cannot exceed 1000 characters")]
        public string Symptoms { get; set; }

        [StringLength(1000, ErrorMessage = "Diagnosis cannot exceed 1000 characters")]
        public string Diagnosis { get; set; }

        [StringLength(2000, ErrorMessage = "Treatment cannot exceed 2000 characters")]
        public string Treatment { get; set; }

        [StringLength(2000, ErrorMessage = "Prescription cannot exceed 2000 characters")]
        public string Prescription { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string Notes { get; set; }

        [Required(ErrorMessage = "Consultation fee is required")]
        [Range(0, 999999.99, ErrorMessage = "Consultation fee must be between 0 and 999999.99")]
        public decimal ConsultationFee { get; set; }

        [Required(ErrorMessage = "Payment status is required")]
        public string PaymentStatus { get; set; } = "Pending";
    }

    public class OPDVisitUpdateDto
    {
        [Required(ErrorMessage = "Visit ID is required")]
        public int Id { get; set; }

        [StringLength(1000, ErrorMessage = "Diagnosis cannot exceed 1000 characters")]
        public string Diagnosis { get; set; }

        [StringLength(2000, ErrorMessage = "Treatment cannot exceed 2000 characters")]
        public string Treatment { get; set; }

        [StringLength(2000, ErrorMessage = "Prescription cannot exceed 2000 characters")]
        public string Prescription { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string Notes { get; set; }

        public string PaymentStatus { get; set; }
    }

    public class IPDAdmissionDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public int? BedId { get; set; }
        public string BedNumber { get; set; }
        public string WardName { get; set; }
        public DateTime AdmissionDate { get; set; }
        public DateTime? DischargeDate { get; set; }
        public string AdmissionType { get; set; } // Emergency, Planned
        public string Diagnosis { get; set; }
        public string Treatment { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; } // Admitted, Discharged, Transferred
        public decimal DailyCharges { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public int? DaysAdmitted => DischargeDate.HasValue ? (int?)(DischargeDate.Value - AdmissionDate).TotalDays : null;
        public string FormattedAdmissionDate => AdmissionDate.ToString("MMM dd, yyyy HH:mm");
        public string FormattedDischargeDate => DischargeDate?.ToString("MMM dd, yyyy HH:mm");
        public string StatusBadgeClass => Status switch
        {
            "Admitted" => "badge-primary",
            "Discharged" => "badge-success",
            "Transferred" => "badge-warning",
            _ => "badge-secondary"
        };
        public string AdmissionTypeBadgeClass => AdmissionType switch
        {
            "Emergency" => "badge-danger",
            "Planned" => "badge-info",
            _ => "badge-secondary"
        };
    }

    public class IPDAdmissionCreateDto
    {
        [Required(ErrorMessage = "Patient ID is required")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Doctor ID is required")]
        public int DoctorId { get; set; }

        public int? BedId { get; set; }

        [Required(ErrorMessage = "Admission date is required")]
        public DateTime AdmissionDate { get; set; }

        [Required(ErrorMessage = "Admission type is required")]
        public string AdmissionType { get; set; } // Emergency, Planned

        [StringLength(1000, ErrorMessage = "Diagnosis cannot exceed 1000 characters")]
        public string Diagnosis { get; set; }

        [StringLength(2000, ErrorMessage = "Treatment cannot exceed 2000 characters")]
        public string Treatment { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string Notes { get; set; }

        [Required(ErrorMessage = "Daily charges are required")]
        [Range(0, 999999.99, ErrorMessage = "Daily charges must be between 0 and 999999.99")]
        public decimal DailyCharges { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = "Admitted";
    }

    public class IPDAdmissionUpdateDto
    {
        [Required(ErrorMessage = "Admission ID is required")]
        public int Id { get; set; }

        public int? BedId { get; set; }

        public DateTime? DischargeDate { get; set; }

        [StringLength(1000, ErrorMessage = "Diagnosis cannot exceed 1000 characters")]
        public string Diagnosis { get; set; }

        [StringLength(2000, ErrorMessage = "Treatment cannot exceed 2000 characters")]
        public string Treatment { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string Notes { get; set; }

        public string Status { get; set; }
    }

    public class WardDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds => TotalBeds - OccupiedBeds;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public double OccupancyRate => TotalBeds > 0 ? (double)OccupiedBeds / TotalBeds * 100 : 0;
        public string OccupancyStatus => OccupancyRate switch
        {
            < 50 => "Low",
            < 80 => "Medium",
            < 100 => "High",
            _ => "Full"
        };
        public string OccupancyStatusBadgeClass => OccupancyRate switch
        {
            < 50 => "badge-success",
            < 80 => "badge-warning",
            < 100 => "badge-danger",
            _ => "badge-dark"
        };
        public List<BedDto> Beds { get; set; } = new();
    }

    public class WardCreateDto
    {
        [Required(ErrorMessage = "Ward name is required")]
        [StringLength(100, ErrorMessage = "Ward name cannot exceed 100 characters")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Total beds is required")]
        [Range(1, 1000, ErrorMessage = "Total beds must be between 1 and 1000")]
        public int TotalBeds { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class WardUpdateDto
    {
        [Required(ErrorMessage = "Ward ID is required")]
        public int Id { get; set; }

        [StringLength(100, ErrorMessage = "Ward name cannot exceed 100 characters")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        [Range(1, 1000, ErrorMessage = "Total beds must be between 1 and 1000")]
        public int? TotalBeds { get; set; }

        public bool? IsActive { get; set; }
    }

    public class BedDto
    {
        public int Id { get; set; }
        public int WardId { get; set; }
        public string WardName { get; set; }
        public string BedNumber { get; set; }
        public string BedType { get; set; } // General, ICU, Private, Semi-private
        public decimal DailyCharges { get; set; }
        public string Status { get; set; } // Available, Occupied, Maintenance
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string StatusBadgeClass => Status switch
        {
            "Available" => "badge-success",
            "Occupied" => "badge-danger",
            "Maintenance" => "badge-warning",
            _ => "badge-secondary"
        };
        public string BedTypeBadgeClass => BedType switch
        {
            "General" => "badge-primary",
            "ICU" => "badge-danger",
            "Private" => "badge-info",
            "Semi-private" => "badge-warning",
            _ => "badge-secondary"
        };
    }

    public class BedCreateDto
    {
        [Required(ErrorMessage = "Ward ID is required")]
        public int WardId { get; set; }

        [Required(ErrorMessage = "Bed number is required")]
        [StringLength(20, ErrorMessage = "Bed number cannot exceed 20 characters")]
        public string BedNumber { get; set; }

        [Required(ErrorMessage = "Bed type is required")]
        public string BedType { get; set; } // General, ICU, Private, Semi-private

        [Required(ErrorMessage = "Daily charges are required")]
        [Range(0, 999999.99, ErrorMessage = "Daily charges must be between 0 and 999999.99")]
        public decimal DailyCharges { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = "Available";

        public bool IsActive { get; set; } = true;
    }

    public class BedUpdateDto
    {
        [Required(ErrorMessage = "Bed ID is required")]
        public int Id { get; set; }

        [StringLength(20, ErrorMessage = "Bed number cannot exceed 20 characters")]
        public string BedNumber { get; set; }

        public string BedType { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Daily charges must be between 0 and 999999.99")]
        public decimal? DailyCharges { get; set; }

        public string Status { get; set; }

        public bool? IsActive { get; set; }
    }
}
