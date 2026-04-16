namespace MedyxHMS.Models
{
    public class RadiologyTest
    {
        public int Id { get; set; }
        public string TestName { get; set; }
        public string TestCode { get; set; }
        public string Category { get; set; } // X-Ray, CT Scan, MRI, Ultrasound, etc.
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int PreparationTimeHours { get; set; }
        public string SpecialInstructions { get; set; }
        public bool RequiresContrast { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class RadiologyResult
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int RadiologyTestId { get; set; }
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ResultDate { get; set; }
        public string Findings { get; set; }
        public string Impression { get; set; }
        public string Status { get; set; } // Ordered, In Progress, Completed, Cancelled
        public string PerformedBy { get; set; }
        public string VerifiedBy { get; set; }
        public string ImagePath { get; set; } // Path to stored images
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Patient Patient { get; set; }
        public RadiologyTest RadiologyTest { get; set; }
    }
}