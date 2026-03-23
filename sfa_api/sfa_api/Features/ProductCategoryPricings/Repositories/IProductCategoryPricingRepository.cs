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
    /// Upserts all 4 category prices for each product row in a single SaveChanges call.
    /// </summary>
    Task BulkUpsertAsync(IEnumerable<PricingRowRequest> items, int callerId, CancellationToken ct = default);
}
