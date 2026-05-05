using Microsoft.EntityFrameworkCore;
using sfa_api.Features.SalesTargets.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.SalesTargets.Repositories;

public class SalesTargetImportBatchRepository(AppDbContext context) : ISalesTargetImportBatchRepository
{
    private readonly AppDbContext _context = context;

    public async Task<long> GetNextBatchNumberAsync(CancellationToken ct = default)
    {
        var result = await _context.Database
            .SqlQueryRaw<long>("SELECT nextval('sales_target_batch_number_seq')")
            .ToListAsync(ct);
        return result[0];
    }

    public Task<SalesTargetImportBatch?> GetByIdAsync(int id, CancellationToken ct = default)
        => _context.SalesTargetImportBatches
            .AsNoTracking()
            .Include(b => b.Importer)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, ct);

    public async Task<(IEnumerable<SalesTargetImportBatch> Items, int TotalCount)> GetPagedAsync(
        int skip, int take, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);

        var query = _context.SalesTargetImportBatches
            .Where(b => !b.IsDeleted);

        var total = await query.CountAsync(ct);
        var items = await query
            .AsNoTracking()
            .Include(b => b.Importer)
            .OrderByDescending(b => b.ImportedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task CreateAsync(SalesTargetImportBatch batch, CancellationToken ct = default)
        => await _context.SalesTargetImportBatches.AddAsync(batch, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
