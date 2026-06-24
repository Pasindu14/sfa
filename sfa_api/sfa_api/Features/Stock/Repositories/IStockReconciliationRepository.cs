using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Services;

namespace sfa_api.Features.Stock.Repositories;

public interface IStockReconciliationRepository
{
    /// <summary>Σ(In) / Σ(Out) per (distributor, product, stock-type) over the whole ledger.
    /// NOTE: SQL GROUP BY SUM over decimal — runs on PostgreSQL; not translatable by the SQLite test provider.</summary>
    Task<List<StockReconciliation.LedgerNet>> GetLedgerNetsAsync(int? distributorId, int? productId, CancellationToken ct = default);

    /// <summary>Latest QuantityAfter per group (greatest Id). No decimal SUM — SQLite-compatible.</summary>
    Task<List<StockReconciliation.Snapshot>> GetLatestSnapshotsAsync(int? distributorId, int? productId, CancellationToken ct = default);

    /// <summary>The live recorded balances per group.</summary>
    Task<Dictionary<StockReconciliation.Key, decimal>> GetOnHandAsync(int? distributorId, int? productId, CancellationToken ct = default);

    /// <summary>Persists a run and its flags in one SaveChanges.</summary>
    Task<StockReconciliationRun> SaveRunAsync(StockReconciliationRun run, CancellationToken ct = default);

    /// <summary>Latest run (with its flags eager-loaded) for the dashboard tile.</summary>
    Task<StockReconciliationRun?> GetLatestRunAsync(CancellationToken ct = default);

    /// <summary>Resolves distributor names + product codes for the given ids (display enrichment).</summary>
    Task<(Dictionary<int, string> DistributorNames, Dictionary<int, string> ProductCodes)> GetNamesAsync(
        IEnumerable<int> distributorIds, IEnumerable<int> productIds, CancellationToken ct = default);

    /// <summary>Deletes runs (and cascades flags) older than the cutoff. Returns rows removed.</summary>
    Task<int> PurgeRunsBeforeAsync(DateTime cutoff, CancellationToken ct = default);
}
