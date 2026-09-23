using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Stock.Repositories;

public class StockAdjustmentRepository(AppDbContext db) : IStockAdjustmentRepository
{
    private readonly AppDbContext _db = db;

    public Task<Distributor?> GetDistributorAsync(int distributorId, CancellationToken ct = default)
        => _db.Distributors
              .AsNoTracking()
              .FirstOrDefaultAsync(d => d.Id == distributorId, ct);

    public async Task<Dictionary<int, Product>> GetProductsAsync(IEnumerable<int> productIds, CancellationToken ct = default)
    {
        var ids = productIds.Distinct().ToList();
        return await _db.Products
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id) && !p.IsDeleted)
            .ToDictionaryAsync(p => p.Id, ct);
    }

    public async Task<(List<StockAdjustmentSummaryDto> Items, int TotalCount)> GetPagedAsync(
        int skip, int take, int? distributorId, CancellationToken ct = default)
    {
        // IgnoreQueryFilters: the distributor/user navigations are required, so their soft-delete
        // filters would turn into inner joins that silently drop historical adjustments.
        var query = _db.StockAdjustments.AsNoTracking().IgnoreQueryFilters().Where(a => !a.IsDeleted);
        if (distributorId.HasValue)
            query = query.Where(a => a.DistributorId == distributorId.Value);

        var total = await query.CountAsync(ct);

        // Line differences are pulled as a list and summed in memory: SQLite (integration tests)
        // cannot SUM decimals, and a page holds at most a few thousand small values.
        var rows = await query
            .OrderByDescending(a => a.AdjustedAt)
            .ThenByDescending(a => a.Id)
            .Skip(skip)
            .Take(take)
            .Select(a => new
            {
                a.Id,
                a.AdjustmentNumber,
                a.DistributorId,
                DistributorName = a.Distributor.Name,
                a.Reason,
                a.Notes,
                AdjustedByName = a.AdjustedByUser.Name,
                a.AdjustedAt,
                Differences = a.Lines.Select(l => l.Difference).ToList(),
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new StockAdjustmentSummaryDto(
            r.Id, r.AdjustmentNumber,
            r.DistributorId, r.DistributorName ?? string.Empty,
            r.Reason.ToString(), r.Notes, r.AdjustedByName ?? string.Empty, r.AdjustedAt,
            r.Differences.Count,
            r.Differences.Where(d => d > 0).Sum(),
            r.Differences.Where(d => d < 0).Sum(d => -d))).ToList();

        return (items, total);
    }

    public async Task<StockAdjustmentDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var a = await _db.StockAdjustments
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(x => x.Distributor)
            .Include(x => x.AdjustedByUser)
            .Include(x => x.Lines).ThenInclude(l => l.Product)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

        if (a is null) return null;

        var lines = a.Lines
            .OrderBy(l => l.Product?.Code)
            .ThenBy(l => l.StockType)
            .Select(l => new StockAdjustmentLineDto(
                l.Id, l.ProductId,
                l.Product?.Code ?? string.Empty,
                l.Product?.ItemDescription ?? string.Empty,
                l.StockType.ToString(),
                l.QuantityBefore,
                l.NewQuantity,
                l.Difference,
                l.Product?.PiecesPerPack ?? 0))
            .ToList();

        return new StockAdjustmentDto(
            a.Id, a.AdjustmentNumber,
            a.DistributorId, a.Distributor?.Name ?? string.Empty,
            a.Reason.ToString(), a.Notes, a.AdjustedByUser?.Name ?? string.Empty, a.AdjustedAt,
            lines.Count,
            lines.Where(l => l.Difference > 0).Sum(l => l.Difference),
            lines.Where(l => l.Difference < 0).Sum(l => -l.Difference),
            lines);
    }

    public async Task AddAsync(StockAdjustment adjustment, CancellationToken ct = default)
        => await _db.StockAdjustments.AddAsync(adjustment, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => _db.Database.BeginTransactionAsync(ct);
}
