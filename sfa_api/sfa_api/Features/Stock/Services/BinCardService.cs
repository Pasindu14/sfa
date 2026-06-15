using sfa_api.Common.Errors;
using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Features.Stock.Requests;

namespace sfa_api.Features.Stock.Services;

/// <summary>
/// Assembles the per-SKU bin card from the stock ledger.
///
/// End Stock = Open
///           + Invoice (GRN) + Market Resaleable (returns) + Deleted Inv (net reversals) ± Stock Adjustment
///           − Sold − Free Issues − Company Free Issues
///
/// Because every column except Rep-Return is summed from the same ledger that maintains the live
/// balance, End Stock equals the ledger's true closing balance — the report reconciles by construction.
/// Rep Return (Damage/Expire) is informational only and never affects End Stock.
/// </summary>
public class BinCardService(IBinCardRepository repo) : IBinCardService
{
    private readonly IBinCardRepository _repo = repo;

    public async Task<BinCardResponseDto> GetBinCardAsync(BinCardQuery query, CancellationToken ct = default)
    {
        var distributorName = await _repo.GetDistributorNameAsync(query.DistributorId, ct)
            ?? throw new NotFoundException("Distributor", query.DistributorId);

        // Inclusive business dates → UTC half-open window [from 00:00, to+1 00:00).
        var fromUtc        = query.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toExclusiveUtc = query.To.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var movements  = await _repo.GetBinCardMovementsAsync(query.DistributorId, fromUtc, toExclusiveUtc, ct);
        var openings   = await _repo.GetBinCardOpeningAsync(query.DistributorId, fromUtc, ct);
        var repReturns = await _repo.GetBinCardRepReturnsAsync(query.DistributorId, query.From, query.To, ct);
        var counts     = await _repo.GetBinCardLatestCountsAsync(query.DistributorId, toExclusiveUtc, ct);

        // Every product that appears in any source becomes a row.
        var productIds = movements.Select(m => m.ProductId)
            .Concat(openings.Select(o => o.ProductId))
            .Concat(repReturns.Select(r => r.ProductId))
            .Concat(counts.Select(c => c.ProductId))
            .Distinct()
            .ToList();

        if (productIds.Count == 0)
            return new BinCardResponseDto(query.DistributorId, distributorName, query.From, query.To, 0, [], EmptyTotals());

        var products     = (await _repo.GetBinCardProductsAsync(productIds, ct)).ToDictionary(p => p.ProductId);
        var openingMap   = openings.ToDictionary(o => o.ProductId, o => o.OpeningQuantity);
        var repReturnMap = repReturns.ToDictionary(r => r.ProductId, r => r.Quantity);
        var countMap     = counts.ToDictionary(c => c.ProductId, c => c.CountedQuantity);
        var movesByProduct = movements.GroupBy(m => m.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<BinCardRowDto>(productIds.Count);
        foreach (var pid in productIds)
        {
            products.TryGetValue(pid, out var info);
            var price = info?.DealerPackPrice ?? 0m;
            var ms = movesByProduct.TryGetValue(pid, out var list) ? list : [];

            decimal SumType(StockTransactionType t) =>
                ms.Where(m => m.TransactionType == t).Sum(m => m.Quantity);
            decimal NetType(StockTransactionType t) =>
                ms.Where(m => m.TransactionType == t && m.Direction == StockTransactionDirection.In).Sum(m => m.Quantity)
              - ms.Where(m => m.TransactionType == t && m.Direction == StockTransactionDirection.Out).Sum(m => m.Quantity);
            decimal FreeIssue(StockType pool) =>
                ms.Where(m => m.TransactionType == StockTransactionType.FreeIssue && m.StockType == pool).Sum(m => m.Quantity);

            var open              = openingMap.GetValueOrDefault(pid, 0m);
            var invoice           = SumType(StockTransactionType.GRNReceipt);
            var marketResaleable  = SumType(StockTransactionType.Return);
            var deletedInv        = NetType(StockTransactionType.BillingReversal);
            var stockAdjustment   = NetType(StockTransactionType.StockTakingAdjustment);
            var sold              = SumType(StockTransactionType.Sale);
            var freeIssues        = FreeIssue(StockType.Normal);
            var companyFreeIssues = FreeIssue(StockType.FreeIssue);
            var repReturn         = repReturnMap.GetValueOrDefault(pid, 0m);

            var endStock = open + invoice + marketResaleable + deletedInv + stockAdjustment
                         - sold - freeIssues - companyFreeIssues;
            var closingValue = endStock * price;

            decimal? currentStock = countMap.TryGetValue(pid, out var c) ? c : null;
            decimal? variance     = currentStock.HasValue ? currentStock.Value - endStock : null;

            rows.Add(new BinCardRowDto(
                info?.Code ?? $"#{pid}",
                info?.Description ?? string.Empty,
                price, open, invoice, marketResaleable, deletedInv, stockAdjustment,
                sold, freeIssues, companyFreeIssues, repReturn, endStock,
                currentStock, closingValue, variance));
        }

        rows = rows.OrderBy(r => r.ItemCode, StringComparer.OrdinalIgnoreCase).ToList();

        var totals = new BinCardTotalsDto(
            rows.Sum(r => r.OpenStock),
            rows.Sum(r => r.InvoiceQuantity),
            rows.Sum(r => r.MarketResaleable),
            rows.Sum(r => r.DeletedInv),
            rows.Sum(r => r.StockAdjustment),
            rows.Sum(r => r.SoldQty),
            rows.Sum(r => r.FreeIssues),
            rows.Sum(r => r.CompanyFreeIssues),
            rows.Sum(r => r.RepReturnQtyDE),
            rows.Sum(r => r.EndStock),
            rows.Sum(r => r.ClosingStockValue));

        return new BinCardResponseDto(
            query.DistributorId, distributorName, query.From, query.To, rows.Count, rows, totals);
    }

    private static BinCardTotalsDto EmptyTotals() =>
        new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}
