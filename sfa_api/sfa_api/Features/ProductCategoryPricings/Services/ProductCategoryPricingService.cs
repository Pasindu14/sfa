using sfa_api.Features.ProductCategoryPricings.DTOs;
using sfa_api.Features.ProductCategoryPricings.Repositories;
using sfa_api.Features.ProductCategoryPricings.Requests;

namespace sfa_api.Features.ProductCategoryPricings.Services;

public class ProductCategoryPricingService(
    IProductCategoryPricingRepository repo,
    ILogger<ProductCategoryPricingService> logger) : IProductCategoryPricingService
{
    private readonly IProductCategoryPricingRepository _repo = repo;
    private readonly ILogger<ProductCategoryPricingService> _logger = logger;

    public async Task<IEnumerable<ProductCategoryPricingDto>> GetAllAsync(CancellationToken ct = default)
        => await _repo.GetAllWithPricingAsync(ct);

    public async Task BulkUpsertAsync(BulkUpsertPricingRequest request, int callerId, CancellationToken ct = default)
    {
        await _repo.BulkUpsertAsync(request.Items, callerId, ct);
        _logger.LogInformation("Product category pricing bulk upserted by caller {CallerId}, {Count} product(s)",
            callerId, request.Items.Count());
    }
}
