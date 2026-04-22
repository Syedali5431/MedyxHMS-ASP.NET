using System.ComponentModel.DataAnnotations;

// Purpose: Contains application code for BillingDtos and its related runtime behavior.
namespace MedyxHMS.DTOs
{
    // Billing and Financial System DTOs

    public class BillDto
    {
        public string Id { get; set; }
        public string BillNumber { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int? AppointmentId { get; set; }
        public DateTime BillDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount
        {
            get => OutstandingAmount;
            set { }
        }
        public string BillType { get; set; }
        public decimal OutstandingAmount => TotalAmount - PaidAmount;
        public string Status { get; set; } // Pending, Paid, Overdue, Cancelled
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool CanPay => Status != "Paid" && Status != "Cancelled";
        public string StatusBadgeClass => Status switch
        {
            "Paid" => "badge-success",
            "Pending" => "badge-warning",
            "Overdue" => "badge-danger",
            "Cancelled" => "badge-secondary",
            _ => "badge-info"
        };
        public List<BillItemDto> Items { get; set; } = new();
        public List<PaymentDto> Payments { get; set; } = new();
    }

    public class BillCreateDto
    {
        [Required(ErrorMessage = "Patient ID is required")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Bill date is required")]
        public DateTime BillDate { get; set; }

        public DateTime? DueDate { get; set; }

        public int? AppointmentId { get; set; }

        [Required(ErrorMessage = "At least one bill item is required")]
        public List<BillItemCreateDto> Items { get; set; } = new();

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; }
    }

    public class BillUpdateDto
    {
        [Required(ErrorMessage = "Bill ID is required")]
        public string Id { get; set; }

        public DateTime? DueDate { get; set; }

        public string Status { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; }

        public List<BillItemCreateDto> Items { get; set; } = new();
    }

    public class BillItemDto
    {
        public string Id { get; set; }
        public string BillId { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Category { get; set; } // Consultation, Medicine, Lab, Radiology, etc.
        public string ItemType { get; set; } // Service, Product, Tax, Discount
        public DateTime CreatedDate { get; set; }
    }

    public class BillItemCreateDto
    {
        [Required(ErrorMessage = "Description is required")]
        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Unit price must be greater than 0")]
        public decimal UnitPrice { get; set; }

        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string Category { get; set; }

        [Required(ErrorMessage = "Item type is required")]
        public string ItemType { get; set; } // Service, Product, Tax, Discount

        public decimal Amount => Quantity * UnitPrice;
    }

    public class PaymentDto
    {
        public string Id { get; set; }
        public string BillId { get; set; }
        public int PatientId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } // Cash, Card, Check, Online, Insurance
        public string TransactionId { get; set; }
        public string GatewayReference { get; set; }
        public string Status { get; set; } // Success, Failed, Pending, Refunded
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public string FormattedDate => PaymentDate.ToString("MMM dd, yyyy HH:mm");
        public string PaymentMethodBadge => PaymentMethod switch
        {
            "Cash" => "badge-primary",
            "Card" => "badge-info",
            "Check" => "badge-secondary",
            "Online" => "badge-success",
            "Insurance" => "badge-warning",
            _ => "badge-light"
        };
    }

    public class PaymentCreateDto
    {
        [Required(ErrorMessage = "Bill ID is required")]
        public string BillId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment method is required")]
        [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters")]
        public string PaymentMethod { get; set; }

        [StringLength(100, ErrorMessage = "Transaction ID cannot exceed 100 characters")]
        public string TransactionId { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; }
    }

    public class RefundDto
    {
        public string Id { get; set; }
        public string PaymentId { get; set; }
        public string BillId { get; set; }
        public decimal Amount { get; set; }
        public DateTime RefundDate { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; } // Pending, Approved, Rejected
        public DateTime CreatedDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
    }

    public class RefundCreateDto
    {
        [Required(ErrorMessage = "Payment ID is required")]
        public string PaymentId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; }
    }

    public class BillingReportDto
    {
        public string ReportType { get; set; } // Daily, Weekly, Monthly, Yearly, Custom
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalBilled { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalRefunded { get; set; }
        public int TotalBills { get; set; }
        public int PaidBills { get; set; }
        public int PendingBills { get; set; }
        public int OverdueBills { get; set; }
        public int CancelledBills { get; set; }
        public decimal AverageBillAmount => TotalBills > 0 ? TotalBilled / TotalBills : 0;
        public decimal CollectionPercentage => TotalBilled > 0 ? (TotalPaid / TotalBilled) * 100 : 0;
        public List<BillDto> Bills { get; set; } = new();
        public List<DepartmentBillingDto> DepartmentBreakdown { get; set; } = new();
    }

    public class DepartmentBillingDto
    {
        public string Department { get; set; }
        public decimal TotalBilled { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Outstanding { get; set; }
        public int BillCount { get; set; }
        public decimal CollectionPercentage { get; set; }
    }

    public class PaymentGatewayConfigDto
    {
        public string Id { get; set; }
        public string GatewayName { get; set; } // Stripe, PayPal, Razorpay, etc.
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public string WebhookSecret { get; set; }
        public bool IsActive { get; set; }
        public bool IsTestMode { get; set; }
        public decimal TransactionFee { get; set; }
        public string Currency { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class PaymentGatewayConfigCreateDto
    {
        [Required(ErrorMessage = "Gateway name is required")]
        [StringLength(100, ErrorMessage = "Gateway name cannot exceed 100 characters")]
        public string GatewayName { get; set; }

        [Required(ErrorMessage = "Public key is required")]
        [StringLength(500, ErrorMessage = "Public key cannot exceed 500 characters")]
        public string PublicKey { get; set; }

        [Required(ErrorMessage = "Private key is required")]
        [StringLength(500, ErrorMessage = "Private key cannot exceed 500 characters")]
        public string PrivateKey { get; set; }

        [StringLength(500, ErrorMessage = "Webhook secret cannot exceed 500 characters")]
        public string WebhookSecret { get; set; }

        [Range(0, 100, ErrorMessage = "Transaction fee must be between 0 and 100")]
        public decimal TransactionFee { get; set; }

        [StringLength(10, ErrorMessage = "Currency cannot exceed 10 characters")]
        public string Currency { get; set; } = "USD";

        public bool IsTestMode { get; set; }
    }

    public class InvoiceDto
    {
        public string Id { get; set; }
        public string InvoiceNumber { get; set; }
        public string BillId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public string PatientEmail { get; set; }
        public string PatientPhone { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceDue { get; set; }
        public string Notes { get; set; }
        public List<BillItemDto> LineItems { get; set; } = new();
        public string HospitalName { get; set; }
        public string HospitalAddress { get; set; }
        public string HospitalPhone { get; set; }
        public string HospitalEmail { get; set; }
    }

    public class ReceiptDto
    {
        public string Id { get; set; }
        public string ReceiptNumber { get; set; }
        public string PaymentId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public string BillNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionId { get; set; }
        public string Notes { get; set; }
        public string HospitalName { get; set; }
        public string HospitalAddress { get; set; }
        public string HospitalPhone { get; set; }
        public string HospitalEmail { get; set; }
    }
}
