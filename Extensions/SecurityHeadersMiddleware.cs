using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;

// Purpose: Contains application code for SecurityHeadersMiddleware and its related runtime behavior.
namespace MedyxHMS.Extensions
{
    /// <summary>
    /// Security middleware for enforcing HTTP security headers and protections.
    /// Protects against common web vulnerabilities:
    /// - XSS (Cross-Site Scripting)
    /// - Clickjacking
    /// - MIME sniffing
    /// - CSS injection
    /// - Referrer leaks
    /// - Insecure content loading
    /// </summary>
    public static class SecurityHeadersExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                var response = context.Response;

                // Remove server header to avoid information disclosure
                response.Headers.Remove("Server");
                response.Headers.Remove("X-Powered-By");
                response.Headers.Remove("X-AspNet-Version");

                // Prevent clickjacking attacks
                response.Headers["X-Frame-Options"] = "SAMEORIGIN";
                response.Headers["X-Content-Type-Options"] = "nosniff";

                // XSS Protection
                response.Headers["X-XSS-Protection"] = "1; mode=block";

                // Content Security Policy - Strict but functional.
                // "upgrade-insecure-requests" is skipped on plain-HTTP localhost dev servers —
                // it otherwise makes the browser rewrite same-origin requests to https:// and
                // fail with ERR_SSL_PROTOCOL_ERROR since no TLS listener exists there.
                var isLocalHttp = !context.Request.IsHttps && context.Request.Host.Host == "localhost";
                response.Headers["Content-Security-Policy"] =
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' 'unsafe-eval' cdn.jsdelivr.net cdnjs.cloudflare.com; " +
                    "style-src 'self' 'unsafe-inline' cdn.jsdelivr.net cdnjs.cloudflare.com fonts.googleapis.com; " +
                    "font-src 'self' data: fonts.gstatic.com cdn.jsdelivr.net cdnjs.cloudflare.com; " +
                    "img-src 'self' data: https:; " +
                    "connect-src 'self'; " +
                    // Map embeds on the public Contact us / Location pages.
                    "frame-src 'self' https://www.google.com https://maps.google.com https://www.openstreetmap.org; " +
                    "frame-ancestors 'self'; " +
                    "form-action 'self';" +
                    (isLocalHttp ? string.Empty : " upgrade-insecure-requests;");

                // Referrer Policy
                response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

                // Feature Policy / Permissions Policy
                response.Headers["Permissions-Policy"] =
                    "geolocation=(), microphone=(), camera=(), usb=(), accelerometer=(), gyroscope=(), magnetometer=()";

                // Enable HSTS in production
                if (!context.Request.IsHttps && context.Request.Host.Host != "localhost")
                {
                    response.Headers["Strict-Transport-Security"] =
                        "max-age=31536000; includeSubDomains; preload";
                }

                // Cross-Origin policies
                // Not on the public website: its Contact / Location pages embed Google Maps, which a page with
                // "require-corp" is not allowed to frame.
                if (!context.Request.Path.StartsWithSegments("/Site"))
                {
                    response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
                }
                response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
                response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";

                // Disable caching for sensitive data
                if (context.Request.Path.StartsWithSegments("/admin") ||
                    context.Request.Path.StartsWithSegments("/account") ||
                    context.Request.Path.StartsWithSegments("/api"))
                {
                    response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, proxy-revalidate, max-age=0";
                    response.Headers["Pragma"] = "no-cache";
                    response.Headers["Expires"] = "0";
                }

