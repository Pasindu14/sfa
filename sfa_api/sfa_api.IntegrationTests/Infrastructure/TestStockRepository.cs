using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Repositories;

namespace sfa_api.IntegrationTests.Infrastructure;

/// <summary>
/// Test-only IStockRepository wrapper that replaces GetStockForUpdateAsync
/// (which issues a PostgreSQL "SELECT … FOR UPDATE" raw query SQLite cannot parse)
/// with a no-op. The service ignores the return value — it only calls the method to
/// take a pessimistic row lock — and DeductStockAsync/CreditStockAsync re-query the
/// tracked row themselves. With SQLite's single in-memory connection there is no
/// concurrency to guard, so dropping the lock is behaviourally equivalent.
/// All other calls delegate to the real StockRepository.
/// </summary>
public sealed class TestStockRepository(IStockRepository inner) : IStockRepository
{
    public Task<DistributorStock?> GetStockForUpdateAsync(
        int distributorId, int productId, StockType stockType, CancellationToken ct = default)
        => Task.FromResult<DistributorStock?>(null);

    public Task<(List<DistributorStock> Items, int TotalCount)> GetStockByDistributorAsync(
        int distributorId, int skip, int take, CancellationToken ct = default)
        => inner.GetStockByDistributorAsync(distributorId, skip, take, ct);

    public Task<List<DistributorStock>> GetAllStockByDistributorAsync(int distributorId, CancellationToken ct = default)
        => inner.GetAllStockByDistributorAsync(distributorId, ct);

    public Task<List<StockTransaction>> GetTransactionsByDistributorAndProductAsync(
        int distributorId, int productId, int page, int pageSize, CancellationToken ct = default)
        => inner.GetTransactionsByDistributorAndProductAsync(distributorId, productId, page, pageSize, ct);

    public Task<int> GetTransactionCountAsync(int distributorId, int productId, CancellationToken ct = default)
        => inner.GetTransactionCountAsync(distributorId, productId, ct);

    public Task DeductStockAsync(
        int distributorId, int productId, decimal quantity,
        StockType stockType, StockTransactionType transactionType,
        string referenceType, int referenceId, int transactedBy,
        string? notes = null, CancellationToken ct = default)
        => inner.DeductStockAsync(distributorId, productId, quantity, stockType, transactionType,
            referenceType, referenceId, transactedBy, notes, ct);

    public Task CreditStockAsync(
        int distributorId, int productId, decimal quantity,
        StockType stockType, StockTransactionType transactionType,
        string referenceType, int referenceId, int transactedBy,
        string? notes = null, CancellationToken ct = default)
        => inner.CreditStockAsync(distributorId, productId, quantity, stockType, transactionType,
            referenceType, referenceId, transactedBy, notes, ct);

    public Task<int> CascadeDistributorFleetChangeAsync(
        int distributorId, int? newFleetId, CancellationToken ct = default)
        => inner.CascadeDistributorFleetChangeAsync(distributorId, newFleetId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => inner.SaveChangesAsync(ct);
}
