using MedyxHMS.DTOs;
using MedyxHMS.Models;
using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.ViewModels
{
    public class LabIndexViewModel
    {
        public IEnumerable<LabTestDto> LabTests { get; set; }
        public string SearchTerm { get; set; }
        public string StatusFilter { get; set; }
        public DateTime? DateFilter { get; set; }
        public int TotalLabTests { get; set; }
        public int PendingTests { get; set; }
        public int CompletedTests { get; set; }
        public int TodayTests { get; set; }

        // Filter options
        public List<string> StatusOptions => new List<string>
        {
            "All", "Pending", "In Progress", "Completed", "Cancelled"
        };
    }

    public class LabCreateViewModel
    {
        public LabTestCreateDto LabTest { get; set; }

        // Dropdown options
        public List<string> TestCategoryOptions => new List<string>
        {
            "Hematology", "Biochemistry", "Microbiology", "Immunology",
            "Endocrinology", "Toxicology", "Serology", "Urinalysis"
        };

        public IEnumerable<PatientDto> RecentPatients { get; set; }
        public IEnumerable<DoctorDto> AvailableDoctors { get; set; }

        // For patient search
        public string PatientSearchTerm { get; set; }
        public IEnumerable<PatientDto> PatientSearchResults { get; set; }
    }

    public class LabEditViewModel
    {
        public LabTestDto CurrentLabTest { get; set; }
        public LabTestUpdateDto LabTest { get; set; }

        // Dropdown options
        public List<string> TestCategoryOptions => new List<string>
        {
            "Hematology", "Biochemistry", "Microbiology", "Immunology",
            "Endocrinology", "Toxicology", "Serology", "Urinalysis"
        };

        public IEnumerable<DoctorDto> AvailableDoctors { get; set; }
    }

    public class LabDetailsViewModel
    {
        public LabTestDto LabTest { get; set; }
        public PatientDto Patient { get; set; }
        public DoctorDto Doctor { get; set; }
        public IEnumerable<LabResultDto> LabResults { get; set; }
        public IEnumerable<LabTestDto> PatientRecentTests { get; set; }
    }

    public class LabResultCreateViewModel
    {
        public LabResultCreateDto LabResult { get; set; }
        public LabTestDto LabTest { get; set; }
        public PatientDto Patient { get; set; }

        // Reference ranges for common tests
        public Dictionary<string, string> ReferenceRanges => new Dictionary<string, string>
        {
            ["Hemoglobin"] = "12-16 g/dL (Female), 14-18 g/dL (Male)",
            ["WBC Count"] = "4,000-11,000 cells/μL",
            ["Platelet Count"] = "150,000-450,000 cells/μL",
            ["Blood Glucose"] = "70-100 mg/dL (Fasting)",
            ["Cholesterol"] = "< 200 mg/dL",
            ["Triglycerides"] = "< 150 mg/dL"
        };
    }

    public class LabResultEditViewModel
    {
        public LabResultDto CurrentLabResult { get; set; }
        public LabResultUpdateDto LabResult { get; set; }
        public LabTestDto LabTest { get; set; }
        public PatientDto Patient { get; set; }
    }

    public class LabDashboardViewModel
    {
        public int TodayTests { get; set; }
        public int PendingTests { get; set; }
        public int CompletedToday { get; set; }
        public int CriticalResults { get; set; }
        public IEnumerable<LabTestDto> TodayTestsList { get; set; }
        public IEnumerable<LabTestDto> PendingTestsList { get; set; }
        public IEnumerable<LabResultDto> CriticalResultsList { get; set; }
        public Dictionary<string, int> TestsByCategory { get; set; }
        public Dictionary<string, int> ResultsByStatus { get; set; }
    }

    public class LabViewModel
    {
        public LabTestDto LabTest { get; set; }
        public PatientDto Patient { get; set; }
        public DoctorDto Doctor { get; set; }
        public IEnumerable<LabResultDto> Results { get; set; }
        public string StatusBadgeClass => LabTest?.Status?.ToLower() switch
        {
            "pending" => "badge-warning",
            "in progress" => "badge-info",
            "completed" => "badge-success",
            "cancelled" => "badge-danger",
            _ => "badge-secondary"
        };
        public string FormattedTestDate => LabTest?.TestDate.ToString("MMM dd, yyyy 'at' hh:mm tt") ?? "";
        public bool HasCriticalResults => Results?.Any(r => r.Status?.ToLower() == "critical") ?? false;
    }

    // Supporting DTOs
    public class LabTestDto
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

    public class LabResultDto
    {
        public int Id { get; set; }
        public int LabTestId { get; set; }
        public string Parameter { get; set; }
        public string Result { get; set; }
        public string Unit { get; set; }
        public string ReferenceRange { get; set; }
        public string Status { get; set; }
        public string Interpretation { get; set; }
        public DateTime ResultDate { get; set; }
        public string PerformedBy { get; set; }
        public string Notes { get; set; }
    }
}