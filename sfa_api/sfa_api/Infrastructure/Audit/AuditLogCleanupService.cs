using Microsoft.EntityFrameworkCore;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Infrastructure.Audit;

public class AuditLogCleanupService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AuditLogCleanupService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<AuditLogCleanupService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            try
            {
                var retentionDays = _configuration.GetValue<int>("AuditLogRetentionDays", 90);
                var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var deleted = await db.AuditLogs
                    .Where(x => x.ChangedAt < cutoff)
                    .ExecuteDeleteAsync(stoppingToken);

                if (deleted > 0)
                    _logger.LogInformation(
                        "Cleaned up {Count} audit log entries older than {RetentionDays} days",
                        deleted, retentionDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit log cleanup failed");
            }
        }
    }
}
