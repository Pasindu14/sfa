using Microsoft.EntityFrameworkCore;
using sfa_api.Features.PricingStructures.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.PricingStructures.Repositories;

public class PricingStructureRepository(AppDbContext context) : IPricingStructureRepository
{
    private readonly AppDbContext _context = context;

    public async Task<PricingStructure?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.PricingStructures.FindAsync([id], ct);

    public async Task<PricingStructure?> GetByIdWithItemsAsync(int id, CancellationToken ct = default)
        => await _context.PricingStructures
            .Include(ps => ps.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(ps => ps.Id == id, ct);

    public async Task<(IEnumerable<PricingStructure>, int TotalCount)> GetAllAsync(int skip, int take, string? search = null, CancellationToken ct = default)
    {
        var query = _context.PricingStructures
            .Include(ps => ps.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(ps => ps.Name.ToLower().Contains(search.ToLower()));

        var totalCount = await query.CountAsync(ct);
        var structures = await query
            .AsNoTracking()
            .OrderBy(ps => ps.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (structures, totalCount);
    }

    public async Task<PricingStructure?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _context.PricingStructures
            .AsNoTracking()
            .FirstOrDefaultAsync(ps => ps.Name.ToLower() == name.ToLower(), ct);

    public async Task<PricingStructure?> GetCurrentDefaultAsync(CancellationToken ct = default)
        => await _context.PricingStructures
            .FirstOrDefaultAsync(ps => ps.IsDefault && ps.IsActive, ct);

    public async Task<IEnumerable<PricingStructureItem>> GetItemsAsync(int pricingStructureId, CancellationToken ct = default)
        => await _context.PricingStructureItems
            .Include(i => i.Product)
            .AsNoTracking()
            .Where(i => i.PricingStructureId == pricingStructureId)
            .OrderBy(i => i.Product.ItemDescription)
            .ToListAsync(ct);

    public async Task<IEnumerable<PricingStructure>> GetAllActiveWithItemsAsync(CancellationToken ct = default)
        => await _context.PricingStructures
            .Include(ps => ps.Items.Where(i => i.Product.IsActive))
                .ThenInclude(i => i.Product)
            .AsNoTracking()
            .Where(ps => ps.IsActive)
            .OrderBy(ps => ps.Name)
            .ToListAsync(ct);

    public async Task CreateAsync(PricingStructure entity, CancellationToken ct = default)
        => await _context.PricingStructures.AddAsync(entity, ct);

    public Task UpdateAsync(PricingStructure entity, CancellationToken ct = default)
    {
        _context.PricingStructures.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.PricingStructures.FindAsync([id], ct);
        if (entity != null)
        {
            entity.IsActive = false;
            _context.PricingStructures.Update(entity);
        }
    }

    public async Task ActivateAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.PricingStructures.FindAsync([id], ct);
        if (entity != null)
        {
            entity.IsActive = true;
            _context.PricingStructures.Update(entity);
        }
    }

    public async Task BulkReplaceItemsAsync(int pricingStructureId, IEnumerable<PricingStructureItem> newItems, CancellationToken ct = default)
    {
        var existing = await _context.PricingStructureItems
            .Where(i => i.PricingStructureId == pricingStructureId)
            .ToListAsync(ct);

        _context.PricingStructureItems.RemoveRange(existing);
        await _context.PricingStructureItems.AddRangeAsync(newItems, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
