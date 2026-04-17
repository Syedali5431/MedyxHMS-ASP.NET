using System.Threading.Tasks;
using MedyxHMS.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MedyxHMS.Services.Implementations
{
    // SMS adapter placeholder: can be swapped with Twilio/Africa's Talking provider later.
    public class SmsLogNotificationProvider : ISmsNotificationProvider
    {
        private readonly ILogger<SmsLogNotificationProvider> _logger;

        public SmsLogNotificationProvider(ILogger<SmsLogNotificationProvider> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string toPhone, string message)
        {
            _logger.LogInformation("SMS notification queued (log adapter). To={Phone} | Message={Message}", toPhone, message);
            return Task.CompletedTask;
        }
    }
}
