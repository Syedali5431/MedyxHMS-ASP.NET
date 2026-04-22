using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.Extensions.Logging;

// Purpose: Contains application code for PublicBookingNotificationService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    // Step 4.2 notification hook (stub): currently logs payload for future email/SMS providers.
    public class PublicBookingNotificationService : IPublicBookingNotificationService
    {
        private readonly ISettingService _settingService;
        private readonly IEmailNotificationProvider _emailProvider;
        private readonly ISmsNotificationProvider _smsProvider;
        private readonly ILogger<PublicBookingNotificationService> _logger;

        public PublicBookingNotificationService(
            ISettingService settingService,
            IEmailNotificationProvider emailProvider,
            ISmsNotificationProvider smsProvider,
            ILogger<PublicBookingNotificationService> logger)
        {
            _settingService = settingService;
            _emailProvider = emailProvider;
            _smsProvider = smsProvider;
            _logger = logger;
        }

        public async Task NotifyAppointmentConfirmedAsync(PublicAppointmentRequest request, string doctorDisplayName)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var hospitalSettings = await _settingService.GetHospitalSettingsAsync();
            var dateText = request.PreferredDate.ToString("dd MMM yyyy");
            var timeText = request.PreferredTime.ToString(@"hh\\:mm");
            var supportPhone = await _settingService.GetSettingValueAsync("PublicSitePhone") ?? "+000-000-0000";

            var subjectTemplate = await _settingService.GetSettingValueAsync("Notification:Templates:AppointmentConfirmed:EmailSubject")
                ?? "Appointment Request Confirmed";
            var emailBodyTemplate = await _settingService.GetSettingValueAsync("Notification:Templates:AppointmentConfirmed:EmailBody")
                ?? "Hello {{PatientName}},\n\nYour appointment request has been confirmed.\nDoctor: {{DoctorName}}\nDate: {{Date}}\nTime: {{Time}}\n\nPlease arrive 15 minutes before your scheduled time.\nIf you need to reschedule, contact the hospital front desk at {{SupportPhone}}.\n\nRegards,\n{{HospitalName}}";
            var smsTemplate = await _settingService.GetSettingValueAsync("Notification:Templates:AppointmentConfirmed:SmsBody")
                ?? "Medyx: Appointment confirmed for {{PatientName}} with {{DoctorName}} on {{Date}} at {{Time}}.";

            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["PatientName"] = request.PatientName ?? string.Empty,
                ["DoctorName"] = doctorDisplayName ?? string.Empty,
                ["Date"] = dateText,
                ["Time"] = timeText,
                ["HospitalName"] = hospitalSettings.Name ?? "Medyx Hospital",
                ["SupportPhone"] = supportPhone
            };

            var subject = ApplyTemplate(subjectTemplate, tokens);
            var emailBody = ApplyTemplate(emailBodyTemplate, tokens);
            var smsMessage = ApplyTemplate(smsTemplate, tokens);

            if (hospitalSettings.EnableEmailNotifications && !string.IsNullOrWhiteSpace(request.Email))
            {
                await _emailProvider.SendAsync(request.Email.Trim(), subject, emailBody);
            }

            if (hospitalSettings.EnableSMSNotifications && !string.IsNullOrWhiteSpace(request.Phone))
            {
                await _smsProvider.SendAsync(request.Phone.Trim(), smsMessage);
            }

            _logger.LogInformation(
                "PUBLIC BOOKING CONFIRMED NOTIFICATION DISPATCHED | RequestId={RequestId} | Patient={PatientName} | Phone={Phone} | Email={Email} | EmailEnabled={EmailEnabled} | SmsEnabled={SmsEnabled} | Doctor={Doctor} | Date={Date} | Time={Time}",
                request.Id,
                request.PatientName,
                request.Phone,
                string.IsNullOrWhiteSpace(request.Email) ? "N/A" : request.Email,
                hospitalSettings.EnableEmailNotifications,
                hospitalSettings.EnableSMSNotifications,
                doctorDisplayName,
                request.PreferredDate.ToString("yyyy-MM-dd"),
                request.PreferredTime.ToString(@"hh\\:mm"));
        }

        private static string ApplyTemplate(string template, IReadOnlyDictionary<string, string> tokens)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return string.Empty;
            }

            var result = template;
            foreach (var token in tokens)
            {
                result = result.Replace($"{{{{{token.Key}}}}}", token.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return result;
        }
    }
}
