using Microsoft.EntityFrameworkCore;
using sfa_api.Infrastructure.Locking;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.LocationPings.Services;

/// <summary>
/// Optional retention purge for <c>RepLocationPings</c>, modelled on AuditLogCleanupService.
///
/// DISABLED BY DEFAULT: runs only when <c>LocationPings:RetentionDays</c> is a positive number.
/// Pings are the only source for the admin rep-route history (GET location-pings/rep/{id}/route),
/// and deletion is irreversible — turning this on permanently removes route history older than
/// the retention window. Deletes in bounded batches so a first run over a large backlog doesn't
/// hold one long-running statement.
/// </summary>
public class LocationPingRetentionService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<LocationPingRetentionService> logger) : BackgroundService
{
    private const int BatchSize = 5_000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            try
            {
                var retentionDays = configuration.GetValue<int?>("LocationPings:RetentionDays") ?? 0;
                if (retentionDays <= 0) continue;   // off unless explicitly configured

                // UTC instant — Npgsql rejects non-zero-offset DateTimeOffset parameters.
                var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);

                using var scope = scopeFactory.CreateScope();

                var lockService = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();
                await using var handle = await lockService.AcquireAsync(
                    "background:location-ping-retention", stoppingToken);
                if (handle is null)
                {
                    logger.LogDebug("Location-ping-retention lock held by another instance; skipping tick.");
                    continue;
                }

                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var total = 0;
                int deleted;
                do
                {
                    var batchIds = db.RepLocationPings
                        .Where(p => p.RecordedAt < cutoff)
                        .OrderBy(p => p.Id)
                        .Select(p => p.Id)
                        .Take(BatchSize);

                    deleted = await db.RepLocationPings
                        .Where(p => batchIds.Contains(p.Id))
                        .ExecuteDeleteAsync(stoppingToken);
                    total += deleted;
                } while (deleted == BatchSize && !stoppingToken.IsCancellationRequested);

                if (total > 0)
                    logger.LogInformation(
                        "Purged {Count} location pings older than {RetentionDays} days",
                        total, retentionDays);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Location ping retention purge failed");
            }
        }
    }
}
