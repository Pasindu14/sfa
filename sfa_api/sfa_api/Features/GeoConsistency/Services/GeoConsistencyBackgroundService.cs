using sfa_api.Features.GeoConsistency.Repositories;
using sfa_api.Infrastructure.Locking;

namespace sfa_api.Features.GeoConsistency.Services;

/// <summary>
/// Nightly unattended geo-consistency check (companion to the re-parent cascade). Runs the drift scan,
/// persists the run so a dashboard can show "last run — N drifted", then purges runs past the retention
/// window. Mirrors <c>StockReconciliationBackgroundService</c>. Does NOT auto-repair — surfacing drift
/// is the job; correcting it is an explicit admin action via the repair endpoint.
/// </summary>
public class GeoConsistencyBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<GeoConsistencyBackgroundService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<GeoConsistencyBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            try
            {
                using var scope = _scopeFactory.CreateScope();

                // Only one API instance should run the nightly check — otherwise each replica inserts a
                // duplicate run row and re-scans every table. Non-blocking try-acquire; skip this tick
                // if another instance already holds the lock.
                var lockService = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();
                await using var handle = await lockService.AcquireAsync(
                    "background:geo-consistency", stoppingToken);
                if (handle is null)
                {
                    _logger.LogDebug("Geo-consistency lock held by another instance; skipping tick.");
                    continue;
                }

                var service = scope.ServiceProvider.GetRequiredService<IGeoConsistencyService>();
                var repo    = scope.ServiceProvider.GetRequiredService<IGeoConsistencyRepository>();

                await service.RunAsync(triggeredBy: "nightly", stoppingToken);

                var retentionDays = _configuration.GetValue<int>("GeoConsistencyRetentionDays", 30);
                var purged = await repo.PurgeRunsBeforeAsync(
                    DateTime.UtcNow.AddDays(-retentionDays), stoppingToken);
                if (purged > 0)
                    _logger.LogInformation(
                        "Purged {Count} geo-consistency runs older than {RetentionDays} days",
                        purged, retentionDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nightly geo-consistency check failed");
            }
        }
    }
}
