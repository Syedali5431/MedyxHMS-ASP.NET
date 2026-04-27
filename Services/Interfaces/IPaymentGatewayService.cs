using MedyxHMS.Models;

namespace MedyxHMS.Services.Interfaces
{
    public interface IPaymentGatewayService
    {
        /// <summary>
        /// Initiates a checkout session and returns the redirect URL (or form HTML for POST-based gateways).
        /// </summary>
        Task<PaymentGatewayResult> InitiateAsync(PaymentGatewayRequest request);

        /// <summary>
        /// Verifies and processes a callback/webhook from the gateway.
        /// Returns the completed Payment record on success.
        /// </summary>
        Task<PaymentGatewayCallbackResult> HandleCallbackAsync(string gateway, IQueryCollection query, IFormCollection form);
    }

    public class PaymentGatewayRequest
    {
        public int BillId { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string PatientName { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public string PatientPhone { get; set; } = string.Empty;
        public string Gateway { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }

    public class PaymentGatewayResult
    {
        public bool Success { get; set; }
        public string? RedirectUrl { get; set; }
        public string? PostFormHtml { get; set; }
        public string? Error { get; set; }
        public string? OrderId { get; set; }
        // For JS-based gateways (Stripe, Razorpay) — client-side key/token
        public string? ClientKey { get; set; }
        public string? ClientToken { get; set; }
        public string? GatewayKey { get; set; } // publishable/public key for JS SDK
    }

    public class PaymentGatewayCallbackResult
    {
        public bool Success { get; set; }
        public int BillId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Gateway { get; set; } = string.Empty;
        public string? Error { get; set; }
    }
}
