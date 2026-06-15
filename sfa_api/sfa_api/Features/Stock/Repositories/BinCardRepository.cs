using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.StockTaking.Enums;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Stock.Repositories;

public class BinCardRepository(AppDbContext db) : IBinCardRepository
{
    private readonly AppDbContext _db = db;

    public async Task<List<BinCardMovementAgg>> GetBinCardMovementsAsync(
        int distributorId, DateTime fromUtc, DateTime toExclusiveUtc, CancellationToken ct = default)
    {
        // Project grouped aggregates into an anonymous type in SQL, then map to the record
        // in memory — EF Core cannot bind a GroupBy result directly to a positional ctor.
        var raw = await _db.StockTransactions
            .AsNoTracking()
            .Where(t => t.DistributorId == distributorId
                     && t.TransactedAt >= fromUtc
                     && t.TransactedAt <  toExclusiveUtc)
            .GroupBy(t => new { t.ProductId, t.TransactionType, t.StockType, t.Direction })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.TransactionType,
                g.Key.StockType,
                g.Key.Direction,
                Quantity = g.Sum(x => x.Quantity),
            })
            .ToListAsync(ct);

        return raw
            .Select(r => new BinCardMovementAgg(r.ProductId, r.TransactionType, r.StockType, r.Direction, r.Quantity))
            .ToList();
    }

    public async Task<List<BinCardOpeningAgg>> GetBinCardOpeningAsync(
        int distributorId, DateTime fromUtc, CancellationToken ct = default)
    {
        // Step 1: the latest ledger row id per (product, pool) strictly before the window.
        // Id is monotonic with posting order, so MAX(Id) == the most recent movement.
        var latestIds = await _db.StockTransactions
            .AsNoTracking()
            .Where(t => t.DistributorId == distributorId && t.TransactedAt < fromUtc)
            .GroupBy(t => new { t.ProductId, t.StockType })
            .Select(g => g.Max(x => x.Id))
            .ToListAsync(ct);

        if (latestIds.Count == 0) return [];

        // Step 2: take each pool's closing balance (QuantityAfter) and combine per product.
        var raw = await _db.StockTransactions
            .AsNoTracking()
            .Where(t => latestIds.Contains(t.Id))
            .GroupBy(t => t.ProductId)
            .Select(g => new { ProductId = g.Key, Opening = g.Sum(x => x.QuantityAfter) })
            .ToListAsync(ct);

        return raw.Select(r => new BinCardOpeningAgg(r.ProductId, r.Opening)).ToList();
    }

    public async Task<List<BinCardRepReturnAgg>> GetBinCardRepReturnsAsync(
        int distributorId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var raw = await _db.BillingItems
            .AsNoTracking()
            .Where(bi => !bi.IsDeleted
                      && bi.BillingItemType == BillingItemType.Return
                      && (bi.ReturnType == ReturnType.Damage || bi.ReturnType == ReturnType.Expire)
                      && bi.Billing.DistributorId == distributorId
                      && bi.Billing.BillingDate >= from
                      && bi.Billing.BillingDate <= to
                      && bi.Billing.RepStatus         != RepBillingStatus.Cancelled
                      && bi.Billing.DistributorStatus != DistributorBillingStatus.Rejected)
            .GroupBy(bi => bi.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToListAsync(ct);

        return raw.Select(r => new BinCardRepReturnAgg(r.ProductId, r.Quantity)).ToList();
    }

    public async Task<List<BinCardCountAgg>> GetBinCardLatestCountsAsync(
        int distributorId, DateTime asOfExclusiveUtc, CancellationToken ct = default)
    {
        // The most recent submitted stock-take for this distributor on or before the To date.
        var latestSubmissionId = await _db.StockTakingSubmissions
            .AsNoTracking()
            .Where(s => s.DistributorId == distributorId
                     && !s.IsDeleted
                     && s.Status == StockTakingSubmissionStatus.Submitted
                     && s.SubmittedAt != null
                     && s.SubmittedAt < asOfExclusiveUtc)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(ct);

        if (latestSubmissionId == 0) return [];

        var raw = await _db.StockTakingLines
            .AsNoTracking()
            .Where(l => l.StockTakingSubmissionId == latestSubmissionId)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Counted = g.Sum(x => x.CountedQuantity) })
            .ToListAsync(ct);

        return raw.Select(r => new BinCardCountAgg(r.ProductId, r.Counted)).ToList();
    }

    public Task<List<BinCardProductInfo>> GetBinCardProductsAsync(
        IReadOnlyCollection<int> productIds, CancellationToken ct = default)
        => _db.Products
              .AsNoTracking()
              .Where(p => productIds.Contains(p.Id))
              .Select(p => new BinCardProductInfo(p.Id, p.Code, p.ItemDescription, p.DealerPackPrice))
              .ToListAsync(ct);

    public Task<string?> GetDistributorNameAsync(int distributorId, CancellationToken ct = default)
        => _db.Distributors
              .AsNoTracking()
              .Where(d => d.Id == distributorId)
              .Select(d => (string?)d.Name)
              .FirstOrDefaultAsync(ct);
}
