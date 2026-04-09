using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Errors;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Stock.Repositories;

public class StockRepository(AppDbContext db) : IStockRepository
{
    private readonly AppDbContext _db = db;

    public async Task<(List<DistributorStock> Items, int TotalCount)> GetStockByDistributorAsync(
        int distributorId, int skip, int take, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var query = _db.DistributorStocks
            .Where(x => x.DistributorId == distributorId);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.Distributor)
            .OrderBy(x => x.Product.Code)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<List<StockTransaction>> GetTransactionsByDistributorAndProductAsync(
        int distributorId, int productId, int page, int pageSize, CancellationToken ct = default)
        => _db.StockTransactions
              .AsNoTracking()
              .Include(x => x.Product)
              .Where(x => x.DistributorId == distributorId && x.ProductId == productId)
              .OrderByDescending(x => x.TransactedAt)
              .Skip((page - 1) * pageSize)
              .Take(pageSize)
              .ToListAsync(ct);

    public Task<int> GetTransactionCountAsync(int distributorId, int productId, CancellationToken ct = default)
        => _db.StockTransactions
              .CountAsync(x => x.DistributorId == distributorId && x.ProductId == productId, ct);

    /// <inheritdoc/>
    public async Task DeductStockAsync(
        int distributorId,
        int productId,
        decimal quantity,
        StockTransactionType transactionType,
        string referenceType,
        int referenceId,
        int transactedBy,
        string? notes = null,
        CancellationToken ct = default)
    {
        // Acquire a tracked, row-locked copy of the stock balance.
        // This must be called inside a transaction that holds SELECT … FOR UPDATE.
        var stock = await _db.DistributorStocks
            .FirstOrDefaultAsync(x => x.DistributorId == distributorId && x.ProductId == productId, ct)
            ?? throw new NotFoundException("DistributorStock", $"distributor={distributorId}/product={productId}");

        var quantityBefore = stock.QuantityOnHand;
        var quantityAfter  = quantityBefore - quantity;

        // ── Negative-stock guard ──────────────────────────────────────────
        if (quantityAfter < 0)
            throw new BusinessRuleException(
                "INSUFFICIENT_STOCK",
                $"Insufficient stock for product {productId}: requested {quantity}, available {quantityBefore}.",
                new { productId, requested = quantity, available = quantityBefore });

        // Update running balance
        stock.QuantityOnHand = quantityAfter;
        stock.LastUpdatedAt  = DateTime.UtcNow;

        // Append immutable ledger entry — never update or delete
        _db.StockTransactions.Add(new StockTransaction
        {
            DistributorId   = distributorId,
            ProductId       = productId,
            TransactionType = transactionType,
            Direction       = StockTransactionDirection.Out,
            Quantity        = quantity,
            QuantityBefore  = quantityBefore,
            QuantityAfter   = quantityAfter,
            ReferenceType   = referenceType,
            ReferenceId     = referenceId,
            TransactedAt    = DateTime.UtcNow,
            TransactedBy    = transactedBy,
            Notes           = notes
        });
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
