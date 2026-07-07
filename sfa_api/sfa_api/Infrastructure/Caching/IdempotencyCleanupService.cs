using Microsoft.EntityFrameworkCore;
using sfa_api.Infrastructure.Locking;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Infrastructure.Caching;

public class IdempotencyCleanupService(IServiceScopeFactory scopeFactory,
    ILogger<IdempotencyCleanupService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<IdempotencyCleanupService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            try
            {
                using var scope = _scopeFactory.CreateScope();

                // Only one API instance should run this sweep per tick (finding #10). The lock is a
                // non-blocking try-acquire; if another instance holds it, skip this tick. The delete
                // is idempotent, so even a lock expiry mid-run cannot corrupt anything.
                var lockService = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();
                await using var handle = await lockService.AcquireAsync(
                    "background:idempotency-cleanup", stoppingToken);
                if (handle is null)
                {
                    _logger.LogDebug("Idempotency-cleanup lock held by another instance; skipping tick.");
                    continue;
                }

                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var deleted = await db.IdempotencyKeys
                    .Where(x => x.ExpiresAt < DateTime.UtcNow)
                    .ExecuteDeleteAsync(stoppingToken);

                if (deleted > 0)
                    _logger.LogInformation(
                        "Cleaned up {Count} expired idempotency keys", deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Idempotency cleanup failed");
            }
        }
    }
}
