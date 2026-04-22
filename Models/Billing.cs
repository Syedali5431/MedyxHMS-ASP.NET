// Purpose: Contains application code for Billing and its related runtime behavior.
namespace MedyxHMS.Models
{
    public class Bill
    {
        public int Id { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public int PatientId { get; set; }
        public int? AppointmentId { get; set; }
        public DateTime BillDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public string Status { get; set; } = string.Empty; // Unpaid, Partially Paid, Paid, Overdue
        public string BillType { get; set; } = string.Empty; // OPD, IPD, Pharmacy, Lab, Radiology
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public bool CanPay => Status != "Paid" && Status != "Cancelled";

        // Navigation properties
        public Patient Patient { get; set; } = null!;
        public ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public class BillItem
    {
        public int Id { get; set; }
        public int BillId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty; // Service, Medicine, Test, etc.
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal Amount
        {
            get => TotalPrice;
            set => TotalPrice = value;
        }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Bill Bill { get; set; } = null!;
    }

    public class Payment
    {
        public int Id { get; set; }
        public int BillId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // Cash, Card, Online, Insurance
        public decimal Amount { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string PaymentGateway { get; set; } = string.Empty; // PayPal, Stripe, etc.
        public string Status { get; set; } = string.Empty; // Pending, Completed, Failed, Refunded
        public string Notes { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string ProcessedBy { get; set; } = string.Empty;

        // Navigation properties
        public Bill Bill { get; set; } = null!;
    }

    public class Transaction
    {
        public int Id { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty; // Payment, Refund, Adjustment
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public string ProcessedBy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Completed, Pending, Failed
    }
}
