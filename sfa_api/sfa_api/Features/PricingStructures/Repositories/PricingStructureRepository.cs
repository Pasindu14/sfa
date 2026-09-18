using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using sfa_api.Common.Errors;
using sfa_api.Features.PricingStructures.DTOs;
using sfa_api.Features.PricingStructures.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.PricingStructures.Repositories;

public class PricingStructureRepository(AppDbContext context) : IPricingStructureRepository
{
    private readonly AppDbContext _context = context;

    public Task<PricingStructure?> GetByIdAsync(int id, CancellationToken ct = default)
        => _context.PricingStructures.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);

    public async Task<(List<PricingStructureDto> Items, int TotalCount)> GetAllAsync(
        int skip, int take, string? search, bool? isActive, CancellationToken ct = default)
    {
        var query = _context.PricingStructures.AsNoTracking().Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term));
        }
        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.IsDefault)
            .ThenBy(s => s.Name)
            .Skip(skip)
            .Take(take)
            .Select(s => new PricingStructureDto(
                s.Id, s.Name, s.Description, s.IsDefault, s.IsActive,
                s.Items.Count(i => i.DealerPackPrice != null && !i.Product.IsDeleted),
                s.RowVersion, s.CreatedAt, s.UpdatedAt))
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<int> GetPricedCountAsync(int id, CancellationToken ct = default)
        => _context.PricingStructureItems
            .CountAsync(i => i.PricingStructureId == id && i.DealerPackPrice != null && !i.Product.IsDeleted, ct);

    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToLower();
        return _context.PricingStructures.AnyAsync(
            s => !s.IsDeleted && s.Name.ToLower() == normalized && (excludeId == null || s.Id != excludeId), ct);
    }

    public Task<PricingStructure?> GetDefaultAsync(CancellationToken ct = default)
        => _context.PricingStructures.FirstOrDefaultAsync(s => s.IsDefault && !s.IsDeleted, ct);

    public Task<int?> GetDefaultIdAsync(CancellationToken ct = default)
        => _context.PricingStructures.AsNoTracking()
            .Where(s => s.IsDefault && !s.IsDeleted)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(ct);

    public async Task<HashSet<int>> GetExistingIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return [];
        var found = await _context.PricingStructures.AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(ct);
        return found.ToHashSet();
    }

    public async Task<Dictionary<(int StructureId, int ProductId), PricingStructurePriceDto>> GetPricesAsync(
        IReadOnlyCollection<int> structureIds, IReadOnlyCollection<int> productIds, CancellationToken ct = default)
    {
        if (structureIds.Count == 0 || productIds.Count == 0) return [];
        var rows = await _context.PricingStructureItems.AsNoTracking()
            .Where(i => structureIds.Contains(i.PricingStructureId) && productIds.Contains(i.ProductId))
            .Select(i => new { i.PricingStructureId, i.ProductId, i.DealerPackPrice, i.DealerCasePrice, i.Mrp })
            .ToListAsync(ct);
        return rows.ToDictionary(
            r => (r.PricingStructureId, r.ProductId),
            r => new PricingStructurePriceDto(r.ProductId, r.DealerPackPrice, r.DealerCasePrice, r.Mrp));
    }

    public async Task AddAsync(PricingStructure structure, CancellationToken ct = default)
        => await _context.PricingStructures.AddAsync(structure, ct);

    // Sets the OriginalValue so EF sends WHERE xmin = <client token> (see ProductRepository).
    public void ApplyConcurrencyToken(PricingStructure structure, uint rowVersion)
        => _context.Entry(structure).Property(x => x.RowVersion).OriginalValue = rowVersion;

    public async Task<List<PricingStructureItemRowDto>> GetItemRowsAsync(int structureId, CancellationToken ct = default)
    {
        // Every non-deleted product LEFT JOIN this structure's item — the admin grid needs the
        // unpriced products too, so they can be priced. (StructureId, ProductId) is unique.
        return await (
                from p in _context.Products.AsNoTracking()
                where !p.IsDeleted
                join i in _context.PricingStructureItems.Where(x => x.PricingStructureId == structureId)
                    on p.Id equals i.ProductId into items
                from i in items.DefaultIfEmpty()
                orderby p.Code
                select new PricingStructureItemRowDto(
                    p.Id, p.Code, p.ItemDescription, p.PiecesPerPack, p.IsActive,
                    i == null ? null : i.DealerPackPrice,
                    i == null ? null : i.DealerCasePrice,
                    i == null ? null : i.Mrp))
            .ToListAsync(ct);
    }

    public Task<List<PricingStructureItem>> GetItemsForProductsAsync(
        int structureId, IReadOnlyCollection<int> productIds, CancellationToken ct = default)
        => _context.PricingStructureItems
            .Where(i => i.PricingStructureId == structureId && productIds.Contains(i.ProductId))
            .ToListAsync(ct);

    public Task<List<PricingStructureItem>> GetItemsAsync(int structureId, CancellationToken ct = default)
        => _context.PricingStructureItems.AsNoTracking()
            .Where(i => i.PricingStructureId == structureId)
            .ToListAsync(ct);

    public Task AddItemsAsync(IEnumerable<PricingStructureItem> items, CancellationToken ct = default)
        => _context.PricingStructureItems.AddRangeAsync(items, ct);

    public async Task<HashSet<int>> GetExistingProductIdsAsync(IReadOnlyCollection<int> productIds, CancellationToken ct = default)
    {
        var found = await _context.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(ct);
        return found.ToHashSet();
    }

    public async Task<DefaultPricingStructurePricesDto?> GetDefaultPricesAsync(CancellationToken ct = default)
    {
        var structure = await _context.PricingStructures.AsNoTracking()
            .Where(s => s.IsDefault && !s.IsDeleted)
            .Select(s => new { s.Id, s.Name })
            .FirstOrDefaultAsync(ct);
        if (structure is null) return null;

        var items = await _context.PricingStructureItems.AsNoTracking()
            .Where(i => i.PricingStructureId == structure.Id && i.DealerPackPrice != null
                        && i.Product.IsActive && !i.Product.IsDeleted)
            .Select(i => new PricingStructurePriceDto(i.ProductId, i.DealerPackPrice, i.DealerCasePrice, i.Mrp))
            .ToListAsync(ct);

        return new DefaultPricingStructurePricesDto(structure.Id, structure.Name, items);
    }

    public IExecutionStrategy CreateExecutionStrategy() => _context.Database.CreateExecutionStrategy();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => _context.Database.BeginTransactionAsync(ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }
    }
}
