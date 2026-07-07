using Microsoft.EntityFrameworkCore;
using sfa_api.Infrastructure.Locking;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Auth.Services;

/// <summary>
/// Daily background sweep that purges refresh tokens which are safely past their
/// <c>ExpiresAt</c> plus a forensic grace window. Without this, expired / consumed /
/// revoked rows accumulate forever on the auth table (review finding #8).
///
/// Keying off <c>ExpiresAt</c> alone is sufficient: a consumed or revoked token also
/// carries an <c>ExpiresAt</c>, so one predicate sweeps all three terminal states once
/// the grace window has elapsed. Mirrors <see cref="Infrastructure.Audit.AuditLogCleanupService"/>
/// and <see cref="Infrastructure.Caching.IdempotencyCleanupService"/>.
/// </summary>
public class RefreshTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<RefreshTokenCleanupService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<RefreshTokenCleanupService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            try
            {
                // Keep recently-expired tokens for a short grace window (forensics / replay
                // investigation), then delete. UtcNow is correct here — token expiry is an
                // absolute instant, not a Sri Lanka business day.
                var graceDays = _configuration.GetValue<int>("RefreshTokenRetentionDays", 7);
                var cutoff = DateTime.UtcNow.AddDays(-graceDays);

                using var scope = _scopeFactory.CreateScope();

                // Single-instance guard (finding #10): non-blocking try-acquire; skip the tick if
                // another instance holds it. The delete is idempotent, so a lock expiry is harmless.
                var lockService = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();
                await using var handle = await lockService.AcquireAsync(
                    "background:refresh-token-cleanup", stoppingToken);
                if (handle is null)
                {
                    _logger.LogDebug("Refresh-token-cleanup lock held by another instance; skipping tick.");
                    continue;
                }

                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var deleted = await db.RefreshTokens
                    .Where(x => x.ExpiresAt < cutoff)
                    .ExecuteDeleteAsync(stoppingToken);

                if (deleted > 0)
                    _logger.LogInformation(
                        "Cleaned up {Count} refresh tokens expired more than {GraceDays} days ago",
                        deleted, graceDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh token cleanup failed");
            }
        }
    }
}
