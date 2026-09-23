using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Enums;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Stock.Repositories;

public class StockActivityRepository(AppDbContext db) : IStockActivityRepository
{
    // ReferenceType values written by the services that post to the ledger.
    public const string GrnReference             = "GRN";
    public const string BillingReference         = "Billing";
    public const string StockTransferReference   = "StockTransfer";
    public const string StockAdjustmentReference = "StockAdjustment";

    private readonly AppDbContext _db = db;

    public async Task<(List<StockActivityDto> Items, int TotalCount)> GetPagedAsync(
        DateTime fromUtc, DateTime toExclusiveUtc,
        int? distributorId, int? productId, int? userId,
        StockTransactionType? transactionType, StockTransactionDirection? direction,
        int skip, int take, CancellationToken ct = default)
    {
        // Audit log: IgnoreQueryFilters so rows of soft-deleted distributors/products/users are
        // not dropped by the required-navigation inner joins.
        var query = _db.StockTransactions
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(t => t.TransactedAt >= fromUtc && t.TransactedAt < toExclusiveUtc);

        if (distributorId.HasValue)   query = query.Where(t => t.DistributorId == distributorId.Value);
        if (productId.HasValue)       query = query.Where(t => t.ProductId == productId.Value);
        if (userId.HasValue)          query = query.Where(t => t.TransactedBy == userId.Value);
        if (transactionType.HasValue) query = query.Where(t => t.TransactionType == transactionType.Value);
        if (direction.HasValue)       query = query.Where(t => t.Direction == direction.Value);

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(t => t.TransactedAt)
            .ThenByDescending(t => t.Id)
            .Skip(skip)
            .Take(take)
            .Select(t => new
            {
                t.Id,
                t.TransactedAt,
                t.TransactedBy,
                TransactedByName = t.TransactedByUser.Name,
                t.DistributorId,
                DistributorName = t.Distributor.Name,
                t.ProductId,
                ProductCode = t.Product.Code,
                ProductDescription = t.Product.ItemDescription,
                t.Product.PiecesPerPack,
                t.StockType,
                t.TransactionType,
                t.Direction,
                t.Quantity,
                t.QuantityBefore,
                t.QuantityAfter,
                t.ReferenceType,
                t.ReferenceId,
                t.Notes,
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new StockActivityDto(
            r.Id, r.TransactedAt,
            r.TransactedBy, r.TransactedByName ?? string.Empty,
            r.DistributorId, r.DistributorName ?? string.Empty,
            r.ProductId, r.ProductCode ?? string.Empty, r.ProductDescription ?? string.Empty, r.PiecesPerPack,
            r.StockType.ToString(), r.TransactionType.ToString(), r.Direction.ToString(),
            r.Quantity, r.QuantityBefore, r.QuantityAfter,
            r.ReferenceType, r.ReferenceId, null, r.Notes)).ToList();

        return (items, total);
    }

    public async Task<Dictionary<(string ReferenceType, int ReferenceId), string>> GetReferenceNumbersAsync(
        IEnumerable<(string ReferenceType, int ReferenceId)> references, CancellationToken ct = default)
    {
        var result = new Dictionary<(string ReferenceType, int ReferenceId), string>();

        foreach (var group in references.Distinct().GroupBy(r => r.ReferenceType))
        {
            var ids = group.Select(r => r.ReferenceId).ToList();

            // One batched query per reference type; soft-deleted documents still resolve.
            var numbers = group.Key switch
            {
                GrnReference => await _db.GRNs.AsNoTracking().IgnoreQueryFilters()
                    .Where(x => ids.Contains(x.Id))
                    .Select(x => new ReferenceNumber(x.Id, x.GrnNumber)).ToListAsync(ct),
                BillingReference => await _db.Billings.AsNoTracking().IgnoreQueryFilters()
                    .Where(x => ids.Contains(x.Id))
                    .Select(x => new ReferenceNumber(x.Id, x.BillingNumber)).ToListAsync(ct),
                StockTransferReference => await _db.StockTransfers.AsNoTracking().IgnoreQueryFilters()
                    .Where(x => ids.Contains(x.Id))
                    .Select(x => new ReferenceNumber(x.Id, x.TransferNumber)).ToListAsync(ct),
                StockAdjustmentReference => await _db.StockAdjustments.AsNoTracking().IgnoreQueryFilters()
                    .Where(x => ids.Contains(x.Id))
                    .Select(x => new ReferenceNumber(x.Id, x.AdjustmentNumber)).ToListAsync(ct),
                _ => [],
            };

            foreach (var n in numbers)
                result[(group.Key, n.Id)] = n.Number;
        }

        return result;
    }

    public Task<List<StockActivityUserDto>> GetUsersAsync(CancellationToken ct = default)
        => _db.Users
              .AsNoTracking()
              .IgnoreQueryFilters()
              .Where(u => _db.StockTransactions.Any(t => t.TransactedBy == u.Id))
              .OrderBy(u => u.Name)
              .ThenBy(u => u.Id)
              .Select(u => new StockActivityUserDto(u.Id, u.Name))
              .ToListAsync(ct);

    private sealed record ReferenceNumber(int Id, string Number);
}
