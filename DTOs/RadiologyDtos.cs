using System.ComponentModel.DataAnnotations;

// Purpose: Contains application code for RadiologyDtos and its related runtime behavior.
namespace MedyxHMS.DTOs
{
    // ======== Radiology Test DTOs ========
    public class RadiologyTestDto
    {
        public int Id { get; set; }
        public string TestName { get; set; }
        public string TestCode { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int PreparationTimeHours { get; set; }
        public string SpecialInstructions { get; set; }
        public bool RequiresContrast { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedDateFormatted => CreatedDate.ToString("MMM dd, yyyy");
        public string ContrastBadge => RequiresContrast ? "<span class='badge bg-warning'>Requires Contrast</span>" : "<span class='badge bg-success'>No Contrast</span>";
    }

    public class RadiologyTestCreateDto
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

        [Required(ErrorMessage = "Preparation time is required")]
        [Range(0, 240, ErrorMessage = "Preparation time must be between 0 and 240 hours")]
        public int PreparationTimeHours { get; set; }

        [StringLength(500, ErrorMessage = "Special instructions cannot exceed 500 characters")]
        public string SpecialInstructions { get; set; }

        public bool RequiresContrast { get; set; }
    }

    public class RadiologyTestUpdateDto
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

        [Range(0, 240, ErrorMessage = "Preparation time must be between 0 and 240 hours")]
        public int PreparationTimeHours { get; set; }

        [StringLength(500, ErrorMessage = "Special instructions cannot exceed 500 characters")]
        public string SpecialInstructions { get; set; }

        public bool? RequiresContrast { get; set; }

        public bool? IsActive { get; set; }
    }

    // ======== Radiology Result DTOs ========
    public class RadiologyResultDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int RadiologyTestId { get; set; }
        public string PatientName { get; set; }
        public string TestName { get; set; }
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ResultDate { get; set; }
        public string Findings { get; set; }
        public string Impression { get; set; }
        public string Status { get; set; }
        public string PerformedBy { get; set; }
        public string VerifiedBy { get; set; }
        public string ImagePath { get; set; }
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
        public bool HasImages => !string.IsNullOrEmpty(ImagePath);
    }

    public class RadiologyResultCreateDto
    {
        [Required(ErrorMessage = "Patient is required")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Radiology test is required")]
        public int RadiologyTestId { get; set; }

        [StringLength(100, ErrorMessage = "Order number cannot exceed 100 characters")]
        public string OrderNumber { get; set; }

        [Required(ErrorMessage = "Order date is required")]
        public DateTime OrderDate { get; set; }
    }

    public class RadiologyResultUpdateDto
    {
        public int Id { get; set; }

        public DateTime? ResultDate { get; set; }

        [StringLength(2000, ErrorMessage = "Findings cannot exceed 2000 characters")]
        public string Findings { get; set; }

        [StringLength(1000, ErrorMessage = "Impression cannot exceed 1000 characters")]
        public string Impression { get; set; }

        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
        public string Status { get; set; }

        [StringLength(100, ErrorMessage = "Performed by cannot exceed 100 characters")]
        public string PerformedBy { get; set; }

        [StringLength(100, ErrorMessage = "Verified by cannot exceed 100 characters")]
        public string VerifiedBy { get; set; }

        [StringLength(500, ErrorMessage = "Image path cannot exceed 500 characters")]
        public string ImagePath { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; }
    }

    // ======== Radiology Statistics DTO ========
    public class RadiologyStatisticsDto
    {
        public int TotalTests { get; set; }
        public int PendingTests { get; set; }
        public int CompletedTests { get; set; }
        public decimal TotalRevenue { get; set; }
        public Dictionary<string, int> TestsByCategory { get; set; }
        public Dictionary<string, int> TestsByStatus { get; set; }
    }

    // ======== Image Upload DTO ========
    public class RadiologyImageUploadDto
    {
        public int RadiologyResultId { get; set; }

        [Required(ErrorMessage = "Image file is required")]
        public IFormFile ImageFile { get; set; }

        [StringLength(500, ErrorMessage = "Image description cannot exceed 500 characters")]
        public string ImageDescription { get; set; }
    }
}
