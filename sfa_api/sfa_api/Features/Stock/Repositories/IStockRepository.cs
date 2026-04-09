using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;

namespace sfa_api.Features.Stock.Repositories;

public interface IStockRepository
{
    Task<(List<DistributorStock> Items, int TotalCount)> GetStockByDistributorAsync(int distributorId, int skip, int take, CancellationToken ct = default);
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
        StockTransactionType transactionType,
        string referenceType,
        int referenceId,
        int transactedBy,
        string? notes = null,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
