using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Stock.Repositories;

/// <summary>Identifies one <see cref="DistributorStock"/> balance row (the table's unique key).</summary>
public readonly record struct StockKey(int DistributorId, int ProductId, StockType StockType);

/// <summary>
/// Batched pessimistic locking for <see cref="DistributorStock"/> rows, shared by the stock and GRN
/// repositories.
/// </summary>
public static class StockLocking
{
    /// <summary>
    /// Locks every existing row matching <paramref name="keys"/> with a single
    /// <c>SELECT … ORDER BY "Id" FOR UPDATE</c> and returns them as EF-tracked entities, keyed by
    /// <see cref="StockKey"/>. Keys with no row are simply absent from the result.
    /// <para>
    /// Rows are locked in primary-key order, so two transactions touching overlapping product sets
    /// always acquire their locks in the same order and cannot deadlock on each other.
    /// Must be called inside an explicit transaction. PostgreSQL only — integration tests substitute
    /// a plain LINQ load (SQLite has no FOR UPDATE / unnest).
    /// </para>
    /// </summary>
    public static async Task<Dictionary<StockKey, DistributorStock>> LockForUpdateAsync(
        AppDbContext db, IEnumerable<StockKey> keys, CancellationToken ct)
    {
        var distinct = keys.Distinct().ToList();
        if (distinct.Count == 0) return new Dictionary<StockKey, DistributorStock>();

        var distributorIds = distinct.Select(k => k.DistributorId).ToArray();
        var productIds     = distinct.Select(k => k.ProductId).ToArray();
        var stockTypes     = distinct.Select(k => k.StockType.ToString()).ToArray();   // stored as the enum member name

        // Not composed with further LINQ, so EF sends this SQL verbatim (FOR UPDATE stays top-level).
        // The Sort node sits below LockRows in the plan, so rows are locked in "Id" order.
        var rows = await db.DistributorStocks
            .FromSqlRaw(
                """
                SELECT s.* FROM "DistributorStocks" AS s
                JOIN unnest({0}::integer[], {1}::integer[], {2}::text[]) AS k("DistributorId", "ProductId", "StockType")
                  ON s."DistributorId" = k."DistributorId"
                 AND s."ProductId"     = k."ProductId"
                 AND s."StockType"     = k."StockType"
                ORDER BY s."Id"
                FOR UPDATE OF s
                """,
                distributorIds, productIds, stockTypes)
            .ToListAsync(ct);

        await RefreshModifiedAsync(db, rows, ct);
        return ToDictionary(rows);
    }

    /// <summary>
    /// An execution-strategy retry re-runs the transaction body against the same DbContext, so a row
    /// mutated by the failed attempt would still carry that attempt's in-memory balance (EF keeps the
    /// tracked instance's values on re-query). Reload those so the retry starts from the locked,
    /// committed balance. A no-op on the first attempt.
    /// </summary>
    public static async Task RefreshModifiedAsync(AppDbContext db, IEnumerable<DistributorStock> rows, CancellationToken ct)
    {
        foreach (var row in rows)
        {
            var entry = db.Entry(row);
            if (entry.State == EntityState.Modified)
                await entry.ReloadAsync(ct);
        }
    }

    public static Dictionary<StockKey, DistributorStock> ToDictionary(IEnumerable<DistributorStock> rows)
        => rows.ToDictionary(s => new StockKey(s.DistributorId, s.ProductId, s.StockType));
}
