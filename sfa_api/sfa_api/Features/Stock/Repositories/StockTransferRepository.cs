using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Stock.Repositories;

public class StockTransferRepository(AppDbContext db) : IStockTransferRepository
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

    public async Task<(List<StockTransferSummaryDto> Items, int TotalCount)> GetPagedAsync(
        int skip, int take, int? distributorId, CancellationToken ct = default)
    {
        // IgnoreQueryFilters: the distributor/user navigations are required, so their soft-delete
        // filters would turn into inner joins that silently drop historical transfers.
        var query = _db.StockTransfers.AsNoTracking().IgnoreQueryFilters().Where(t => !t.IsDeleted);
        if (distributorId.HasValue)
            query = query.Where(t => t.SourceDistributorId == distributorId.Value
                                  || t.TargetDistributorId == distributorId.Value);

        var total = await query.CountAsync(ct);

        // Line quantities are pulled as a list and summed in memory: SQLite (integration tests)
        // cannot SUM decimals, and a page holds at most a few thousand small values.
        var rows = await query
            .OrderByDescending(t => t.TransferredAt)
            .ThenByDescending(t => t.Id)
            .Skip(skip)
            .Take(take)
            .Select(t => new
            {
                t.Id,
                t.TransferNumber,
                t.SourceDistributorId,
                SourceDistributorName = t.SourceDistributor.Name,
                t.TargetDistributorId,
                TargetDistributorName = t.TargetDistributor.Name,
                t.Notes,
                TransferredByName = t.TransferredByUser.Name,
                t.TransferredAt,
                Quantities = t.Lines.Select(l => l.Quantity).ToList(),
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new StockTransferSummaryDto(
            r.Id, r.TransferNumber,
            r.SourceDistributorId, r.SourceDistributorName ?? string.Empty,
            r.TargetDistributorId, r.TargetDistributorName ?? string.Empty,
            r.Notes, r.TransferredByName ?? string.Empty, r.TransferredAt,
            r.Quantities.Count, r.Quantities.Sum())).ToList();

        return (items, total);
    }

    public async Task<StockTransferDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var t = await _db.StockTransfers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(x => x.SourceDistributor)
            .Include(x => x.TargetDistributor)
            .Include(x => x.TransferredByUser)
            .Include(x => x.Lines).ThenInclude(l => l.Product)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

        if (t is null) return null;

        var lines = t.Lines
            .OrderBy(l => l.Product?.Code)
            .ThenBy(l => l.StockType)
            .Select(l => new StockTransferLineDto(
                l.Id, l.ProductId,
                l.Product?.Code ?? string.Empty,
                l.Product?.ItemDescription ?? string.Empty,
                l.StockType.ToString(),
                l.Quantity,
                l.Product?.PiecesPerPack ?? 0))
            .ToList();

        return new StockTransferDto(
            t.Id, t.TransferNumber,
            t.SourceDistributorId, t.SourceDistributor?.Name ?? string.Empty,
            t.TargetDistributorId, t.TargetDistributor?.Name ?? string.Empty,
            t.Notes, t.TransferredByUser?.Name ?? string.Empty, t.TransferredAt,
            lines.Count, lines.Sum(l => l.Quantity), lines);
    }

    public async Task AddAsync(StockTransfer transfer, CancellationToken ct = default)
        => await _db.StockTransfers.AddAsync(transfer, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => _db.Database.BeginTransactionAsync(ct);
}
