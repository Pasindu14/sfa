using Microsoft.EntityFrameworkCore;
using sfa_api.Features.MobileSync.DTOs;
using sfa_api.Features.PricingStructures;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.MobileSync.Repositories;

public class MobileSyncRepository(AppDbContext db) : IMobileSyncRepository
{
    private readonly AppDbContext _db = db;

    /// <summary>
    /// Hard safety ceiling on the catalog sync response. Far above any realistic catalog
    /// size; bounds memory/payload so a runaway query can never pull an unbounded result.
    /// If a real catalog ever approaches this, the sync must move to paged/delta sync.
    /// </summary>
    public const int MaxCatalogProducts = 10_000;

    public Task<List<MobileSyncProductDto>> GetActiveProductsAsync(CancellationToken ct = default)
        => (from p in _db.Products.AsNoTracking()
            where p.IsActive && !p.IsDeleted
            // Legacy price fields — from the default pricing structure (see MobileSyncProductDto).
            // (PricingStructureId, ProductId) is unique, so this LEFT JOIN adds at most one row.
            join i in _db.DefaultPricingItems() on p.Id equals i.ProductId into prices
            from i in prices.DefaultIfEmpty()
            orderby p.Code
            select new MobileSyncProductDto(
                p.Id,
                p.Code,
                p.ItemDescription,
                p.PrintDescription,
                p.PiecesPerPack,
                p.ImageUrl,
                p.CategoryId,
                p.Category != null ? p.Category.Name : null,
                i != null ? i.DealerPackPrice ?? 0m : 0m,
                i != null ? i.DealerCasePrice ?? 0m : 0m,
                i != null ? i.Mrp ?? 0m : 0m))
            .Take(MaxCatalogProducts)
            .ToListAsync(ct);

    public Task<List<MobileProductCategoryDto>> GetActiveProductCategoriesAsync(CancellationToken ct = default)
        => _db.ProductCategories
            .AsNoTracking()
            .Where(c => c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.Id)
            .Select(c => new MobileProductCategoryDto(c.Id, c.Name))
            .ToListAsync(ct);

    public async Task<List<MobilePricingStructureDto>> GetActivePricingStructuresAsync(CancellationToken ct = default)
    {
        // Only ACTIVE structures reach the phone, and only their priced items for active products —
        // an item the phone doesn't receive shows as "No price" and cannot be billed.
        var structures = await _db.PricingStructures
            .AsNoTracking()
            .Where(s => s.IsActive && !s.IsDeleted)
            .OrderByDescending(s => s.IsDefault)
            .ThenBy(s => s.Name)
            .Select(s => new { s.Id, s.Name, s.IsDefault })
            .ToListAsync(ct);
        if (structures.Count == 0) return [];

        var ids = structures.Select(s => s.Id).ToList();
        var items = await _db.PricingStructureItems
            .AsNoTracking()
            .Where(i => ids.Contains(i.PricingStructureId)
                     && i.DealerPackPrice != null
                     && i.Product.IsActive && !i.Product.IsDeleted)
            .OrderBy(i => i.ProductId)
            .Select(i => new { i.PricingStructureId, i.ProductId, i.DealerPackPrice, i.DealerCasePrice, i.Mrp })
            .ToListAsync(ct);

        var byStructure = items.ToLookup(i => i.PricingStructureId);
        return structures
            .Select(s => new MobilePricingStructureDto(
                s.Id, s.Name, s.IsDefault,
                byStructure[s.Id]
                    .Select(i => new MobilePricingItemDto(i.ProductId, i.DealerPackPrice!.Value, i.DealerCasePrice, i.Mrp))
                    .ToList()))
            .ToList();
    }
}
