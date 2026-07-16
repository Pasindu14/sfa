using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;

namespace sfa_api.Features.Stock.Repositories;

public interface IStockRepository
{
    Task<(List<DistributorStock> Items, int TotalCount)> GetStockByDistributorAsync(int distributorId, int skip, int take, CancellationToken ct = default);
    Task<List<DistributorStock>> GetAllStockByDistributorAsync(int distributorId, CancellationToken ct = default);
    Task<List<StockTransaction>> GetTransactionsByDistributorAndProductAsync(
        int distributorId, int productId, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetTransactionCountAsync(int distributorId, int productId, CancellationToken ct = default);

    /// <summary>
    /// Deducts <paramref name="quantity"/> units from a distributor's stock balance and appends
    /// an immutable <see cref="StockTransaction"/> ledger entry.
    /// Must be called inside an explicit transaction that already holds a row-level lock
    /// via <c>SELECT … FOR UPDATE</c> on the <see cref="DistributorStock"/> row.
    /// Throws <see cref="sfa_api.Common.Errors.BusinessRuleException"/> (INSUFFICIENT_STOCK)
    /// when the resulting balance would fall below zero.
    /// </summary>
    Task DeductStockAsync(
        int distributorId,
        int productId,
        decimal quantity,
        StockType stockType,
        StockTransactionType transactionType,
        string referenceType,
        int referenceId,
        int transactedBy,
        string? notes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Pessimistically locks the <see cref="DistributorStock"/> row via SELECT … FOR UPDATE.
    /// Returns the EF-tracked entity so subsequent Deduct/Credit calls use the change tracker.
    /// Must be called inside an explicit transaction.
    /// </summary>
    Task<DistributorStock?> GetStockForUpdateAsync(
        int distributorId,
        int productId,
        StockType stockType,
        CancellationToken ct = default);

    /// <summary>
    /// Credits <paramref name="quantity"/> units back to a distributor's stock balance and appends
    /// an immutable <see cref="StockTransaction"/> ledger entry (Direction=In).
    /// Must be called inside an explicit transaction that already holds a row-level lock
    /// via <c>SELECT … FOR UPDATE</c> on the <see cref="DistributorStock"/> row.
    /// No negative-stock guard — credits are always valid.
    /// </summary>
    Task CreditStockAsync(
        int distributorId,
        int productId,
        decimal quantity,
        StockType stockType,
        StockTransactionType transactionType,
        string referenceType,
        int referenceId,
        int transactedBy,
        string? notes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Re-tags every <see cref="DistributorStock"/> row of a distributor with a new fleet, after the
    /// distributor's own <c>FleetId</c> changed. <see cref="DistributorStock.FleetId"/> is denormalized
    /// current state, so it must follow the distributor; <see cref="StockTransaction.FleetId"/> is a
    /// historical fact and is deliberately left frozen.
    /// Caller must run this in the same transaction as the distributor update so the two cannot diverge.
    /// Returns the number of stock rows re-tagged.
    /// </summary>
    Task<int> CascadeDistributorFleetChangeAsync(int distributorId, int? newFleetId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
