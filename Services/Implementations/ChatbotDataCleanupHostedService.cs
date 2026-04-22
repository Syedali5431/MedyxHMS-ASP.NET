using MedyxHMS.Services.Interfaces;

// Purpose: Contains application code for ChatbotDataCleanupHostedService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class ChatbotDataCleanupHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChatbotDataCleanupHostedService> _logger;

        public ChatbotDataCleanupHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<ChatbotDataCleanupHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunCleanupCycleAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunCleanupCycleAsync(stoppingToken);
            }
        }

        private async Task RunCleanupCycleAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var cleanup = scope.ServiceProvider.GetRequiredService<IChatbotDataCleanupService>();

                var sessions = await cleanup.CleanupExpiredSessionsAsync(stoppingToken);
                var eventsResult = await cleanup.CleanupExpiredEventLogsAsync(stoppingToken);
                var revoked = await cleanup.CleanupUnconsentedDataAsync(stoppingToken);

                _logger.LogInformation(
                    "Chatbot cleanup complete: Sessions={Sessions}, EventLogs={Events}, RevokedData={Revoked}",
                    sessions.DeletedCount,
                    eventsResult.DeletedCount,
                    revoked.DeletedCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // no-op
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chatbot cleanup cycle failed");
            }
        }
    }
}
