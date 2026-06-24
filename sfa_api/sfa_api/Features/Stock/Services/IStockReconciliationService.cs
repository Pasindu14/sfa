using sfa_api.Features.Stock.DTOs;

namespace sfa_api.Features.Stock.Services;

public interface IStockReconciliationService
{
    /// <summary>
    /// Runs a reconciliation pass (optionally scoped to one distributor/product), persists the run
    /// and any flags, logs a warning per discrepancy, and returns the enriched result.
    /// </summary>
    Task<StockReconciliationResultDto> RunAsync(
        int? distributorId, int? productId, string triggeredBy, CancellationToken ct = default);

    /// <summary>The most recent persisted run (for a dashboard tile). Null if none has run yet.</summary>
    Task<StockReconciliationResultDto?> GetLatestRunAsync(CancellationToken ct = default);
}
