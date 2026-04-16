namespace MedyxHMS.Models
{
    public class Medicine
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string GenericName { get; set; }
        public string Category { get; set; }
        public string DosageForm { get; set; } // Tablet, Capsule, Syrup, Injection
        public string Strength { get; set; }
        public string Manufacturer { get; set; }
        public decimal UnitPrice { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string BatchNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class PharmacyBill
    {
        public int Id { get; set; }
        public string BillNumber { get; set; }
        public int PatientId { get; set; }
        public DateTime BillDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string Status { get; set; } // Paid, Pending, Cancelled
        public string PaymentMethod { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }

        // Navigation properties
        public Patient Patient { get; set; }
        public ICollection<Prescription> Prescriptions { get; set; }
    }

    public class Prescription
    {
        public int Id { get; set; }
        public int PharmacyBillId { get; set; }
        public int MedicineId { get; set; }
        public string Dosage { get; set; }
        public string Frequency { get; set; } // Once daily, Twice daily, etc.
        public int Duration { get; set; } // Duration in days
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Instructions { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public PharmacyBill PharmacyBill { get; set; }
        public Medicine Medicine { get; set; }
    }
}