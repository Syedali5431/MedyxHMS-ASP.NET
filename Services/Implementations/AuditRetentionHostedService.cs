using MedyxHMS.Services.Interfaces;

namespace MedyxHMS.Services.Implementations
{
    /// <summary>Once a day, deletes audit and user-action logs older than the configured retention period.</summary>
    public class AuditRetentionHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuditRetentionHostedService> _logger;

        public AuditRetentionHostedService(IServiceScopeFactory scopeFactory, ILogger<AuditRetentionHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // First run shortly after start-up (not during it), then daily.
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await RunAsync(stoppingToken);
            using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunAsync(stoppingToken);
            }
        }

        private async Task RunAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var policy = scope.ServiceProvider.GetRequiredService<ISecurityPolicyService>();
                await policy.PurgeOldAuditLogsAsync(null);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit-log retention run failed");
            }
        }
    }
}
