using sfa_api.Features.ProductCategoryPricings.DTOs;
using sfa_api.Features.ProductCategoryPricings.Requests;

namespace sfa_api.Features.ProductCategoryPricings.Services;

public interface IProductCategoryPricingService
{
    Task<IEnumerable<ProductCategoryPricingDto>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<ProductPriceForDistributorDto>> GetForDistributorAsync(int distributorId, CancellationToken ct = default);
    Task BulkUpsertAsync(BulkUpsertPricingRequest request, int callerId, CancellationToken ct = default);
}
