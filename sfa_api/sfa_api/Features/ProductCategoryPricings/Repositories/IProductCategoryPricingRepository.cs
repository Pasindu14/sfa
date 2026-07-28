using sfa_api.Features.ProductCategoryPricings.DTOs;
using sfa_api.Features.ProductCategoryPricings.Requests;

namespace sfa_api.Features.ProductCategoryPricings.Repositories;

public interface IProductCategoryPricingRepository
{
    /// <summary>
    /// Returns all active products with their A/B/C/D category prices.
    /// Products with no pricing rows are included with prices defaulting to 0.
    /// </summary>
    Task<IEnumerable<ProductCategoryPricingDto>> GetAllWithPricingAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns all active products with a single resolved price for the given distributor category.
    /// </summary>
    Task<IEnumerable<ProductPriceForDistributorDto>> GetForCategoryAsync(string category, CancellationToken ct = default);

    /// <summary>
    /// Returns configured prices for the given category, keyed by product ID, for the
    /// requested products only. Unlike <see cref="GetForCategoryAsync"/> this does NOT
    /// default missing rows to 0 — a product absent from the result has no price
    /// configured, which callers must be able to distinguish from a price of zero.
    /// </summary>
    Task<Dictionary<int, decimal>> GetPriceMapForCategoryAsync(
        string category, IEnumerable<int> productIds, CancellationToken ct = default);

    /// <summary>
    /// Upserts all 4 category prices for each product row in a single SaveChanges call.
    /// </summary>
    Task BulkUpsertAsync(IEnumerable<PricingRowRequest> items, int callerId, CancellationToken ct = default);
}
