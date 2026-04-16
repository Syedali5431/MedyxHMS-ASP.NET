using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.DTOs
{
    // Prescription DTOs
    public class PrescriptionDto
    {
        public int Id { get; set; }
        public int PharmacyBillId { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string Dosage { get; set; }
        public string Frequency { get; set; }
        public int Duration { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Instructions { get; set; }
        public DateTime CreatedDate { get; set; }
        public string FormattedCreatedDate => CreatedDate.ToString("MMM dd, yyyy");
    }

    public class PrescriptionCreateDto
    {
        [Required(ErrorMessage = "Pharmacy Bill ID is required")]
        public int PharmacyBillId { get; set; }

        [Required(ErrorMessage = "Medicine ID is required")]
        public int MedicineId { get; set; }

        [Required(ErrorMessage = "Dosage is required")]
        [StringLength(100, ErrorMessage = "Dosage cannot exceed 100 characters")]
        public string Dosage { get; set; }

        [Required(ErrorMessage = "Frequency is required")]
        [StringLength(100, ErrorMessage = "Frequency cannot exceed 100 characters")]
        public string Frequency { get; set; } // Once daily, Twice daily, etc.

        [Required(ErrorMessage = "Duration is required")]
        [Range(1, 365, ErrorMessage = "Duration must be between 1 and 365 days")]
        public int Duration { get; set; } // Duration in days

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10000")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0, 999999.99, ErrorMessage = "Unit price must be between 0 and 999999.99")]
        public decimal UnitPrice { get; set; }

        [StringLength(500, ErrorMessage = "Instructions cannot exceed 500 characters")]
        public string Instructions { get; set; }
    }

    public class PrescriptionUpdateDto
    {
        [Required(ErrorMessage = "Prescription ID is required")]
        public int Id { get; set; }

        [StringLength(100, ErrorMessage = "Dosage cannot exceed 100 characters")]
        public string Dosage { get; set; }

        [StringLength(100, ErrorMessage = "Frequency cannot exceed 100 characters")]
        public string Frequency { get; set; }

        [Range(1, 365, ErrorMessage = "Duration must be between 1 and 365 days")]
        public int? Duration { get; set; }

        [Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10000")]
        public int? Quantity { get; set; }

        [StringLength(500, ErrorMessage = "Instructions cannot exceed 500 characters")]
        public string Instructions { get; set; }
    }

    // Medicine DTOs
    public class MedicineDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string GenericName { get; set; }
        public string Category { get; set; }
        public string DosageForm { get; set; }
        public string Strength { get; set; }
        public string Manufacturer { get; set; }
        public decimal UnitPrice { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string BatchNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string StockStatus => StockQuantity <= MinStockLevel ? "Low Stock" : "Available";
        public string StockStatusBadgeClass => StockQuantity <= MinStockLevel ? "badge-danger" : "badge-success";
    }

    public class MedicineCreateDto
    {
        [Required(ErrorMessage = "Medicine name is required")]
        [StringLength(200, ErrorMessage = "Medicine name cannot exceed 200 characters")]
        public string Name { get; set; }

        [StringLength(200, ErrorMessage = "Generic name cannot exceed 200 characters")]
        public string GenericName { get; set; }

        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
        public string Category { get; set; }

        [Required(ErrorMessage = "Dosage form is required")]
        [StringLength(100, ErrorMessage = "Dosage form cannot exceed 100 characters")]
        public string DosageForm { get; set; }

        [StringLength(100, ErrorMessage = "Strength cannot exceed 100 characters")]
        public string Strength { get; set; }

        [StringLength(200, ErrorMessage = "Manufacturer cannot exceed 200 characters")]
        public string Manufacturer { get; set; }

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0, 999999.99, ErrorMessage = "Unit price must be between 0 and 999999.99")]
        public decimal UnitPrice { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, 1000000, ErrorMessage = "Stock quantity must be between 0 and 1000000")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Minimum stock level is required")]
        [Range(1, 1000000, ErrorMessage = "Minimum stock level must be between 1 and 1000000")]
        public int MinStockLevel { get; set; }

        [Required(ErrorMessage = "Expiry date is required")]
        public DateTime ExpiryDate { get; set; }

        [StringLength(50, ErrorMessage = "Batch number cannot exceed 50 characters")]
        public string BatchNumber { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class MedicineUpdateDto
    {
        [Required(ErrorMessage = "Medicine ID is required")]
        public int Id { get; set; }

        [StringLength(200, ErrorMessage = "Medicine name cannot exceed 200 characters")]
        public string Name { get; set; }

        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
        public string Category { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Unit price must be between 0 and 999999.99")]
        public decimal? UnitPrice { get; set; }

        [Range(0, 1000000, ErrorMessage = "Stock quantity must be between 0 and 1000000")]
        public int? StockQuantity { get; set; }

        [Range(1, 1000000, ErrorMessage = "Minimum stock level must be between 1 and 1000000")]
        public int? MinStockLevel { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool? IsActive { get; set; }
    }

    // Pharmacy Bill DTOs
    public class PharmacyBillDto
    {
        public int Id { get; set; }
        public string BillNumber { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public DateTime BillDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount => TotalAmount - PaidAmount;
        public string Status { get; set; }
        public string PaymentMethod { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public List<PrescriptionDto> Prescriptions { get; set; } = new();
        public string StatusBadgeClass => Status switch
        {
            "Paid" => "badge-success",
            "Pending" => "badge-warning",
            "Cancelled" => "badge-danger",
            _ => "badge-secondary"
        };
    }

    public class PharmacyBillCreateDto
    {
        [Required(ErrorMessage = "Patient ID is required")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Bill date is required")]
        public DateTime BillDate { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; }

        public List<PrescriptionCreateDto> Prescriptions { get; set; } = new();
    }

    public class PharmacyBillUpdateDto
    {
        [Required(ErrorMessage = "Bill ID is required")]
        public int Id { get; set; }

        public string Status { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; }
    }
}
