using System.ComponentModel.DataAnnotations;

// Purpose: Bills of items purchased (vendor purchase bills with a submit → approve → receive → pay lifecycle).
// Items consumed are recorded as stock issues (InventoryTransaction "OUT" with department / patient).
namespace MedyxHMS.Models
{
    public class PurchaseBill : IHospitalScoped
    {
        public int Id { get; set; }
        public int? HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        /// <summary>Internal number PB-yyyy-0001 (unique across the group).</summary>
        [StringLength(30)]
        public string BillNumber { get; set; } = string.Empty;

        [Display(Name = "Vendor")]
        public int VendorId { get; set; }
        public Vendor Vendor { get; set; } = null!;

        [Required, StringLength(50)]
        [Display(Name = "Vendor invoice no.")]
        public string VendorInvoiceNumber { get; set; } = string.Empty;

        [Display(Name = "Invoice date")]
        public DateTime InvoiceDate { get; set; } = DateTime.Today;

        [Display(Name = "Payment due")]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

        /// <summary>Draft, Submitted, Approved, Rejected, Received, Cancelled.</summary>
        [StringLength(20)]
        public string Status { get; set; } = "Draft";

        /// <summary>Unpaid, Partially paid, Paid.</summary>
        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Unpaid";

        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }

        public string Notes { get; set; } = string.Empty;

        public string CreatedByUserId { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? SubmittedAt { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
        public DateTime? ApprovedAt { get; set; }
        public string ApprovalComments { get; set; } = string.Empty;
        public string ReceivedBy { get; set; } = string.Empty;
        public DateTime? ReceivedAt { get; set; }
        public string CancelReason { get; set; } = string.Empty;

        public ICollection<PurchaseBillItem> Items { get; set; } = new List<PurchaseBillItem>();
        public ICollection<VendorPayment> Payments { get; set; } = new List<VendorPayment>();
    }

    public class PurchaseBillItem
    {
        public int Id { get; set; }
        public int PurchaseBillId { get; set; }
        public PurchaseBill PurchaseBill { get; set; } = null!;

        /// <summary>Stock item received into inventory when the goods arrive (optional, e.g. not for services).</summary>
        public int? InventoryItemId { get; set; }
        public InventoryItem? InventoryItem { get; set; }

        [Required, StringLength(200)]
        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal LineTotal { get; set; }

        [StringLength(50)]
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
    }

    public class VendorPayment
    {
        public int Id { get; set; }
        public int PurchaseBillId { get; set; }
        public PurchaseBill PurchaseBill { get; set; } = null!;

        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [StringLength(30)]
        public string Method { get; set; } = "Bank transfer";

        [StringLength(100)]
        public string Reference { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
