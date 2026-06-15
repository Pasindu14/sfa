using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
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

    public Task<List<DistributorStock>> GetAllStockByDistributorAsync(int distributorId, CancellationToken ct = default)
        => _db.DistributorStocks
              .AsNoTracking()
              .Include(x => x.Product)
              .Include(x => x.Distributor)
              .Where(x => x.DistributorId == distributorId)
              .OrderBy(x => x.Product.Code)
              .ToListAsync(ct);

    public Task<List<StockTransaction>> GetTransactionsByDistributorAndProductAsync(
        int distributorId, int productId, int page, int pageSize, CancellationToken ct = default)
    {
        var (_, size, skip) = PaginationHelper.Normalize(page, pageSize);
        return _db.StockTransactions
              .AsNoTracking()
              .Include(x => x.Product)
              .Where(x => x.DistributorId == distributorId && x.ProductId == productId)
              .OrderByDescending(x => x.TransactedAt)
              .Skip(skip)
              .Take(size)
              .ToListAsync(ct);
    }

    public Task<int> GetTransactionCountAsync(int distributorId, int productId, CancellationToken ct = default)
        => _db.StockTransactions
              .CountAsync(x => x.DistributorId == distributorId && x.ProductId == productId, ct);

    /// <inheritdoc/>
    public async Task DeductStockAsync(
        int distributorId,
        int productId,
        decimal quantity,
        StockType stockType,
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
            .FirstOrDefaultAsync(x => x.DistributorId == distributorId && x.ProductId == productId && x.StockType == stockType, ct)
            ?? throw new NotFoundException("DistributorStock", $"distributor={distributorId}/product={productId}/stockType={stockType}");

        var quantityBefore = stock.QuantityOnHand;
        var quantityAfter  = quantityBefore - quantity;

        // ── Negative-stock guard ──────────────────────────────────────────
        if (quantityAfter < 0)
            throw new BusinessRuleException(
                "INSUFFICIENT_STOCK",
                $"Insufficient {stockType} stock for product {productId}: requested {quantity}, available {quantityBefore}.",
                new { productId, stockType, requested = quantity, available = quantityBefore });

        // Update running balance
        stock.QuantityOnHand = quantityAfter;
        stock.LastUpdatedAt  = DateTime.UtcNow;

        // Append immutable ledger entry — never update or delete
        _db.StockTransactions.Add(new StockTransaction
        {
            DistributorId   = distributorId,
            ProductId       = productId,
            StockType       = stockType,
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

    /// <inheritdoc/>
    public async Task<DistributorStock?> GetStockForUpdateAsync(
        int distributorId, int productId, StockType stockType, CancellationToken ct = default)
    {
        var ids = await _db.Database
            .SqlQueryRaw<int>(
                "SELECT \"Id\" FROM \"DistributorStocks\" WHERE \"DistributorId\" = {0} AND \"ProductId\" = {1} AND \"StockType\" = {2} FOR UPDATE",
                distributorId, productId, stockType.ToString())
            .ToListAsync(ct);

        if (ids.Count == 0) return null;

        return await _db.DistributorStocks
            .FirstOrDefaultAsync(x => x.Id == ids[0], ct);
    }

    /// <inheritdoc/>
    public async Task CreditStockAsync(
        int distributorId,
        int productId,
        decimal quantity,
        StockType stockType,
        StockTransactionType transactionType,
        string referenceType,
        int referenceId,
        int transactedBy,
        string? notes = null,
        CancellationToken ct = default)
    {
        var stock = await _db.DistributorStocks
            .FirstOrDefaultAsync(x => x.DistributorId == distributorId && x.ProductId == productId && x.StockType == stockType, ct);

        if (stock is null)
        {
            // First time this distributor holds this product/stockType — start at zero.
            // (e.g. a positive stock-take adjustment for a never-before-held product.)
            stock = new DistributorStock
            {
                DistributorId  = distributorId,
                ProductId      = productId,
                StockType      = stockType,
                QuantityOnHand = 0m,
                LastUpdatedAt  = DateTime.UtcNow
            };
            _db.DistributorStocks.Add(stock);
        }

        var quantityBefore = stock.QuantityOnHand;
        var quantityAfter  = quantityBefore + quantity;

        stock.QuantityOnHand = quantityAfter;
        stock.LastUpdatedAt  = DateTime.UtcNow;

        _db.StockTransactions.Add(new StockTransaction
        {
            DistributorId   = distributorId,
            ProductId       = productId,
            StockType       = stockType,
            TransactionType = transactionType,
            Direction       = StockTransactionDirection.In,
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
