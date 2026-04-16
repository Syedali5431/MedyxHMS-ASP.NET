using MedyxHMS.DTOs;
using MedyxHMS.Models;
using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.ViewModels
{
    public class RadiologyIndexViewModel
    {
        public IEnumerable<RadiologyTestDto> RadiologyTests { get; set; }
        public string SearchTerm { get; set; }
        public string StatusFilter { get; set; }
        public DateTime? DateFilter { get; set; }
        public int TotalRadiologyTests { get; set; }
        public int PendingTests { get; set; }
        public int CompletedTests { get; set; }
        public int TodayTests { get; set; }

        // Filter options
        public List<string> StatusOptions => new List<string>
        {
            "All", "Pending", "In Progress", "Completed", "Cancelled"
        };
    }

    public class RadiologyCreateViewModel
    {
        public RadiologyTestCreateDto RadiologyTest { get; set; }

        // Dropdown options
        public List<string> TestCategoryOptions => new List<string>
        {
            "X-Ray", "CT Scan", "MRI", "Ultrasound", "Mammography",
            "DEXA Scan", "PET Scan", "Nuclear Medicine", "Fluoroscopy"
        };

        public IEnumerable<PatientDto> RecentPatients { get; set; }
        public IEnumerable<DoctorDto> AvailableDoctors { get; set; }

        // For patient search
        public string PatientSearchTerm { get; set; }
        public IEnumerable<PatientDto> PatientSearchResults { get; set; }
    }

    public class RadiologyEditViewModel
    {
        public RadiologyTestDto CurrentRadiologyTest { get; set; }
        public RadiologyTestUpdateDto RadiologyTest { get; set; }

        // Dropdown options
        public List<string> TestCategoryOptions => new List<string>
        {
            "X-Ray", "CT Scan", "MRI", "Ultrasound", "Mammography",
            "DEXA Scan", "PET Scan", "Nuclear Medicine", "Fluoroscopy"
        };

        public IEnumerable<DoctorDto> AvailableDoctors { get; set; }
    }

    public class RadiologyDetailsViewModel
    {
        public RadiologyTestDto RadiologyTest { get; set; }
        public PatientDto Patient { get; set; }
        public DoctorDto Doctor { get; set; }
        public IEnumerable<RadiologyResultDto> RadiologyResults { get; set; }
        public IEnumerable<RadiologyTestDto> PatientRecentTests { get; set; }
    }

    public class RadiologyResultCreateViewModel
    {
        public RadiologyResultCreateDto RadiologyResult { get; set; }
        public RadiologyTestDto RadiologyTest { get; set; }
        public PatientDto Patient { get; set; }

        // Common findings options
        public List<string> CommonFindings => new List<string>
        {
            "Normal", "Abnormal", "Fracture", "Dislocation", "Arthritis",
            "Tumor", "Infection", "Inflammation", "Calcification", "Other"
        };
    }

    public class RadiologyResultEditViewModel
    {
        public RadiologyResultDto CurrentRadiologyResult { get; set; }
        public RadiologyResultUpdateDto RadiologyResult { get; set; }
        public RadiologyTestDto RadiologyTest { get; set; }
        public PatientDto Patient { get; set; }
    }

    public class RadiologyDashboardViewModel
    {
        public int TodayTests { get; set; }
        public int PendingTests { get; set; }
        public int CompletedToday { get; set; }
        public int CriticalFindings { get; set; }
        public IEnumerable<RadiologyTestDto> TodayTestsList { get; set; }
        public IEnumerable<RadiologyTestDto> PendingTestsList { get; set; }
        public IEnumerable<RadiologyResultDto> CriticalFindingsList { get; set; }
        public Dictionary<string, int> TestsByCategory { get; set; }
        public Dictionary<string, int> ResultsByStatus { get; set; }
    }

    public class RadiologyViewModel
    {
        public RadiologyTestDto RadiologyTest { get; set; }
        public PatientDto Patient { get; set; }
        public DoctorDto Doctor { get; set; }
        public IEnumerable<RadiologyResultDto> Results { get; set; }
        public string StatusBadgeClass => RadiologyTest?.Status?.ToLower() switch
        {
            "pending" => "badge-warning",
            "in progress" => "badge-info",
            "completed" => "badge-success",
            "cancelled" => "badge-danger",
            _ => "badge-secondary"
        };
        public string FormattedTestDate => RadiologyTest?.TestDate.ToString("MMM dd, yyyy 'at' hh:mm tt") ?? "";
        public bool HasCriticalFindings => Results?.Any(r => r.Status?.ToLower() == "critical") ?? false;
    }

    // Supporting DTOs
    public class RadiologyTestDto
    {
        public int Id { get; set; }
        public string TestCode { get; set; }
        public string TestName { get; set; }
        public string TestCategory { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; }
        public DateTime TestDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string PatientId { get; set; }
        public string DoctorId { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
    }

    public class RadiologyResultDto
    {
        public int Id { get; set; }
        public int RadiologyTestId { get; set; }
        public string Findings { get; set; }
        public string Impression { get; set; }
        public string Recommendations { get; set; }
        public string Status { get; set; }
        public string ImagePath { get; set; }
        public DateTime ResultDate { get; set; }
        public string PerformedBy { get; set; }
        public string ReviewedBy { get; set; }
        public string Notes { get; set; }
    }
}