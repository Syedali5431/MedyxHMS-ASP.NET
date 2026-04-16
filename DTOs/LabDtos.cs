using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.DTOs
{
    // ======== Lab Test DTOs ========
    public class LabTestDto
    {
        public int Id { get; set; }
        public string TestName { get; set; }
        public string TestCode { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string NormalRange { get; set; }
        public string Unit { get; set; }
        public int PreparationTimeHours { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedDateFormatted => CreatedDate.ToString("MMM dd, yyyy");
    }

    public class LabTestCreateDto
    {
        [Required(ErrorMessage = "Test name is required")]
        [StringLength(100, ErrorMessage = "Test name cannot exceed 100 characters")]
        public string TestName { get; set; }

        [Required(ErrorMessage = "Test code is required")]
        [StringLength(20, ErrorMessage = "Test code cannot exceed 20 characters")]
        public string TestCode { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string Category { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999999.99")]
        public decimal Price { get; set; }

        [StringLength(100, ErrorMessage = "Normal range cannot exceed 100 characters")]
        public string NormalRange { get; set; }

        [StringLength(20, ErrorMessage = "Unit cannot exceed 20 characters")]
        public string Unit { get; set; }

        [Required(ErrorMessage = "Preparation time is required")]
        [Range(0, 240, ErrorMessage = "Preparation time must be between 0 and 240 hours")]
        public int PreparationTimeHours { get; set; }
    }

    public class LabTestUpdateDto
    {
        public int Id { get; set; }

        [StringLength(100, ErrorMessage = "Test name cannot exceed 100 characters")]
        public string TestName { get; set; }

        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string Category { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999999.99")]
        public decimal Price { get; set; }

        [StringLength(100, ErrorMessage = "Normal range cannot exceed 100 characters")]
        public string NormalRange { get; set; }

        [Range(0, 240, ErrorMessage = "Preparation time must be between 0 and 240 hours")]
        public int PreparationTimeHours { get; set; }

        public bool? IsActive { get; set; }
    }

    // ======== Lab Result DTOs ========
    public class LabResultDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int LabTestId { get; set; }
        public string PatientName { get; set; }
        public string TestName { get; set; }
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ResultDate { get; set; }
        public string ResultValue { get; set; }
        public string NormalRange { get; set; }
        public string Unit { get; set; }
        public string Interpretation { get; set; }
        public string Status { get; set; }
        public string PerformedBy { get; set; }
        public string VerifiedBy { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public string OrderDateFormatted => OrderDate.ToString("MMM dd, yyyy hh:mm tt");
        public string ResultDateFormatted => ResultDate?.ToString("MMM dd, yyyy hh:mm tt") ?? "Not Yet Completed";
        public string StatusBadgeClass => Status switch
        {
            "Completed" => "badge bg-success",
            "In Progress" => "badge bg-info",
            "Ordered" => "badge bg-primary",
            "Cancelled" => "badge bg-danger",
            _ => "badge bg-secondary"
        };
        public string InterpretationBadgeClass => Interpretation switch
        {
            "Normal" => "badge bg-success",
            "High" => "badge bg-warning",
            "Low" => "badge bg-warning",
            "Abnormal" => "badge bg-danger",
            _ => "badge bg-secondary"
        };
    }

    public class LabResultCreateDto
    {
        [Required(ErrorMessage = "Patient is required")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Lab test is required")]
        public int LabTestId { get; set; }

        [StringLength(100, ErrorMessage = "Order number cannot exceed 100 characters")]
        public string OrderNumber { get; set; }

        [Required(ErrorMessage = "Order date is required")]
        public DateTime OrderDate { get; set; }
    }

    public class LabResultUpdateDto
    {
        public int Id { get; set; }

        public DateTime? ResultDate { get; set; }

        [StringLength(100, ErrorMessage = "Result value cannot exceed 100 characters")]
        public string ResultValue { get; set; }

        [StringLength(100, ErrorMessage = "Normal range cannot exceed 100 characters")]
        public string NormalRange { get; set; }

        [StringLength(20, ErrorMessage = "Unit cannot exceed 20 characters")]
        public string Unit { get; set; }

        [StringLength(50, ErrorMessage = "Interpretation cannot exceed 50 characters")]
        public string Interpretation { get; set; }

        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
        public string Status { get; set; }

        [StringLength(100, ErrorMessage = "Performed by cannot exceed 100 characters")]
        public string PerformedBy { get; set; }

        [StringLength(100, ErrorMessage = "Verified by cannot exceed 100 characters")]
        public string VerifiedBy { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; }
    }

    // ======== Lab Statistics DTO ========
    public class LabStatisticsDto
    {
        public int TotalTests { get; set; }
        public int PendingTests { get; set; }
        public int CompletedTests { get; set; }
        public decimal TotalRevenue { get; set; }
        public Dictionary<string, int> TestsByCategory { get; set; }
        public Dictionary<string, int> TestsByStatus { get; set; }
    }
}
