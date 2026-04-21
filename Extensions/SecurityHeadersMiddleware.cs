using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;

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
                response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
                response.Headers.Add("X-Content-Type-Options", "nosniff");

                // XSS Protection
                response.Headers.Add("X-XSS-Protection", "1; mode=block");

                // Content Security Policy - Strict but functional
                response.Headers.Add("Content-Security-Policy",
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' 'unsafe-eval' cdn.jsdelivr.net cdnjs.cloudflare.com; " +
                    "style-src 'self' 'unsafe-inline' cdn.jsdelivr.net cdnjs.cloudflare.com fonts.googleapis.com; " +
                    "font-src 'self' fonts.gstatic.com cdn.jsdelivr.net cdnjs.cloudflare.com; " +
                    "img-src 'self' data: https:; " +
                    "connect-src 'self'; " +
                    "frame-ancestors 'self'; " +
                    "form-action 'self'; " +
                    "upgrade-insecure-requests;");

                // Referrer Policy
                response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

                // Feature Policy / Permissions Policy
                response.Headers.Add("Permissions-Policy",
                    "geolocation=(), microphone=(), camera=(), usb=(), accelerometer=(), gyroscope=(), magnetometer=()");

                // Enable HSTS in production
                if (!context.Request.IsHttps && context.Request.Host.Host != "localhost")
                {
                    response.Headers.Add("Strict-Transport-Security",
                        "max-age=31536000; includeSubDomains; preload");
                }

                // Cross-Origin policies
                response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
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
        private const int MaxFailedAttempts = 5;

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"{clientIp}:{context.Request.Path}";

            lock (RequestCounts)
            {
                if (RequestCounts.TryGetValue(key, out var data))
                {
                    if (DateTime.UtcNow < data.ResetTime)
                    {
                        if (data.Count > MaxRequestsPerMinute)
                        {
                            _logger.LogWarning("Rate limit exceeded for IP: {ClientIp}", clientIp);
                            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                            context.Response.Headers.Add("Retry-After", "60");
                            return;
                        }

                        RequestCounts[key] = (data.Count + 1, data.ResetTime);
                    }
                    else
                    {
                        RequestCounts[key] = (1, DateTime.UtcNow.AddMinutes(1));
                    }
                }
                else
                {
                    RequestCounts[key] = (1, DateTime.UtcNow.AddMinutes(1));
                }
            }

            await _next(context);
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
                if (!context.Request.Headers.ContainsKey("Authorization"))
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
    }

    /// <summary>
    /// Extension methods to register security middleware.
    /// </summary>
    public static class SecurityMiddlewareExtensions
    {
        public static IApplicationBuilder UseEnhancedSecurity(this IApplicationBuilder app)
        {
            // Enable HTTPS redirection
            app.UseHttpsRedirection();

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
