using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.IntegrationTests.Infrastructure;

/// <summary>
/// SQLite stand-in for <see cref="StockLocking.LockForUpdateAsync"/>: SQLite has no
/// <c>FOR UPDATE</c> and no <c>unnest</c>, and with a single in-memory connection there is no
/// concurrency to guard, so a plain tracked LINQ load is behaviourally equivalent. It still returns
/// tracked entities in Id order, so Deduct/Credit exercise the no-reload path exactly as in prod.
/// </summary>
public static class TestStockLocking
{
    public static async Task<Dictionary<StockKey, DistributorStock>> LoadAsync(
        AppDbContext db, IEnumerable<StockKey> keys, CancellationToken ct)
    {
        var wanted = keys.ToHashSet();
        if (wanted.Count == 0) return new Dictionary<StockKey, DistributorStock>();

        var distributorIds = wanted.Select(k => k.DistributorId).Distinct().ToList();
        var productIds     = wanted.Select(k => k.ProductId).Distinct().ToList();

        var candidates = await db.DistributorStocks
            .Where(s => distributorIds.Contains(s.DistributorId) && productIds.Contains(s.ProductId))
            .OrderBy(s => s.Id)
            .ToListAsync(ct);

        var rows = candidates
            .Where(s => wanted.Contains(new StockKey(s.DistributorId, s.ProductId, s.StockType)))
            .ToList();

        await StockLocking.RefreshModifiedAsync(db, rows, ct);
        return StockLocking.ToDictionary(rows);
    }
}
