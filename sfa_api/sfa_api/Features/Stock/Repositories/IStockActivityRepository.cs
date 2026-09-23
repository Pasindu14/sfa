using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Enums;

namespace sfa_api.Features.Stock.Repositories;

/// <summary>Read-only queries over the StockTransactions ledger for the admin activity log.</summary>
public interface IStockActivityRepository
{
    /// <summary>
    /// One page of ledger rows in [fromUtc, toExclusiveUtc), newest first (TransactedAt, Id desc),
    /// with distributor/product/user names resolved — soft-deleted ones included. ReferenceNumber
    /// is left null; <see cref="GetReferenceNumbersAsync"/> fills it per page.
    /// </summary>
    Task<(List<StockActivityDto> Items, int TotalCount)> GetPagedAsync(
        DateTime fromUtc, DateTime toExclusiveUtc,
        int? distributorId, int? productId, int? userId,
        StockTransactionType? transactionType, StockTransactionDirection? direction,
        int skip, int take, CancellationToken ct = default);

    /// <summary>
    /// Human-readable document numbers keyed by (ReferenceType, ReferenceId) — one query per
    /// supported reference type present in <paramref name="references"/>. Unsupported types are absent.
    /// </summary>
    Task<Dictionary<(string ReferenceType, int ReferenceId), string>> GetReferenceNumbersAsync(
        IEnumerable<(string ReferenceType, int ReferenceId)> references, CancellationToken ct = default);

    /// <summary>Every user who appears as TransactedBy on at least one ledger row, ordered by name.</summary>
    Task<List<StockActivityUserDto>> GetUsersAsync(CancellationToken ct = default);
}
