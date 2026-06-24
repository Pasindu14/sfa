using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Services;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Stock.Repositories;

public class StockReconciliationRepository(AppDbContext db) : IStockReconciliationRepository
{
    private readonly AppDbContext _db = db;

    public async Task<List<StockReconciliation.LedgerNet>> GetLedgerNetsAsync(
        int? distributorId, int? productId, CancellationToken ct = default)
    {
        var q = _db.StockTransactions.AsNoTracking().AsQueryable();
        if (distributorId.HasValue) q = q.Where(t => t.DistributorId == distributorId.Value);
        if (productId.HasValue)     q = q.Where(t => t.ProductId == productId.Value);

        // SUM(CASE WHEN Direction = In THEN Quantity ELSE 0 END) — PostgreSQL only; the SQLite test
        // provider can't translate SUM over decimal (same limitation the BinCard data path hits).
        var raw = await q
            .GroupBy(t => new { t.DistributorId, t.ProductId, t.StockType })
            .Select(g => new
            {
                g.Key.DistributorId,
                g.Key.ProductId,
                g.Key.StockType,
                TotalIn  = g.Sum(x => x.Direction == StockTransactionDirection.In  ? x.Quantity : 0m),
                TotalOut = g.Sum(x => x.Direction == StockTransactionDirection.Out ? x.Quantity : 0m),
            })
            .ToListAsync(ct);

        return raw
            .Select(r => new StockReconciliation.LedgerNet(
                new StockReconciliation.Key(r.DistributorId, r.ProductId, r.StockType),
                r.TotalIn, r.TotalOut))
            .ToList();
    }

    public async Task<List<StockReconciliation.Snapshot>> GetLatestSnapshotsAsync(
        int? distributorId, int? productId, CancellationToken ct = default)
    {
        var q = _db.StockTransactions.AsNoTracking().AsQueryable();
        if (distributorId.HasValue) q = q.Where(t => t.DistributorId == distributorId.Value);
        if (productId.HasValue)     q = q.Where(t => t.ProductId == productId.Value);

        // Greatest Id per group == most recent movement (Id is monotonic with posting order).
        // MAX(Id) + a select of QuantityAfter — no decimal aggregate, so this runs under SQLite too.
        var latestIds = await q
            .GroupBy(t => new { t.DistributorId, t.ProductId, t.StockType })
            .Select(g => g.Max(x => x.Id))
            .ToListAsync(ct);

        if (latestIds.Count == 0) return [];

        var rows = await _db.StockTransactions
            .AsNoTracking()
            .Where(t => latestIds.Contains(t.Id))
            .Select(t => new { t.DistributorId, t.ProductId, t.StockType, t.QuantityAfter })
            .ToListAsync(ct);

        return rows
            .Select(r => new StockReconciliation.Snapshot(
                new StockReconciliation.Key(r.DistributorId, r.ProductId, r.StockType),
                r.QuantityAfter))
            .ToList();
    }

    public async Task<Dictionary<StockReconciliation.Key, decimal>> GetOnHandAsync(
        int? distributorId, int? productId, CancellationToken ct = default)
    {
        var q = _db.DistributorStocks.AsNoTracking().AsQueryable();
        if (distributorId.HasValue) q = q.Where(s => s.DistributorId == distributorId.Value);
        if (productId.HasValue)     q = q.Where(s => s.ProductId == productId.Value);

        var rows = await q
            .Select(s => new { s.DistributorId, s.ProductId, s.StockType, s.QuantityOnHand })
            .ToListAsync(ct);

        return rows.ToDictionary(
            r => new StockReconciliation.Key(r.DistributorId, r.ProductId, r.StockType),
            r => r.QuantityOnHand);
    }

    public async Task<StockReconciliationRun> SaveRunAsync(StockReconciliationRun run, CancellationToken ct = default)
    {
        _db.StockReconciliationRuns.Add(run);   // flags cascade-insert via the navigation
        await _db.SaveChangesAsync(ct);
        return run;
    }

    public async Task<StockReconciliationRun?> GetLatestRunAsync(CancellationToken ct = default)
        => await _db.StockReconciliationRuns
            .AsNoTracking()
            .Include(r => r.Flags)
            .OrderByDescending(r => r.RunAt)
            .ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync(ct);

    public async Task<(Dictionary<int, string> DistributorNames, Dictionary<int, string> ProductCodes)> GetNamesAsync(
        IEnumerable<int> distributorIds, IEnumerable<int> productIds, CancellationToken ct = default)
    {
        var dIds = distributorIds.Distinct().ToList();
        var pIds = productIds.Distinct().ToList();

        var distributorNames = dIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Distributors.AsNoTracking().IgnoreQueryFilters()
                .Where(d => dIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        var productCodes = pIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Products.AsNoTracking().IgnoreQueryFilters()
                .Where(p => pIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Code, ct);

        return (distributorNames, productCodes);
    }

    public async Task<int> PurgeRunsBeforeAsync(DateTime cutoff, CancellationToken ct = default)
        => await _db.StockReconciliationRuns
            .Where(r => r.RunAt < cutoff)
            .ExecuteDeleteAsync(ct);   // flags removed by DB ON DELETE CASCADE
}
