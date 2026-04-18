using MedyxHMS.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.ViewModels
{
    // Billing and Financial System ViewModels

    public class BillViewModel
    {
        public BillDto Bill { get; set; } = new BillDto();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages => (TotalRecords + PageSize - 1) / PageSize;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public List<BillDto> Bills { get; set; } = new();
        public string Filter { get; set; } = "all";
    }

    public class BillDetailsViewModel
    {
        public BillDto Bill { get; set; } = new BillDto();
        public PatientPortalDto Patient { get; set; } = new PatientPortalDto();
        public List<PaymentDto> PaymentHistory { get; set; } = new();
        public decimal RemainingBalance => Bill?.OutstandingAmount ?? 0;
    }

    public class CreateBillViewModel
    {
        public BillCreateDto Bill { get; set; } = new();
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public List<BillItemCreateDto> LineItems { get; set; } = new();
        public decimal TotalAmount => LineItems.Sum(i => i.Amount);
    }

    public class EditBillViewModel
    {
        public string BillId { get; set; } = string.Empty;
        public BillUpdateDto Bill { get; set; } = new();
        public BillDto CurrentBill { get; set; } = new BillDto();
    }

    public class PaymentViewModel
    {
        [Required(ErrorMessage = "Bill ID is required")]
        public string BillId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment method is required")]
        public string PaymentMethod { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; } = string.Empty;

        public decimal BillAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public List<string> PaymentMethods { get; set; } = new() { "Cash", "Card", "Check", "Online", "Insurance" };
    }

    public class OnlinePaymentViewModel
    {
        public string BillId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string GatewayName { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string BillNumber { get; set; } = string.Empty;
    }

    public class RefundViewModel
    {
        public RefundCreateDto Refund { get; set; } = new();
        public PaymentDto Payment { get; set; } = new PaymentDto();
        public decimal MaxRefundAmount { get; set; }
    }

    public class BillingReportViewModel
    {
        public BillingReportDto Report { get; set; } = new BillingReportDto();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ReportType { get; set; } = "Monthly";
        public List<string> ReportTypes { get; set; } = new() { "Daily", "Weekly", "Monthly", "Yearly", "Custom" };
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PaymentGatewayConfigViewModel
    {
        public List<PaymentGatewayConfigDto> Gateways { get; set; } = new();
        public PaymentGatewayConfigCreateDto NewGateway { get; set; } = new();
    }

    public class InvoiceViewModel
    {
        public InvoiceDto Invoice { get; set; } = new InvoiceDto();
        public bool ShowDownloadButton { get; set; } = true;
        public bool ShowPrintButton { get; set; } = true;
        public bool ShowEmailButton { get; set; } = true;
    }

    public class ReceiptViewModel
    {
        public ReceiptDto Receipt { get; set; } = new ReceiptDto();
        public bool ShowDownloadButton { get; set; } = true;
        public bool ShowPrintButton { get; set; } = true;
        public bool ShowEmailButton { get; set; } = true;
    }

    public class DaySheetViewModel
    {
        public DateTime ReportDate { get; set; }
        public List<BillDto> Bills { get; set; } = new();
        public decimal TotalCollection { get; set; }
        public int TotalTransactions { get; set; }
        public decimal CashCollection { get; set; }
        public decimal CardCollection { get; set; }
        public decimal CheckCollection { get; set; }
        public decimal OnlineCollection { get; set; }
        public decimal InsuranceCollection { get; set; }
        public string PreparedBy { get; set; } = string.Empty;
        public string VerifiedBy { get; set; } = string.Empty;
    }

    public class CollectionReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalBilled { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal Outstanding { get; set; }
        public decimal CollectionPercentage { get; set; }
        public List<DepartmentBillingDto> DepartmentWise { get; set; } = new();
        public List<DailyCollectionDto> DailyCollection { get; set; } = new();
    }

    public class DailyCollectionDto
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public int TransactionCount { get; set; }
    }

    public class OutstandingBillsViewModel
    {
        public List<BillDto> Bills { get; set; } = new();
        public decimal TotalOutstanding { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalRecords { get; set; }
        public int TotalPages => (TotalRecords + PageSize - 1) / PageSize;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public string SortBy { get; set; } = "DueDate";
    }

    public class AgeingReportViewModel
    {
        public List<BillDto> CurrentBills { get; set; } = new(); // 0-30 days
        public List<BillDto> ThirtyPlusBills { get; set; } = new(); // 31-60 days
        public List<BillDto> SixtyPlusBills { get; set; } = new(); // 61-90 days
        public List<BillDto> NinetyPlusBills { get; set; } = new(); // 90+ days
        public decimal CurrentAmount { get; set; }
        public decimal ThirtyPlusAmount { get; set; }
        public decimal SixtyPlusAmount { get; set; }
        public decimal NinetyPlusAmount { get; set; }
        public DateTime AsOfDate { get; set; }
    }
}