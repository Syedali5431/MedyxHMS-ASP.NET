using System;
using System.Threading.Tasks;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.Extensions.Logging;

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
            var subject = "Appointment Request Confirmed";
            var dateText = request.PreferredDate.ToString("dd MMM yyyy");
            var timeText = request.PreferredTime.ToString(@"hh\\:mm");

            var emailBody =
                $"Hello {request.PatientName},\n\n" +
                $"Your appointment request has been confirmed.\n" +
                $"Doctor: {doctorDisplayName}\n" +
                $"Date: {dateText}\n" +
                $"Time: {timeText}\n\n" +
                "Please arrive 15 minutes before your scheduled time.\n" +
                "If you need to reschedule, contact the hospital front desk.\n\n" +
                "Regards,\nMedyx Hospital";

            var smsMessage =
                $"Medyx: Appointment confirmed for {request.PatientName} with {doctorDisplayName} on {dateText} at {timeText}.";

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
    }
}
