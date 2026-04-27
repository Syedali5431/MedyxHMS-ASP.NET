using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public class PaymentGatewayService : IPaymentGatewayService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PaymentGatewayService> _logger;

        public PaymentGatewayService(
            ApplicationDbContext db,
            IHttpClientFactory httpClientFactory,
            ILogger<PaymentGatewayService> logger)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // ── helpers ─────────────────────────────────────────────────────────────
        private async Task<string?> Cfg(string key)
        {
            var s = await _db.Settings.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key);
            return s?.Value;
        }

        private static string Hmac256Hex(string secret, string data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            using var hmac = new HMACSHA256(keyBytes);
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
        }

        private static string Md5Hex(string input)
        {
            var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToUpperInvariant();
        }

        // ════════════════════════════════════════════════════════════════════════
        // INITIATE
        // ════════════════════════════════════════════════════════════════════════
        public async Task<PaymentGatewayResult> InitiateAsync(PaymentGatewayRequest req)
        {
            try
            {
                return req.Gateway.ToLowerInvariant() switch
                {
                    "paypal"      => await InitiatePayPal(req),
                    "stripe"      => await InitiateStripe(req),
                    "razorpay"    => await InitiateRazorpay(req),
                    "paytm"       => await InitiatePaytm(req),
                    "payu"        => InitiatePayU(req),
                    "ccavenue"    => await InitiateCCAvenue(req),
                    "instamojo"   => await InitiateInstamojo(req),
                    "paystack"    => InitiatePaystack(req),
                    "midtrans"    => await InitiateMidtrans(req),
                    "pesapal"     => await InitiatePesapal(req),
                    "flutterwave" => InitiateFlutterwave(req),
                    "ipayafrica"  => InitiateIPayAfrica(req),
                    "jazzcash"    => await InitiateJazzCash(req),
                    "billplz"     => await InitiateBillplz(req),
                    "sslcommerz"  => await InitiateSSLCommerz(req),
                    "walkingm"    => InitiateWalkingm(req),
                    "easypaisa"   => await InitiateEasyPaisa(req),
                    _ => new PaymentGatewayResult { Success = false, Error = $"Gateway '{req.Gateway}' is not supported." }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment gateway initiation failed for {Gateway}", req.Gateway);
                return new PaymentGatewayResult { Success = false, Error = "Payment initiation failed. Please try again." };
            }
        }

        // ── PayPal ──────────────────────────────────────────────────────────────
        private async Task<PaymentGatewayResult> InitiatePayPal(PaymentGatewayRequest req)
        {
            var clientId = await Cfg("Payment:PayPal:ClientId") ?? string.Empty;
            var secret   = await Cfg("Payment:PayPal:ClientSecret") ?? string.Empty;
            var testMode = (await Cfg("Payment:PayPal:TestMode")) != "false";
            var baseUrl  = testMode ? "https://api-m.sandbox.paypal.com" : "https://api-m.paypal.com";

            var http = _httpClientFactory.CreateClient();
            // Get access token
            var tokenReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/oauth2/token");
            tokenReq.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}")));
            tokenReq.Content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("grant_type", "client_credentials") });
            var tokenRes = await http.SendAsync(tokenReq);
            if (!tokenRes.IsSuccessStatusCode)
                return new PaymentGatewayResult { Success = false, Error = "PayPal authentication failed." };

            var tokenJson = await tokenRes.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = tokenJson.GetProperty("access_token").GetString() ?? string.Empty;

            // Create order
            var orderPayload = new
            {
                intent = "CAPTURE",
                purchase_units = new[] { new {
                    reference_id = req.BillId.ToString(),
                    amount = new { currency_code = req.Currency, value = req.Amount.ToString("0.00") },
                    description = $"Bill #{req.BillNumber}"
                }},
                application_context = new {
                    return_url = req.ReturnUrl,
                    cancel_url = req.CancelUrl
                }
            };
            var orderReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/checkout/orders");
            orderReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            orderReq.Content = new StringContent(JsonSerializer.Serialize(orderPayload), Encoding.UTF8, "application/json");
            var orderRes = await http.SendAsync(orderReq);
            if (!orderRes.IsSuccessStatusCode)
                return new PaymentGatewayResult { Success = false, Error = "PayPal order creation failed." };

            var orderJson = await orderRes.Content.ReadFromJsonAsync<JsonElement>();
            var orderId   = orderJson.GetProperty("id").GetString();
            var approveLink = orderJson.GetProperty("links").EnumerateArray()
                .FirstOrDefault(l => l.TryGetProperty("rel", out var rel) && rel.GetString() == "approve");
            if (approveLink.ValueKind == JsonValueKind.Undefined)
                return new PaymentGatewayResult { Success = false, Error = "PayPal approve URL not found." };
            var approveUrl = approveLink.GetProperty("href").GetString();

            return new PaymentGatewayResult { Success = true, RedirectUrl = approveUrl, OrderId = orderId };
        }

        // ── Stripe ──────────────────────────────────────────────────────────────
        private async Task<PaymentGatewayResult> InitiateStripe(PaymentGatewayRequest req)
        {
            var secretKey    = await Cfg("Payment:Stripe:SecretKey") ?? string.Empty;
            var publishable  = await Cfg("Payment:Stripe:PublishableKey") ?? string.Empty;

            var http = _httpClientFactory.CreateClient();
            var sessionReq = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/checkout/sessions");
            sessionReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
            var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["payment_method_types[]"] = "card",
                ["mode"] = "payment",
                ["line_items[0][price_data][currency]"] = req.Currency.ToLowerInvariant(),
                ["line_items[0][price_data][unit_amount]"] = ((long)(req.Amount * 100)).ToString(),
                ["line_items[0][price_data][product_data][name]"] = $"Bill #{req.BillNumber}",
                ["line_items[0][quantity]"] = "1",
                ["metadata[bill_id]"] = req.BillId.ToString(),
                ["success_url"] = req.ReturnUrl + "?session_id={CHECKOUT_SESSION_ID}",
                ["cancel_url"] = req.CancelUrl,
                ["customer_email"] = req.PatientEmail
            });
            sessionReq.Content = body;
            var sessionRes = await http.SendAsync(sessionReq);
            if (!sessionRes.IsSuccessStatusCode)
                return new PaymentGatewayResult { Success = false, Error = "Stripe session creation failed." };

            var json = await sessionRes.Content.ReadFromJsonAsync<JsonElement>();
            var url  = json.GetProperty("url").GetString();
            return new PaymentGatewayResult { Success = true, RedirectUrl = url, GatewayKey = publishable };
        }

        // ── Razorpay ─────────────────────────────────────────────────────────────
        private async Task<PaymentGatewayResult> InitiateRazorpay(PaymentGatewayRequest req)
        {
            var keyId     = await Cfg("Payment:Razorpay:KeyId") ?? string.Empty;
            var keySecret = await Cfg("Payment:Razorpay:KeySecret") ?? string.Empty;

            var http = _httpClientFactory.CreateClient();
            var orderReq = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders");
            orderReq.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}")));
            var payload = new { amount = (long)(req.Amount * 100), currency = req.Currency, receipt = req.BillId.ToString() };
            orderReq.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var orderRes = await http.SendAsync(orderReq);
            if (!orderRes.IsSuccessStatusCode)
                return new PaymentGatewayResult { Success = false, Error = "Razorpay order creation failed." };

            var json    = await orderRes.Content.ReadFromJsonAsync<JsonElement>();
            var orderId = json.GetProperty("id").GetString();
            return new PaymentGatewayResult { Success = true, ClientToken = orderId, GatewayKey = keyId, OrderId = orderId };
        }

        // ── Paytm ─────────────────────────────────────────────────────────────────
        private async Task<PaymentGatewayResult> InitiatePaytm(PaymentGatewayRequest req)
        {
            var merchantId  = await Cfg("Payment:Paytm:MerchantId") ?? string.Empty;
            var merchantKey = await Cfg("Payment:Paytm:MerchantKey") ?? string.Empty;
            var testMode    = (await Cfg("Payment:Paytm:TestMode")) != "false";
            var baseUrl     = testMode
                ? "https://securegw-stage.paytm.in/theia/api/v1/initiateTransaction"
                : "https://securegw.paytm.in/theia/api/v1/initiateTransaction";

            var txnToken = Guid.NewGuid().ToString("N");
            var orderId  = $"BILL-{req.BillId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var payload  = new
            {
                body = new {
                    requestType   = "Payment",
                    mid           = merchantId,
                    websiteName   = testMode ? "WEBSTAGING" : "WEBPROD",
                    orderId,
                    callbackUrl   = req.CallbackUrl,
                    txnAmount     = new { value = req.Amount.ToString("0.00"), currency = req.Currency },
                    userInfo      = new { custId = req.BillId.ToString() }
                }
            };
            var http = _httpClientFactory.CreateClient();
            var r = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}?mid={merchantId}&orderId={orderId}");
            r.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await http.SendAsync(r);
            if (!res.IsSuccessStatusCode)
                return new PaymentGatewayResult { Success = false, Error = "Paytm initiation failed." };

            var json  = await res.Content.ReadFromJsonAsync<JsonElement>();
            var token = json.GetProperty("body").GetProperty("txnToken").GetString();
            var host  = testMode ? "https://securegw-stage.paytm.in" : "https://securegw.paytm.in";
            var redirectUrl = $"{host}/theia/api/v1/showPaymentPage?mid={merchantId}&orderId={orderId}&txnToken={token}";
            return new PaymentGatewayResult { Success = true, RedirectUrl = redirectUrl, OrderId = orderId };
        }

        // ── PayU ──────────────────────────────────────────────────────────────────
        private PaymentGatewayResult InitiatePayU(PaymentGatewayRequest req)
        {
            // PayU uses a POST form redirect — return form HTML
            var key  = _db.Settings.AsNoTracking().FirstOrDefault(x => x.Key == "Payment:PayU:MerchantKey")?.Value ?? string.Empty;
            var salt = _db.Settings.AsNoTracking().FirstOrDefault(x => x.Key == "Payment:PayU:Salt")?.Value ?? string.Empty;
            var txnId = $"BILL{req.BillId}{DateTime.UtcNow:yyyyMMddHHmmss}";
            var productInfo = $"Bill #{req.BillNumber}";
            var hash = Sha512Hex($"{key}|{txnId}|{req.Amount:0.00}|{productInfo}|{req.PatientName}|{req.PatientEmail}|||||||||||{salt}");
            var url = "https://secure.payu.in/_payment";
            var html = $@"<form id='payuForm' action='{url}' method='POST'>
<input type='hidden' name='key' value='{key}'/>
<input type='hidden' name='txnid' value='{txnId}'/>
<input type='hidden' name='productinfo' value='{productInfo}'/>
<input type='hidden' name='amount' value='{req.Amount:0.00}'/>
<input type='hidden' name='email' value='{req.PatientEmail}'/>
<input type='hidden' name='firstname' value='{req.PatientName}'/>
<input type='hidden' name='surl' value='{req.ReturnUrl}'/>
<input type='hidden' name='furl' value='{req.CancelUrl}'/>
<input type='hidden' name='hash' value='{hash}'/>
</form><script>document.getElementById('payuForm').submit();</script>";
            return new PaymentGatewayResult { Success = true, PostFormHtml = html };
        }

        // ── CCAvenue ──────────────────────────────────────────────────────────────
        private async Task<PaymentGatewayResult> InitiateCCAvenue(PaymentGatewayRequest req)
        {
            var merchantId  = await Cfg("Payment:CCAvenue:MerchantId") ?? string.Empty;
            var accessCode  = await Cfg("Payment:CCAvenue:AccessCode") ?? string.Empty;
            var workingKey  = await Cfg("Payment:CCAvenue:WorkingKey") ?? string.Empty;
            var orderId     = $"BILL{req.BillId}{DateTime.UtcNow:yyyyMMddHHmmss}";
            var plain = $"merchant_id={merchantId}&order_id={orderId}&currency={req.Currency}&amount={req.Amount:0.00}&redirect_url={req.ReturnUrl}&cancel_url={req.CancelUrl}&billing_name={req.PatientName}&billing_email={req.PatientEmail}";
            var encrypted = AesEncryptCCAvenue(plain, workingKey);
            var url  = $"https://secure.ccavenue.com/transaction/transaction.do?command=initiateTransaction&merchant_id={merchantId}&encRequest={HttpUtility.UrlEncode(encrypted)}&access_code={accessCode}";
            return new PaymentGatewayResult { Success = true, RedirectUrl = url, OrderId = orderId };
        }

        // ── Instamojo ─────────────────────────────────────────────────────────────
        private async Task<PaymentGatewayResult> InitiateInstamojo(PaymentGatewayRequest req)
        {
            var apiKey    = await Cfg("Payment:Instamojo:ApiKey") ?? string.Empty;
            var authToken = await Cfg("Payment:Instamojo:AuthToken") ?? string.Empty;
            var testMode  = (await Cfg("Payment:Instamojo:TestMode")) != "false";
            var baseUrl   = testMode ? "https://test.instamojo.com/api/1.1" : "https://www.instamojo.com/api/1.1";
            var http = _httpClientFactory.CreateClient();
            var r = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/payment-requests/");
            r.Headers.Add("X-Api-Key", apiKey);
            r.Headers.Add("X-Auth-Token", authToken);
            r.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["purpose"] = $"Bill #{req.BillNumber}",
                ["amount"]  = req.Amount.ToString("0.00"),
                ["email"]   = req.PatientEmail,
                ["phone"]   = req.PatientPhone,
                ["redirect_url"] = req.ReturnUrl,
                ["allow_repeated_payments"] = "false"
            });
            var res = await http.SendAsync(r);
            if (!res.IsSuccessStatusCode)
                return new PaymentGatewayResult { Success = false, Error = "Instamojo request creation failed." };
            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            var url  = json.GetProperty("payment_request").GetProperty("longurl").GetString();
            return new PaymentGatewayResult { Success = true, RedirectUrl = url };
        }

        // ── Paystack ──────────────────────────────────────────────────────────────
        private PaymentGatewayResult InitiatePaystack(PaymentGatewayRequest req)
        {
            var pubKey = _db.Settings.AsNoTracking().FirstOrDefault(x => x.Key == "Payment:Paystack:PublicKey")?.Value ?? string.Empty;
            // Paystack uses inline JS — return client key and amount for Checkout view
            return new PaymentGatewayResult { Success = true, GatewayKey = pubKey, ClientToken = ((long)(req.Amount * 100)).ToString() };
        }

        // ── Midtrans ──────────────────────────────────────────────────────────────
        private async Task<PaymentGatewayResult> InitiateMidtrans(PaymentGatewayRequest req)
        {
            var serverKey = await Cfg("Payment:Midtrans:ServerKey") ?? string.Empty;
            var clientKey = await Cfg("Payment:Midtrans:ClientKey") ?? string.Empty;
            var testMode  = (await Cfg("Payment:Midtrans:TestMode")) != "false";
            var baseUrl   = testMode ? "https://app.sandbox.midtrans.com" : "https://app.midtrans.com";
            var http = _httpClientFactory.CreateClient();
            var r = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/snap/v1/transactions");
            r.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{serverKey}:")));
            var payload = new
            {
                transaction_details = new { order_id = $"BILL{req.BillId}", gross_amount = (long)req.Amount },
                customer_details    = new { first_name = req.PatientName, email = req.PatientEmail, phone = req.PatientPhone }
            };
            r.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await http.SendAsync(r);
            if (!res.IsSuccessStatusCode)
                return new PaymentGatewayResult { Success = false, Error = "Midtrans snap token creation failed." };
            var json  = await res.Content.ReadFromJsonAsync<JsonElement>();
            var token = json.GetProperty("token").GetString();
            return new PaymentGatewayResult { Success = true, ClientToken = token, GatewayKey = clientKey };
        }

        // ── Pesapal ──────────────────────────────────────────────────────────────
        private async Task<PaymentGatewayResult> InitiatePesapal(PaymentGatewayRequest req)
        {
            var consumerKey    = await Cfg("Payment:Pesapal:ConsumerKey") ?? string.Empty;
            var consumerSecret = await Cfg("Payment:Pesapal:ConsumerSecret") ?? string.Empty;
            var testMode       = (await Cfg("Payment:Pesapal:TestMode")) != "false";
            var baseUrl        = testMode ? "https://cybqa.pesapal.com/pesapalv3" : "https://pay.pesapal.com/v3";
            var http = _httpClientFactory.CreateClient();
            // Get token
            var tokenReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/Auth/RequestToken");
            tokenReq.Content = new StringContent(JsonSerializer.Serialize(new { consumer_key = consumerKey, consumer_secret = consumerSecret }), Encoding.UTF8, "application/json");
            var tokenRes = await http.SendAsync(tokenReq);
            if (!tokenRes.IsSuccessStatusCode)
                return new PaymentGatewayResult { Success = false, Error = "Pesapal authentication failed." };
            var tokenJson  = await tokenRes.Content.ReadFromJsonAsync<JsonElement>();
            var token      = tokenJson.GetProperty("token").GetString();
            // Submit order
            var orderReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/Transactions/SubmitOrderRequest");
            orderReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var orderPayload = new
            {
                id                = $"BILL{req.BillId}",
                currency          = req.Currency,
                amount            = req.Amount,
                description       = $"Bill #{req.BillNumber}",
                callback_url      = req.ReturnUrl,
                notification_id   = string.Empty,
                billing_address   = new { email_address = req.PatientEmail, phone_number = req.PatientPhone, first_name = req.PatientName }
            };
            orderReq.Content = new StringContent(JsonSerializer.Serialize(orderPayload), Encoding.UTF8, "application/json");
            var orderRes = await http.SendAsync(orderReq);
            if (!orderRes.IsSuccessStatusCode)
                return new PaymentGatewayResult { Success = false, Error = "Pesapal order submission failed." };
            var orderJson = await orderRes.Content.ReadFromJsonAsync<JsonElement>();
            var redirectUrl = orderJson.GetProperty("redirect_url").GetString();
            return new PaymentGatewayResult { Success = true, RedirectUrl = redirectUrl };
        }

        // ── Flutterwave ───────────────────────────────────────────────────────────
        private PaymentGatewayResult InitiateFlutterwave(PaymentGatewayRequest req)
        {
            var pubKey = _db.Settings.AsNoTracking().FirstOrDefault(x => x.Key == "Payment:Flutterwave:PublicKey")?.Value ?? string.Empty;
            return new PaymentGatewayResult { Success = true, GatewayKey = pubKey, ClientToken = req.BillId.ToString() };
        }

        // ── iPay Africa ───────────────────────────────────────────────────────────
        private PaymentGatewayResult InitiateIPayAfrica(PaymentGatewayRequest req)
        {
            var merchantId = _db.Settings.AsNoTracking().FirstOrDefault(x => x.Key == "Payment:IPayAfrica:MerchantId")?.Value ?? string.Empty;
            var hashKey    = _db.Settings.AsNoTracking().FirstOrDefault(x => x.Key == "Payment:IPayAfrica:HashKey")?.Value ?? string.Empty;
            var orderId    = $"BILL{req.BillId}{DateTime.UtcNow:yyyyMMddHHmmss}";
            var hash       = Hmac256Hex(hashKey, $"{merchantId}{orderId}{req.Amount:0.00}{req.Currency}");
            var url = $"https://www.ipayafrica.com/ipn/?vendor={merchantId}&oid={orderId}&inv={req.BillNumber}&ttl={req.Amount:0.00}&curr={req.Currency}&p1=bill&p2=&p3=&p4=&cbk={HttpUtility.UrlEncode(req.ReturnUrl)}&cst=0&crl=0&hsh={hash}";
            return new PaymentGatewayResult { Success = true, RedirectUrl = url, OrderId = orderId };
        }

        // ── JazzCash ──────────────────────────────────────────────────────────────
        private async Task<PaymentGatewayResult> InitiateJazzCash(PaymentGatewayRequest req)
        {
            var merchantId  = await Cfg("Payment:JazzCash:MerchantId") ?? string.Empty;
            var password    = await Cfg("Payment:JazzCash:Password") ?? string.Empty;
            var testMode    = (await Cfg("Payment:JazzCash:TestMode")) != "false";
            var baseUrl     = testMode ? "https://sandbox.jazzcash.com.pk/CustomerPortal/transactionmanagement/merchantform/"
                                       : "https://payments.jazzcash.com.pk/CustomerPortal/transactionmanagement/merchantform/";
            var txnDateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            var txnExpiry   = DateTime.Now.AddHours(1).ToString("yyyyMMddHHmmss");
            var txnRefNo    = $"T{txnDateTime}";
            var amount      = ((long)(req.Amount * 100)).ToString();
            var hashStr     = $"{password}&{merchantId}&{txnRefNo}&{txnDateTime}&{txnExpiry}&{amount}&{req.Currency}&C&{req.ReturnUrl}";
            var hash        = Hmac256Hex(password, hashStr);
            var html = $@"<form id='jcForm' action='{baseUrl}' method='POST'>
<input type='hidden' name='pp_Version' value='1.1'/>
<input type='hidden' name='pp_TxnType' value='MWALLET'/>
<input type='hidden' name='pp_Language' value='EN'/>
<input type='hidden' name='pp_MerchantID' value='{merchantId}'/>
<input type='hidden' name='pp_SubMerchantID' value=''/>
<input type='hidden' name='pp_Password' value='{password}'/>
<input type='hidden' name='pp_BankID' value='TBANK'/>
<input type='hidden' name='pp_ProductID' value='RETL'/>
<input type='hidden' name='pp_TxnRefNo' value='{txnRefNo}'/>
<input type='hidden' name='pp_Amount' value='{amount}'/>
<input type='hidden' name='pp_TxnCurrency' value='{req.Currency}'/>
<input type='hidden' name='pp_TxnDateTime' value='{txnDateTime}'/>
<input type='hidden' name='pp_BillReference' value='BILL{req.BillId}'/>
<input type='hidden' name='pp_Description' value='Bill {req.BillNumber}'/>
<input type='hidden' name='pp_TxnExpiryDateTime' value='{txnExpiry}'/>
<input type='hidden' name='pp_ReturnURL' value='{req.ReturnUrl}'/>
<input type='hidden' name='pp_SecureHash' value='{hash}'/>
</form><script>document.getElementById('jcForm').submit();</script>";
            return new PaymentGatewayResult { Success = true, PostFormHtml = html };
        }

        // ── Billplz ──────────────────────────────────────────────────────────────
        private async Task<PaymentGatewayResult> InitiateBillplz(PaymentGatewayRequest req)
        {
            var collectionId = await Cfg("Payment:Billplz:CollectionId") ?? string.Empty;
            var apiKey       = await Cfg("Payment:Billplz:ApiKey") ?? string.Empty;
            var http = _httpClientFactory.CreateClient();
            var r = new HttpRequestMessage(HttpMethod.Post, "https://www.billplz.com/api/v3/bills");
            r.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:")));
            r.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["collection_id"]  = collectionId,
                ["email"]          = req.PatientEmail,
                ["name"]           = req.PatientName,
                ["amount"]         = ((long)(req.Amount * 100)).ToString(),
                ["description"]    = $"Bill #{req.BillNumber}",
                ["callback_url"]   = req.CallbackUrl,
                ["redirect_url"]   = req.ReturnUrl
            });
            var res = await http.SendAsync(r);
            if (!res.IsSuccessStatusCode)
                return new PaymentGatewayResult { Success = false, Error = "Billplz bill creation failed." };
            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            var url  = json.GetProperty("url").GetString();
            return new PaymentGatewayResult { Success = true, RedirectUrl = url };
        }

        // ── SSLCommerz ───────────────────────────────────────────────────────────
        private async Task<PaymentGatewayResult> InitiateSSLCommerz(PaymentGatewayRequest req)
        {
            var storeId  = await Cfg("Payment:SSLCommerz:StoreId") ?? string.Empty;
            var storePass = await Cfg("Payment:SSLCommerz:StorePassword") ?? string.Empty;
            var testMode = (await Cfg("Payment:SSLCommerz:TestMode")) != "false";
            var baseUrl  = testMode ? "https://sandbox.sslcommerz.com" : "https://securepay.sslcommerz.com";
            var http = _httpClientFactory.CreateClient();
            var r = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/gwprocess/v4/api.php");
            r.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["store_id"]         = storeId,
                ["store_passwd"]     = storePass,
                ["total_amount"]     = req.Amount.ToString("0.00"),
                ["currency"]         = req.Currency,
                ["tran_id"]          = $"BILL{req.BillId}{DateTime.UtcNow:yyyyMMddHHmmss}",
                ["success_url"]      = req.ReturnUrl,
                ["fail_url"]         = req.CancelUrl,
                ["cancel_url"]       = req.CancelUrl,
                ["ipn_url"]          = req.CallbackUrl,
                ["cus_name"]         = req.PatientName,
                ["cus_email"]        = req.PatientEmail,
                ["cus_phone"]        = req.PatientPhone,
                ["product_name"]     = $"Bill #{req.BillNumber}",
                ["product_category"] = "Medical",
                ["product_profile"]  = "general"
            });
            var res = await http.SendAsync(r);
            if (!res.IsSuccessStatusCode)
                return new PaymentGatewayResult { Success = false, Error = "SSLCommerz initiation failed." };
            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            var url  = json.GetProperty("GatewayPageURL").GetString();
            return new PaymentGatewayResult { Success = true, RedirectUrl = url };
        }

        // ── Walkingm ─────────────────────────────────────────────────────────────
        private PaymentGatewayResult InitiateWalkingm(PaymentGatewayRequest req)
        {
            var clientId = _db.Settings.AsNoTracking().FirstOrDefault(x => x.Key == "Payment:Walkingm:ClientId")?.Value ?? string.Empty;
            var url = $"https://walkingm.com/pay?client_id={clientId}&amount={req.Amount:0.00}&currency={req.Currency}&ref=BILL{req.BillId}&return_url={HttpUtility.UrlEncode(req.ReturnUrl)}&cancel_url={HttpUtility.UrlEncode(req.CancelUrl)}";
            return new PaymentGatewayResult { Success = true, RedirectUrl = url };
        }

        // ── EasyPaisa ────────────────────────────────────────────────────────────
        private async Task<PaymentGatewayResult> InitiateEasyPaisa(PaymentGatewayRequest req)
        {
            var merchantId = await Cfg("Payment:EasyPaisa:MerchantId") ?? string.Empty;
            var hashKey    = await Cfg("Payment:EasyPaisa:HashKey") ?? string.Empty;
            var testMode   = (await Cfg("Payment:EasyPaisa:TestMode")) != "false";
            var baseUrl    = testMode ? "https://easypay.easypaisa.com.pk/tpg/js" : "https://easypay.easypaisa.com.pk/tpg/js";
            var orderId    = $"BILL{req.BillId}{DateTime.UtcNow:yyyyMMddHHmmss}";
            var expiry     = DateTime.Now.AddHours(1).ToString("yyyyMMddHHmmss");
            var hash       = Hmac256Hex(hashKey, $"{merchantId}|{orderId}|{req.Amount:0.00}|{req.Currency}|{expiry}");
            var url = $"{baseUrl}?storeId={merchantId}&orderId={orderId}&transactionAmount={req.Amount:0.00}&mobileAccountNo={req.PatientPhone}&emailAddress={req.PatientEmail}&transactionType=InitialRequest&tokenExpiry={expiry}&bankIdentificationNumber=&encryptedHashRequest={hash}&postBackURL={HttpUtility.UrlEncode(req.ReturnUrl)}";
            return new PaymentGatewayResult { Success = true, RedirectUrl = url, OrderId = orderId };
        }

        // ════════════════════════════════════════════════════════════════════════
        // CALLBACK HANDLING
        // ════════════════════════════════════════════════════════════════════════
        public async Task<PaymentGatewayCallbackResult> HandleCallbackAsync(string gateway, IQueryCollection query, IFormCollection form)
        {
            try
            {
                return gateway.ToLowerInvariant() switch
                {
                    "paypal"      => await CallbackPayPal(query),
                    "stripe"      => await CallbackStripe(query),
                    "razorpay"    => CallbackRazorpay(form),
                    "paytm"       => CallbackPaytm(form),
                    "payu"        => await CallbackPayU(form),
                    "ccavenue"    => await CallbackCCAvenue(form),
                    "instamojo"   => CallbackInstamojo(query),
                    "paystack"    => await CallbackPaystack(query),
                    "midtrans"    => CallbackMidtrans(form),
                    "pesapal"     => await CallbackPesapal(query),
                    "flutterwave" => await CallbackFlutterwave(query),
                    "ipayafrica"  => await CallbackIPayAfrica(form),
                    "jazzcash"    => await CallbackJazzCash(form),
                    "billplz"     => await CallbackBillplz(form),
                    "sslcommerz"  => await CallbackSSLCommerz(form),
                    "walkingm"    => CallbackWalkingm(query),
                    "easypaisa"   => CallbackEasyPaisa(query),
                    _ => Fail($"Unknown gateway: {gateway}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Callback handling failed for {Gateway}", gateway);
                return Fail("Callback processing error.");
            }
        }

        private static PaymentGatewayCallbackResult Fail(string error) =>
            new() { Success = false, Error = error };

        private static PaymentGatewayCallbackResult Ok(int billId, decimal amount, string txnId, string gateway) =>
            new() { Success = true, BillId = billId, Amount = amount, TransactionId = txnId, Gateway = gateway };

        private async Task<PaymentGatewayCallbackResult> CallbackPayPal(IQueryCollection q)
        {
            var token    = q["token"].ToString();
            var payerId  = q["PayerID"].ToString();
            var clientId = await Cfg("Payment:PayPal:ClientId") ?? string.Empty;
            var secret   = await Cfg("Payment:PayPal:ClientSecret") ?? string.Empty;
            var testMode = (await Cfg("Payment:PayPal:TestMode")) != "false";
            var baseUrl  = testMode ? "https://api-m.sandbox.paypal.com" : "https://api-m.paypal.com";
            var http = _httpClientFactory.CreateClient();
            var tokenReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/oauth2/token");
            tokenReq.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}")));
            tokenReq.Content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("grant_type", "client_credentials") });
            var tokenRes = await http.SendAsync(tokenReq);
            if (!tokenRes.IsSuccessStatusCode) return Fail("PayPal re-auth failed.");
            var tokenJson  = await tokenRes.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = tokenJson.GetProperty("access_token").GetString() ?? string.Empty;
            if (string.IsNullOrEmpty(accessToken)) return Fail("PayPal re-auth token missing.");
            var captureReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/checkout/orders/{token}/capture");
            captureReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            captureReq.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            var captureRes = await http.SendAsync(captureReq);
            if (!captureRes.IsSuccessStatusCode) return Fail("PayPal capture failed.");
            var captureJson = await captureRes.Content.ReadFromJsonAsync<JsonElement>();
            var purchaseUnits = captureJson.GetProperty("purchase_units");
            if (purchaseUnits.GetArrayLength() == 0) return Fail("PayPal capture response missing purchase_units.");
            var unit0 = purchaseUnits[0];
            var billId = int.TryParse(unit0.GetProperty("reference_id").GetString(), out var b) ? b : 0;
            var captures = unit0.GetProperty("payments").GetProperty("captures");
            if (captures.GetArrayLength() == 0) return Fail("PayPal capture response missing captures.");
            var capture0  = captures[0];
            var captureId = capture0.GetProperty("id").GetString() ?? string.Empty;
            var amount    = decimal.TryParse(capture0.GetProperty("amount").GetProperty("value").GetString(), out var a) ? a : 0m;
            return Ok(billId, amount, captureId, "paypal");
        }

        private async Task<PaymentGatewayCallbackResult> CallbackStripe(IQueryCollection q)
        {
            var sessionId = q["session_id"].ToString();
            var secretKey = await Cfg("Payment:Stripe:SecretKey") ?? string.Empty;
            var http = _httpClientFactory.CreateClient();
            var r = new HttpRequestMessage(HttpMethod.Get, $"https://api.stripe.com/v1/checkout/sessions/{sessionId}");
            r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
            var res = await http.SendAsync(r);
            if (!res.IsSuccessStatusCode) return Fail("Stripe session retrieval failed.");
            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            if (json.GetProperty("payment_status").GetString() != "paid") return Fail("Payment not completed.");
            var billId = int.Parse(json.GetProperty("metadata").GetProperty("bill_id").GetString() ?? "0");
            var amount = decimal.Parse(json.GetProperty("amount_total").GetDecimal().ToString()) / 100m;
            var paymentIntent = json.GetProperty("payment_intent").GetString() ?? string.Empty;
            return Ok(billId, amount, paymentIntent, "stripe");
        }

        private PaymentGatewayCallbackResult CallbackRazorpay(IFormCollection f)
        {
            var paymentId = f["razorpay_payment_id"].ToString();
            var orderId   = f["razorpay_order_id"].ToString();
            // order receipt is set as "BILL{id}" at creation time
            var receipt   = f["razorpay_order_receipt"].ToString();
            var billIdStr = !string.IsNullOrEmpty(receipt)
                ? receipt.Replace("BILL", "")
                : new string(orderId.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray());
            if (!int.TryParse(billIdStr, out var billId)) billId = 0;
            return Ok(billId, 0, paymentId, "razorpay");
        }

        private PaymentGatewayCallbackResult CallbackPaytm(IFormCollection f)
        {
            if (f["STATUS"].ToString() != "TXN_SUCCESS") return Fail("Paytm transaction not successful.");
            var txnId  = f["TXNID"].ToString();
            var amount = decimal.TryParse(f["TXNAMOUNT"].ToString(), out var a) ? a : 0m;
            var orderId = f["ORDERID"].ToString(); // BILL{id}-...
            var billId  = int.TryParse(orderId.Split('-').ElementAtOrDefault(1), out var b) ? b : 0;
            return Ok(billId, amount, txnId, "paytm");
        }

        private async Task<PaymentGatewayCallbackResult> CallbackPayU(IFormCollection f)
        {
            var status   = f["status"].ToString();
            if (status != "success") return Fail("PayU payment not successful.");
            var txnId    = f["mihpayid"].ToString();
            var amount   = decimal.TryParse(f["amount"].ToString(), out var a) ? a : 0m;
            var txnIdRef = f["txnid"].ToString(); // BILL{id}...
            var billId   = int.TryParse(new string(txnIdRef.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray()), out var b) ? b : 0;
            // Verify hash
            var salt     = await Cfg("Payment:PayU:Salt") ?? string.Empty;
            var key      = await Cfg("Payment:PayU:MerchantKey") ?? string.Empty;
            var retroHash = Sha512Hex($"{salt}|{status}|||||||||||{f["email"]}|{f["firstname"]}|{f["productinfo"]}|{amount:0.00}|{txnIdRef}|{key}");
            if (!string.Equals(retroHash, f["hash"].ToString(), StringComparison.OrdinalIgnoreCase))
                return Fail("PayU hash verification failed.");
            return Ok(billId, amount, txnId, "payu");
        }

        private async Task<PaymentGatewayCallbackResult> CallbackCCAvenue(IFormCollection f)
        {
            var workingKey = await Cfg("Payment:CCAvenue:WorkingKey") ?? string.Empty;
            var encrypted  = f["encResp"].ToString();
            var plain      = AesDecryptCCAvenue(encrypted, workingKey);
            var parts      = plain.Split('&').Select(x => x.Split('=')).Where(x => x.Length == 2).ToDictionary(x => x[0], x => HttpUtility.UrlDecode(x[1]));
            if (parts.GetValueOrDefault("order_status") != "Success") return Fail("CCAvenue payment not successful.");
            var txnId   = parts.GetValueOrDefault("tracking_id") ?? string.Empty;
            var amount  = decimal.TryParse(parts.GetValueOrDefault("amount"), out var a) ? a : 0m;
            var orderId = parts.GetValueOrDefault("order_id") ?? string.Empty;
            var billId  = int.TryParse(new string(orderId.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray()), out var b) ? b : 0;
            return Ok(billId, amount, txnId, "ccavenue");
        }

        private PaymentGatewayCallbackResult CallbackInstamojo(IQueryCollection q)
        {
            if (q["payment_status"].ToString() != "Credit") return Fail("Instamojo payment not successful.");
            var paymentId = q["payment_id"].ToString();
            var amount    = decimal.TryParse(q["amount"].ToString(), out var a) ? a : 0m;
            // payment_request_id not directly mapped to bill; use amount + payment_id
            return Ok(0, amount, paymentId, "instamojo");
        }

        private async Task<PaymentGatewayCallbackResult> CallbackPaystack(IQueryCollection q)
        {
            var reference = q["reference"].ToString();
            var secretKey = await Cfg("Payment:Paystack:SecretKey") ?? string.Empty;
            var http = _httpClientFactory.CreateClient();
            var r = new HttpRequestMessage(HttpMethod.Get, $"https://api.paystack.co/transaction/verify/{reference}");
            r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
            var res = await http.SendAsync(r);
            if (!res.IsSuccessStatusCode) return Fail("Paystack verification failed.");
            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            var data = json.GetProperty("data");
            if (data.GetProperty("status").GetString() != "success") return Fail("Paystack payment not successful.");
            var amount = data.GetProperty("amount").GetDecimal() / 100m;
            var txnId  = data.GetProperty("id").GetInt64().ToString();
            // Extract bill ID from reference (format: BILL{id})
            var billId = int.TryParse(reference.Replace("BILL", ""), out var b) ? b : 0;
            return Ok(billId, amount, txnId, "paystack");
        }

        private PaymentGatewayCallbackResult CallbackMidtrans(IFormCollection f)
        {
            var status   = f["transaction_status"].ToString();
            if (status != "settlement" && status != "capture") return Fail("Midtrans payment not complete.");
            var txnId    = f["transaction_id"].ToString();
            var amount   = decimal.TryParse(f["gross_amount"].ToString(), out var a) ? a : 0m;
            var orderId  = f["order_id"].ToString(); // BILL{id}
            var billId   = int.TryParse(new string(orderId.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray()), out var b) ? b : 0;
            return Ok(billId, amount, txnId, "midtrans");
        }

        private async Task<PaymentGatewayCallbackResult> CallbackPesapal(IQueryCollection q)
        {
            var trackingId = q["pesapal_transaction_tracking_id"].ToString();
            var merchantRef = q["pesapal_merchant_reference"].ToString(); // BILL{id}
            var consumerKey    = await Cfg("Payment:Pesapal:ConsumerKey") ?? string.Empty;
            var consumerSecret = await Cfg("Payment:Pesapal:ConsumerSecret") ?? string.Empty;
            var testMode       = (await Cfg("Payment:Pesapal:TestMode")) != "false";
            var baseUrl        = testMode ? "https://cybqa.pesapal.com/pesapalv3" : "https://pay.pesapal.com/v3";
            var http = _httpClientFactory.CreateClient();
            var tokenReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/Auth/RequestToken");
            tokenReq.Content = new StringContent(JsonSerializer.Serialize(new { consumer_key = consumerKey, consumer_secret = consumerSecret }), Encoding.UTF8, "application/json");
            var tokenRes = await http.SendAsync(tokenReq);
            if (!tokenRes.IsSuccessStatusCode) return Fail("Pesapal auth failed.");
            var tokenJson = await tokenRes.Content.ReadFromJsonAsync<JsonElement>();
            var token     = tokenJson.GetProperty("token").GetString();
            var statusReq = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/Transactions/GetTransactionStatus?orderTrackingId={trackingId}");
            statusReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var statusRes = await http.SendAsync(statusReq);
            if (!statusRes.IsSuccessStatusCode) return Fail("Pesapal status check failed.");
            var statusJson = await statusRes.Content.ReadFromJsonAsync<JsonElement>();
            if (statusJson.GetProperty("payment_status_description").GetString() != "Completed") return Fail("Pesapal payment not completed.");
            var billId = int.TryParse(new string(merchantRef.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray()), out var b) ? b : 0;
            var amount = statusJson.TryGetProperty("amount", out var amtEl) ? amtEl.GetDecimal() : 0m;
            return Ok(billId, amount, trackingId, "pesapal");
        }

        private async Task<PaymentGatewayCallbackResult> CallbackFlutterwave(IQueryCollection q)
        {
            var status = q["status"].ToString();
            if (status != "successful") return Fail("Flutterwave payment not successful.");
            var txRef     = q["tx_ref"].ToString();
            var txId      = q["transaction_id"].ToString();
            var secretKey = await Cfg("Payment:Flutterwave:SecretKey") ?? string.Empty;
            var http = _httpClientFactory.CreateClient();
            var r = new HttpRequestMessage(HttpMethod.Get, $"https://api.flutterwave.com/v3/transactions/{txId}/verify");
            r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
            var res = await http.SendAsync(r);
            if (!res.IsSuccessStatusCode) return Fail("Flutterwave verification failed.");
            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            var data = json.GetProperty("data");
            if (data.GetProperty("status").GetString() != "successful") return Fail("Flutterwave payment not verified.");
            var amount = data.GetProperty("amount").GetDecimal();
            var billId = int.TryParse(txRef.Replace("BILL", ""), out var b) ? b : 0;
            return Ok(billId, amount, txId, "flutterwave");
        }

        private async Task<PaymentGatewayCallbackResult> CallbackIPayAfrica(IFormCollection f)
        {
            var status    = f["status"].ToString();
            if (status != "aei7p7yrx4ae34" /* iPay success code */) return Fail("iPay Africa payment not successful.");
            var txnRef    = f["txncd"].ToString();
            var amount    = decimal.TryParse(f["mc"].ToString(), out var a) ? a : 0m;
            var orderId   = f["id"].ToString(); // BILL{id}...
            var billId    = int.TryParse(new string(orderId.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray()), out var b) ? b : 0;
            // Verify hash
            var merchantId = await Cfg("Payment:IPayAfrica:MerchantId") ?? string.Empty;
            var hashKey    = await Cfg("Payment:IPayAfrica:HashKey") ?? string.Empty;
            var expected   = Hmac256Hex(hashKey, $"{merchantId}{orderId}{amount:0.00}{f["curr"]}");
            if (!string.Equals(expected, f["hash"].ToString(), StringComparison.OrdinalIgnoreCase))
                return Fail("iPay Africa hash mismatch.");
            return Ok(billId, amount, txnRef, "ipayafrica");
        }

        private async Task<PaymentGatewayCallbackResult> CallbackJazzCash(IFormCollection f)
        {
            var responseCode = f["pp_ResponseCode"].ToString();
            if (responseCode != "000") return Fail($"JazzCash payment failed: {f["pp_ResponseMessage"]}");
            var txnId  = f["pp_TxnRefNo"].ToString();
            var amount = decimal.TryParse(f["pp_Amount"].ToString(), out var a) ? a / 100m : 0m;
            var billRef = f["pp_BillReference"].ToString(); // BILL{id}
            var billId  = int.TryParse(new string(billRef.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray()), out var b) ? b : 0;
            // Verify hash
            var password = await Cfg("Payment:JazzCash:Password") ?? string.Empty;
            var fields   = new[] { f["pp_Amount"].ToString(), f["pp_BankID"].ToString(), f["pp_BillReference"].ToString(),
                                   f["pp_Language"].ToString(), f["pp_MerchantID"].ToString(), f["pp_Password"].ToString(),
                                   f["pp_ProductID"].ToString(), f["pp_ResponseCode"].ToString(), f["pp_ResponseMessage"].ToString(),
                                   f["pp_ReturnURL"].ToString(), f["pp_SubMerchantID"].ToString(), f["pp_TxnCurrency"].ToString(),
                                   f["pp_TxnDateTime"].ToString(), f["pp_TxnExpiryDateTime"].ToString(), f["pp_TxnRefNo"].ToString(),
                                   f["pp_TxnType"].ToString(), f["pp_Version"].ToString() };
            var expected = Hmac256Hex(password, string.Join("&", fields.Where(x => !string.IsNullOrEmpty(x))));
            if (!string.Equals(expected, f["pp_SecureHash"].ToString(), StringComparison.OrdinalIgnoreCase))
                return Fail("JazzCash hash verification failed.");
            return Ok(billId, amount, txnId, "jazzcash");
        }

        private async Task<PaymentGatewayCallbackResult> CallbackBillplz(IFormCollection f)
        {
            var paid       = f["paid"].ToString().ToLowerInvariant() == "true";
            if (!paid) return Fail("Billplz bill not paid.");
            var billplzId  = f["id"].ToString();
            var amount     = decimal.TryParse(f["paid_amount"].ToString(), out var a) ? a / 100m : 0m;
            var apiKey     = await Cfg("Payment:Billplz:ApiKey") ?? string.Empty;
            // Verify x-signature
            var sig        = Hmac256Hex(apiKey, $"id={billplzId}&paid={f["paid"]}&paid_amount={f["paid_amount"]}&paid_at={f["paid_at"]}&payment_method={f["payment_method"]}&state={f["state"]}&transaction_id={f["transaction_id"]}&url={f["url"]}");
            if (!string.Equals(sig, f["x_signature"].ToString(), StringComparison.OrdinalIgnoreCase))
                return Fail("Billplz signature verification failed.");
            return Ok(0, amount, billplzId, "billplz");
        }

        private async Task<PaymentGatewayCallbackResult> CallbackSSLCommerz(IFormCollection f)
        {
            var status = f["status"].ToString();
            if (status != "VALID" && status != "VALIDATED") return Fail("SSLCommerz payment not valid.");
            var txnId  = f["tran_id"].ToString();
            var amount = decimal.TryParse(f["amount"].ToString(), out var a) ? a : 0m;
            // tran_id = BILL{id}...
            var billId = int.TryParse(new string(txnId.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray()), out var b) ? b : 0;
            // Verify hash
            var storePass = await Cfg("Payment:SSLCommerz:StorePassword") ?? string.Empty;
            var verifyHash = Md5Hex(storePass + f["amount"] + f["currency_amount"] + f["store_amount"] + f["card_no"] + txnId + f["val_id"]).ToLowerInvariant();
            if (!string.Equals(verifyHash, f["verify_sign"].ToString(), StringComparison.OrdinalIgnoreCase))
                return Fail("SSLCommerz hash verification failed.");
            return Ok(billId, amount, txnId, "sslcommerz");
        }

        private PaymentGatewayCallbackResult CallbackWalkingm(IQueryCollection q)
        {
            var status = q["status"].ToString();
            if (status != "success") return Fail("Walkingm payment not successful.");
            var txnId  = q["transaction_id"].ToString();
            var amount = decimal.TryParse(q["amount"].ToString(), out var a) ? a : 0m;
            var billId = int.TryParse(q["ref"].ToString().Replace("BILL", ""), out var b) ? b : 0;
            return Ok(billId, amount, txnId, "walkingm");
        }

        private PaymentGatewayCallbackResult CallbackEasyPaisa(IQueryCollection q)
        {
            var status    = q["responseCode"].ToString();
            if (status != "0000") return Fail($"EasyPaisa payment failed: {q["responseDesc"]}");
            var txnId     = q["transactionId"].ToString();
            var amount    = decimal.TryParse(q["transactionAmount"].ToString(), out var a) ? a : 0m;
            var orderId   = q["orderId"].ToString(); // BILL{id}...
            var billId    = int.TryParse(new string(orderId.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray()), out var b) ? b : 0;
            return Ok(billId, amount, txnId, "easypaisa");
        }

        // ════════════════════════════════════════════════════════════════════════
        // CRYPTO HELPERS
        // ════════════════════════════════════════════════════════════════════════
        private static string Sha512Hex(string input)
        {
            var bytes = SHA512.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string AesEncryptCCAvenue(string plainText, string workingKey)
        {
            var keyBytes = Md5Hex(workingKey).ToLower();
            using var aes = Aes.Create();
            aes.Key     = Encoding.UTF8.GetBytes(keyBytes)[..16];
            aes.IV      = new byte[16];
            aes.Mode    = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using var enc = aes.CreateEncryptor();
            var data   = Encoding.UTF8.GetBytes(plainText);
            var result = enc.TransformFinalBlock(data, 0, data.Length);
            return Convert.ToHexString(result).ToLowerInvariant();
        }

        private static string AesDecryptCCAvenue(string hexCipher, string workingKey)
        {
            var keyBytes = Md5Hex(workingKey).ToLower();
            using var aes = Aes.Create();
            aes.Key     = Encoding.UTF8.GetBytes(keyBytes)[..16];
            aes.IV      = new byte[16];
            aes.Mode    = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using var dec = aes.CreateDecryptor();
            var data   = Convert.FromHexString(hexCipher);
            var result = dec.TransformFinalBlock(data, 0, data.Length);
            return Encoding.UTF8.GetString(result);
        }
    }
}
