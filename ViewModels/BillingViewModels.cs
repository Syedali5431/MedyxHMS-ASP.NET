using MedyxHMS.DTOs;
using System.ComponentModel.DataAnnotations;

// Purpose: Contains application code for BillingViewModels and its related runtime behavior.
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
        public string ActiveGateway { get; set; } = string.Empty;
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

    public class GatewayInfo
    {
        public string Key { get; set; } = string.Empty;          // e.g. "paypal"
        public string DisplayName { get; set; } = string.Empty;  // e.g. "PayPal"
        public string Logo { get; set; } = string.Empty;         // e.g. "paypal.png"
        public string Website { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;       // e.g. "Global", "Pakistan"
    }

    public class PaymentGatewaySettingsViewModel
    {
        public string ActiveGateway { get; set; } = "none";

        // Per-gateway credential fields
        public string PayPalClientId { get; set; } = string.Empty;
        public string PayPalClientSecret { get; set; } = string.Empty;
        public bool PayPalTestMode { get; set; } = true;

        public string StripePublishableKey { get; set; } = string.Empty;
        public string StripeSecretKey { get; set; } = string.Empty;
        public bool StripeTestMode { get; set; } = true;

        public string PayUMerchantKey { get; set; } = string.Empty;
        public string PayUSalt { get; set; } = string.Empty;

        public string CCAvenueMerchantId { get; set; } = string.Empty;
        public string CCAvenueAccessCode { get; set; } = string.Empty;
        public string CCAvenueWorkingKey { get; set; } = string.Empty;

        public string InstamojoApiKey { get; set; } = string.Empty;
        public string InstamojoAuthToken { get; set; } = string.Empty;
        public bool InstamojoTestMode { get; set; } = true;

        public string PaystackPublicKey { get; set; } = string.Empty;
        public string PaystackSecretKey { get; set; } = string.Empty;

        public string RazorpayKeyId { get; set; } = string.Empty;
        public string RazorpayKeySecret { get; set; } = string.Empty;
        public bool RazorpayTestMode { get; set; } = true;

        public string PaytmMerchantId { get; set; } = string.Empty;
        public string PaytmMerchantKey { get; set; } = string.Empty;
        public bool PaytmTestMode { get; set; } = true;

        public string MidtransClientKey { get; set; } = string.Empty;
        public string MidtransServerKey { get; set; } = string.Empty;
        public bool MidtransTestMode { get; set; } = true;

        public string PesapalConsumerKey { get; set; } = string.Empty;
        public string PesapalConsumerSecret { get; set; } = string.Empty;
        public bool PesapalTestMode { get; set; } = true;

        public string FlutterwavePublicKey { get; set; } = string.Empty;
        public string FlutterwaveSecretKey { get; set; } = string.Empty;
        public bool FlutterwaveTestMode { get; set; } = true;

        public string IPayAfricaMerchantId { get; set; } = string.Empty;
        public string IPayAfricaHashKey { get; set; } = string.Empty;

        public string JazzCashMerchantId { get; set; } = string.Empty;
        public string JazzCashPassword { get; set; } = string.Empty;
        public bool JazzCashTestMode { get; set; } = true;

        public string BillplzCollectionId { get; set; } = string.Empty;
        public string BillplzApiKey { get; set; } = string.Empty;

        public string SSLCommerzStoreId { get; set; } = string.Empty;
        public string SSLCommerzStorePassword { get; set; } = string.Empty;
        public bool SSLCommerzTestMode { get; set; } = true;

        public string WalkingmClientId { get; set; } = string.Empty;
        public string WalkingmClientSecret { get; set; } = string.Empty;

        public string EasyPaisaMerchantId { get; set; } = string.Empty;
        public string EasyPaisaHashKey { get; set; } = string.Empty;
        public bool EasyPaisaTestMode { get; set; } = true;

        // Secret-preservation flags (don't overwrite with empty on save)
        public bool HasSavedPayPalClientSecret { get; set; }
        public bool HasSavedStripeSecretKey { get; set; }
        public bool HasSavedPayUSalt { get; set; }
        public bool HasSavedCCAvenueWorkingKey { get; set; }
        public bool HasSavedInstamojoAuthToken { get; set; }
        public bool HasSavedPaystackSecretKey { get; set; }
        public bool HasSavedRazorpayKeySecret { get; set; }
        public bool HasSavedPaytmMerchantKey { get; set; }
        public bool HasSavedMidtransServerKey { get; set; }
        public bool HasSavedPesapalConsumerSecret { get; set; }
        public bool HasSavedFlutterwaveSecretKey { get; set; }
        public bool HasSavedIPayAfricaHashKey { get; set; }
        public bool HasSavedJazzCashPassword { get; set; }
        public bool HasSavedBillplzApiKey { get; set; }
        public bool HasSavedSSLCommerzStorePassword { get; set; }
        public bool HasSavedWalkingmClientSecret { get; set; }
        public bool HasSavedEasyPaisaHashKey { get; set; }

        public static readonly List<GatewayInfo> AllGateways = new()
        {
            new() { Key = "paypal",      DisplayName = "PayPal",       Logo = "paypal.png",      Website = "https://www.paypal.com",              Region = "Global" },
            new() { Key = "stripe",      DisplayName = "Stripe",       Logo = "stripe.png",      Website = "https://stripe.com",                  Region = "Global" },
            new() { Key = "payu",        DisplayName = "PayU",         Logo = "payu.png",         Website = "https://www.payu.in",                 Region = "India" },
            new() { Key = "ccavenue",    DisplayName = "CCAvenue",     Logo = "ccavenue.png",    Website = "https://www.ccavenue.com",            Region = "India" },
            new() { Key = "instamojo",   DisplayName = "Instamojo",    Logo = "instamojo.png",   Website = "https://www.instamojo.com",           Region = "India" },
            new() { Key = "paystack",    DisplayName = "Paystack",     Logo = "paystack.png",    Website = "https://paystack.com",                Region = "Africa" },
            new() { Key = "razorpay",    DisplayName = "Razorpay",     Logo = "razorpay.jpg",    Website = "https://razorpay.com",                Region = "India" },
            new() { Key = "paytm",       DisplayName = "Paytm",        Logo = "paytm.jpg",       Website = "https://www.paytm.com",               Region = "India" },
            new() { Key = "midtrans",    DisplayName = "Midtrans",     Logo = "midtrans.jpg",    Website = "https://midtrans.com",                Region = "Indonesia" },
            new() { Key = "pesapal",     DisplayName = "Pesapal",      Logo = "pesapal.jpg",     Website = "https://pesapal.com",                 Region = "Africa" },
            new() { Key = "flutterwave", DisplayName = "Flutterwave",  Logo = "flutterwave.png", Website = "https://flutterwave.com",             Region = "Africa" },
            new() { Key = "ipayafrica",  DisplayName = "iPay Africa",  Logo = "ipayafrica.png",  Website = "https://www.ipayafrica.com",          Region = "Africa" },
            new() { Key = "jazzcash",    DisplayName = "JazzCash",     Logo = "jazzcash.jpg",    Website = "https://www.jazzcash.com.pk",         Region = "Pakistan" },
            new() { Key = "billplz",     DisplayName = "Billplz",      Logo = "billplz.jpg",     Website = "https://www.billplz.com",             Region = "Malaysia" },
            new() { Key = "sslcommerz",  DisplayName = "SSLCommerz",   Logo = "sslcommerz.png",  Website = "https://www.sslcommerz.com",          Region = "Bangladesh" },
            new() { Key = "walkingm",    DisplayName = "Walkingm",     Logo = "walkingm.png",    Website = "https://walkingm.com",                Region = "Liberia" },
            new() { Key = "easypaisa",   DisplayName = "EasyPaisa",    Logo = "easypaisa.png",   Website = "https://www.easypaisa.com.pk",        Region = "Pakistan" },
        };
    }

    public class CheckoutViewModel
    {
        public int BillId { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string PatientName { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public string Gateway { get; set; } = string.Empty;
        public string GatewayKey { get; set; } = string.Empty;    // public/publishable key
        public string ClientToken { get; set; } = string.Empty;   // order token / snap token
        public string ReturnUrl { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
    }
}
