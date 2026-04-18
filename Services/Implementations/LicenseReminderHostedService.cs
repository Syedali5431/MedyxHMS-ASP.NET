using MedyxHMS.Services.Interfaces;

namespace MedyxHMS.Services.Implementations
{
    public class LicenseReminderHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<LicenseReminderHostedService> _logger;

        public LicenseReminderHostedService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<LicenseReminderHostedService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunReminderCycleAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunReminderCycleAsync(stoppingToken);
            }
        }

        private async Task RunReminderCycleAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var licenseService = scope.ServiceProvider.GetRequiredService<ILicenseService>();
                var result = await licenseService.SendReminderAsync(force: false, performedByUserId: "System");

                if (!string.Equals(result.Status, "Skipped", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "License reminder cycle completed with status {Status}. Sent={Sent} Failed={Failed}",
                        result.Status,
                        result.SentToCount,
                        result.FailedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "License reminder cycle failed.");
            }
        }
    }
}