                await next();
            });

            return app;
        }
    }

    /// <summary>
    /// Rate limiting middleware to prevent brute force attacks and DDoS.
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private static readonly Dictionary<string, (int Count, DateTime ResetTime)> RequestCounts = new();
        private const int MaxRequestsPerMinute = 100;
        // Expired counters are purged once this many keys are tracked, so memory cannot grow without limit.
        private const int PurgeThreshold = 10_000;

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var client = await ResolveClientAsync(context);
            var key = $"{client}:{context.Request.Path}";
            var now = DateTime.UtcNow;
            var limited = false;

            lock (RequestCounts)
            {
                if (RequestCounts.Count >= PurgeThreshold)
                {
                    foreach (var expired in RequestCounts.Where(kv => kv.Value.ResetTime <= now).Select(kv => kv.Key).ToList())
                        RequestCounts.Remove(expired);
                }

                if (RequestCounts.TryGetValue(key, out var data) && now < data.ResetTime)
                {
                    if (data.Count > MaxRequestsPerMinute)
                        limited = true;
                    else
                        RequestCounts[key] = (data.Count + 1, data.ResetTime);
                }
                else
                {
                    RequestCounts[key] = (1, now.AddMinutes(1));
                }
            }

            if (limited)
            {
                _logger.LogWarning("Rate limit exceeded for {Client} on {Path}", client, context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = "60";
                return;
            }

            await _next(context);
        }

        /// <summary>
        /// Signed-in users get their own budget, so many staff behind one hospital NAT/proxy do not
        /// share (and exhaust) a single per-IP limit. Anonymous requests and static files stay per IP.
        /// </summary>
        private static async Task<string> ResolveClientAsync(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (context.Request.Cookies.Count == 0 || Path.HasExtension(context.Request.Path.Value))
                return "ip:" + ip;

            // Validates the auth cookie; the result is cached for the request, so the later
            // authentication middleware does not repeat the work.
            var auth = await context.AuthenticateAsync();
            var userId = auth.Succeeded ? auth.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
            return string.IsNullOrEmpty(userId) ? "ip:" + ip : "user:" + userId;
        }
    }

    /// <summary>
    /// Input validation middleware for sanitizing and validating request data.
    /// </summary>
    public class InputValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<InputValidationMiddleware> _logger;

        public InputValidationMiddleware(RequestDelegate next, ILogger<InputValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check for potentially malicious patterns
            if (context.Request.QueryString.HasValue)
            {
                var query = context.Request.QueryString.Value;
                if (ContainsMaliciousPatterns(query))
                {
                    _logger.LogWarning("Potential SQL injection detected in query from IP: {ClientIp}",
                        context.Connection.RemoteIpAddress);
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }
            }

            // Check request body for API calls
            if (context.Request.ContentType?.Contains("application/json") == true)
            {
                context.Request.EnableBuffering();
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (ContainsMaliciousPatterns(body))
                {
                    _logger.LogWarning("Potential injection detected in request body from IP: {ClientIp}",
                        context.Connection.RemoteIpAddress);
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }
            }

            await _next(context);
        }

        private static bool ContainsMaliciousPatterns(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var patterns = new[]
            {
                @"(\b(UNION|SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE|SCRIPT|JAVASCRIPT|ONERROR|ONLOAD)\b)",
                @"(--|;|'|""|\*)(?=.*(\bOR\b|\b1\s*=\s*1\b))",
                @"<\s*script",
                @"javascript\s*:",
                @"onerror\s*=",
                @"onload\s*=",
                @"<\s*iframe",
                @"%3c\s*script", // URL encoded <script
            };

            var options = System.Text.RegularExpressions.RegexOptions.IgnoreCase;
            foreach (var pattern in patterns)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(input, pattern, options))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Middleware for validating and enforcing API security policies.
    /// </summary>
    public class ApiSecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiSecurityMiddleware> _logger;

        public ApiSecurityMiddleware(RequestDelegate next, ILogger<ApiSecurityMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Enforce HTTPS for API endpoints
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                if (!context.Request.IsHttps && context.Request.Host.Host != "localhost")
                {
                    _logger.LogWarning("Insecure API request from IP: {ClientIp}",
                        context.Connection.RemoteIpAddress);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("HTTPS required for API endpoints");
                    return;
                }

                // Check for required API key or bearer token
                if (!context.Request.Headers.ContainsKey("Authorization") && !HasAppAuthCookie(context))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Authorization header required");
                    return;
                }

                // Validate content type for POST/PUT requests
                if ((context.Request.Method == "POST" || context.Request.Method == "PUT") &&
                    !context.Request.ContentType?.Contains("application/json") == true)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Content-Type must be application/json");
                    return;
                }
            }

            await _next(context);
        }

        private static bool HasAppAuthCookie(HttpContext context)
        {
            foreach (var cookie in context.Request.Cookies.Keys)
            {
                if (cookie.Contains("Identity.Application", StringComparison.OrdinalIgnoreCase) ||
                    cookie.Contains(".AspNetCore.Cookies", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Extension methods to register security middleware.
    /// </summary>
    public static class SecurityMiddlewareExtensions
    {
        public static IApplicationBuilder UseEnhancedSecurity(this IApplicationBuilder app)
        {
            // HTTPS redirection is configured in Program.cs (outside Development); calling it here as well
            // logged "Failed to determine the https port for redirect" when running over plain http.

            // Use X-Forwarded-For headers from reverse proxy
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                RequireHeaderSymmetry = false,
                KnownNetworks = { }
            });

            // Apply security headers
            app.UseSecurityHeaders();

            // Apply rate limiting
            app.UseMiddleware<RateLimitingMiddleware>();

            // Apply input validation
            app.UseMiddleware<InputValidationMiddleware>();

            // Apply API security
            app.UseMiddleware<ApiSecurityMiddleware>();

            return app;
        }
    }
}
