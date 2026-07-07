using sfa_api.Features.Stock.Repositories;
using sfa_api.Infrastructure.Locking;

namespace sfa_api.Features.Stock.Services;

/// <summary>
/// Nightly unattended stock-ledger reconciliation (review finding #4). Runs the full self-consistency
/// check, persists the run (so a dashboard can show "last run — N discrepancies"), then purges runs
/// past the retention window. Mirrors the existing cleanup BackgroundServices.
/// </summary>
public class StockReconciliationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<StockReconciliationBackgroundService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<StockReconciliationBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            try
            {
                using var scope = _scopeFactory.CreateScope();

                // Only one API instance should run the nightly reconciliation (finding #10) — otherwise
                // each replica inserts a duplicate run row and re-reads the whole ledger. Non-blocking
                // try-acquire; skip this tick if another instance already holds the lock.
                var lockService = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();
                await using var handle = await lockService.AcquireAsync(
                    "background:stock-reconciliation", stoppingToken);
                if (handle is null)
                {
                    _logger.LogDebug("Stock-reconciliation lock held by another instance; skipping tick.");
                    continue;
                }

                var service = scope.ServiceProvider.GetRequiredService<IStockReconciliationService>();
                var repo    = scope.ServiceProvider.GetRequiredService<IStockReconciliationRepository>();

                // Full-scope pass (no distributor/product filter).
                await service.RunAsync(distributorId: null, productId: null, triggeredBy: "nightly", stoppingToken);

                var retentionDays = _configuration.GetValue<int>("StockReconciliationRetentionDays", 30);
                var purged = await repo.PurgeRunsBeforeAsync(
                    DateTime.UtcNow.AddDays(-retentionDays), stoppingToken);
                if (purged > 0)
                    _logger.LogInformation(
                        "Purged {Count} stock-reconciliation runs older than {RetentionDays} days",
                        purged, retentionDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nightly stock reconciliation failed");
            }
        }
    }
}
