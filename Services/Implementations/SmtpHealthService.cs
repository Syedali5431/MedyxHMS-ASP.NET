using System.Net.Sockets;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;

namespace MedyxHMS.Services.Implementations
{
    public class SmtpHealthService : ISmtpHealthService
    {
        private readonly IConfiguration _configuration;

        public SmtpHealthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<SmtpHealthStatus> CheckAsync()
        {
            var status = new SmtpHealthStatus
            {
                Host = (_configuration["Notification:Smtp:Host"] ?? string.Empty).Trim(),
                FromEmail = (_configuration["Notification:Smtp:FromEmail"] ?? string.Empty).Trim(),
                EnableSsl = bool.TryParse(_configuration["Notification:Smtp:EnableSsl"], out var ssl) && ssl
            };

            if (!int.TryParse(_configuration["Notification:Smtp:Port"], out var port))
            {
                status.Issues.Add("SMTP port is missing or invalid.");
                port = 587;
            }

            status.Port = port;

            if (string.IsNullOrWhiteSpace(status.Host))
                status.Issues.Add("SMTP host is missing.");

            if (string.IsNullOrWhiteSpace(status.FromEmail))
                status.Issues.Add("SMTP from-email is missing.");

            status.IsConfigured = status.Issues.Count == 0;
            if (!status.IsConfigured)
            {
                status.ConnectivityOk = false;
                return status;
            }

            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try
            {
                await client.ConnectAsync(status.Host, status.Port, cts.Token);
                status.ConnectivityOk = client.Connected;
                if (!status.ConnectivityOk)
                    status.Issues.Add("Unable to open TCP connection to SMTP host.");
            }
            catch (Exception ex)
            {
                status.ConnectivityOk = false;
                status.Issues.Add($"SMTP connectivity failed: {ex.Message}");
            }

            return status;
        }
    }
}
