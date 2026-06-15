using sfa_api.Features.Stock.DTOs;

namespace sfa_api.Features.Stock.Repositories;

/// <summary>
/// Read-only aggregation queries that power the distributor bin-card report.
/// Kept separate from <see cref="IStockRepository"/> (which handles transactional posting)
/// so the report's heavy grouped reads never touch the write path.
/// </summary>
public interface IBinCardRepository
{
    /// <summary>SUM(Quantity) of movements in [fromUtc, toExclusiveUtc) grouped by product/type/pool/direction.</summary>
    Task<List<BinCardMovementAgg>> GetBinCardMovementsAsync(
        int distributorId, DateTime fromUtc, DateTime toExclusiveUtc, CancellationToken ct = default);

    /// <summary>Combined opening balance per product — QuantityAfter of the last movement before fromUtc, summed across pools.</summary>
    Task<List<BinCardOpeningAgg>> GetBinCardOpeningAsync(
        int distributorId, DateTime fromUtc, CancellationToken ct = default);

    /// <summary>Damage/Expire return quantity per product from bills in range (informational — no stock movement).</summary>
    Task<List<BinCardRepReturnAgg>> GetBinCardRepReturnsAsync(
        int distributorId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Counted quantity per product from the latest stock-take submitted on or before asOfExclusiveUtc.</summary>
    Task<List<BinCardCountAgg>> GetBinCardLatestCountsAsync(
        int distributorId, DateTime asOfExclusiveUtc, CancellationToken ct = default);

    /// <summary>Code, description and dealer pack price for the given product ids.</summary>
    Task<List<BinCardProductInfo>> GetBinCardProductsAsync(
        IReadOnlyCollection<int> productIds, CancellationToken ct = default);

    Task<string?> GetDistributorNameAsync(int distributorId, CancellationToken ct = default);
}
