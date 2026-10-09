using System.ComponentModel.DataAnnotations;

// Purpose: Vendors / suppliers master (shared by all hospitals of the group).
namespace MedyxHMS.Models
{
    public class Vendor
    {
        public int Id { get; set; }

        [StringLength(20)]
        [Display(Name = "Vendor code")]
        public string VendorCode { get; set; } = string.Empty;

        [Required, StringLength(150)]
        [Display(Name = "Vendor name")]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Category { get; set; } = "Medical consumables";

        [StringLength(100)]
        [Display(Name = "Contact person")]
        public string ContactPerson { get; set; } = string.Empty;

        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Tax no. (GST / ABN / VAT)")]
        public string TaxNumber { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Drug / trade licence no.")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Range(0, 365)]
        [Display(Name = "Payment terms (days)")]
        public int PaymentTermsDays { get; set; } = 30;

        [StringLength(100)]
        [Display(Name = "Bank name")]
        public string BankName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Account name")]
        public string BankAccountName { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Account no.")]
        public string BankAccountNumber { get; set; } = string.Empty;

        [StringLength(30)]
        [Display(Name = "IFSC / BSB / SWIFT")]
        public string BankRoutingCode { get; set; } = string.Empty;

        /// <summary>Supplier performance rating 1–5 (0 = not rated).</summary>
        [Range(0, 5)]
        public int Rating { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
    }
}
