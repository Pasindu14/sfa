using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.IntegrationTests.Infrastructure;

/// <summary>
/// Test-only IStockRepository wrapper that replaces GetStockForUpdateAsync / LockStocksForUpdateAsync
/// (which issue a PostgreSQL "SELECT … FOR UPDATE" raw query SQLite cannot parse) with a plain
/// tracked LINQ load. Callers read the returned balance (stock-taking submit snapshots it, adjust
/// computes its delta from it), so it must return the real row, not null. With SQLite's single
/// in-memory connection there is no concurrency to guard, so dropping the lock is equivalent.
/// All other calls delegate to the real StockRepository.
/// </summary>
public sealed class TestStockRepository(IStockRepository inner, AppDbContext db) : IStockRepository
{
    public async Task<DistributorStock?> GetStockForUpdateAsync(
        int distributorId, int productId, StockType stockType, CancellationToken ct = default)
    {
        var key    = new StockKey(distributorId, productId, stockType);
        var loaded = await TestStockLocking.LoadAsync(db, [key], ct);
        return loaded.GetValueOrDefault(key);
    }

    // Batched lock: SQLite has no FOR UPDATE, so load the rows (tracked) with plain LINQ instead.
    public Task<Dictionary<StockKey, DistributorStock>> LockStocksForUpdateAsync(
        IEnumerable<StockKey> keys, CancellationToken ct = default)
        => TestStockLocking.LoadAsync(db, keys, ct);

    public Task<(List<DistributorStock> Items, int TotalCount)> GetStockByDistributorAsync(
        int distributorId, int skip, int take, CancellationToken ct = default)
        => inner.GetStockByDistributorAsync(distributorId, skip, take, ct);

    public Task<List<DistributorStock>> GetAllStockByDistributorAsync(int distributorId, CancellationToken ct = default)
        => inner.GetAllStockByDistributorAsync(distributorId, ct);

    public Task<List<DistributorStock>> GetAllStockByDistributorWithZeroFillAsync(int distributorId, CancellationToken ct = default)
        => inner.GetAllStockByDistributorWithZeroFillAsync(distributorId, ct);

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
