namespace MedyxHMS.Models
{
    public class Bill
    {
        public int Id { get; set; }
        public string BillNumber { get; set; }
        public int PatientId { get; set; }
        public DateTime BillDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public string Status { get; set; } // Unpaid, Partially Paid, Paid, Overdue
        public string BillType { get; set; } // OPD, IPD, Pharmacy, Lab, Radiology
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }

        // Navigation properties
        public Patient Patient { get; set; }
        public ICollection<BillItem> BillItems { get; set; }
        public ICollection<Payment> Payments { get; set; }
    }

    public class BillItem
    {
        public int Id { get; set; }
        public int BillId { get; set; }
        public string ItemName { get; set; }
        public string ItemType { get; set; } // Service, Medicine, Test, etc.
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Bill Bill { get; set; }
    }

    public class Payment
    {
        public int Id { get; set; }
        public int BillId { get; set; }
        public string PaymentMethod { get; set; } // Cash, Card, Online, Insurance
        public decimal Amount { get; set; }
        public string TransactionId { get; set; }
        public string PaymentGateway { get; set; } // PayPal, Stripe, etc.
        public string Status { get; set; } // Pending, Completed, Failed, Refunded
        public string Notes { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string ProcessedBy { get; set; }

        // Navigation properties
        public Bill Bill { get; set; }
    }

    public class Transaction
    {
        public int Id { get; set; }
        public string TransactionId { get; set; }
        public string TransactionType { get; set; } // Payment, Refund, Adjustment
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public string ProcessedBy { get; set; }
        public string Status { get; set; } // Completed, Pending, Failed
    }
}