using sfa_api.Features.PricingStructures.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.PricingStructures;

/// <summary>
/// Shared query root for screens that value stock or targets at "the" price — bin card, sales
/// summary, rep monthly target, and the legacy price fields on the mobile product sync. Those used
/// to read Product prices; they now read the current default structure's prices.
/// </summary>
public static class PricingStructureQueries
{
    /// <summary>Items of the current default structure. Join on ProductId; at most one row per product.</summary>
    public static IQueryable<PricingStructureItem> DefaultPricingItems(this AppDbContext db)
        => db.PricingStructureItems.Where(i => i.PricingStructure.IsDefault && !i.PricingStructure.IsDeleted);
}
