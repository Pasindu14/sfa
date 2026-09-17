using sfa_api.Common.Errors;
using sfa_api.Features.Distributors.Repositories;
using sfa_api.Features.ProductCategoryPricings.DTOs;
using sfa_api.Features.ProductCategoryPricings.Repositories;
using sfa_api.Features.ProductCategoryPricings.Requests;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.ProductCategoryPricings.Services;

public class ProductCategoryPricingService(
    IProductCategoryPricingRepository repo,
    IDistributorRepository distributorRepo,
    ICacheService cache,
    ILogger<ProductCategoryPricingService> logger) : IProductCategoryPricingService
{
    private readonly IProductCategoryPricingRepository _repo = repo;
    private readonly IDistributorRepository _distributorRepo = distributorRepo;
    private readonly ICacheService _cache = cache;
    private readonly ILogger<ProductCategoryPricingService> _logger = logger;

    public async Task<IEnumerable<ProductCategoryPricingDto>> GetAllAsync(CancellationToken ct = default)
        => await _repo.GetAllWithPricingAsync(ct);

    public async Task<IEnumerable<ProductPriceForDistributorDto>> GetForDistributorAsync(int distributorId, CancellationToken ct = default)
    {
        // Resolved on every call (outside the cache) so a distributor's category change applies immediately.
        var distributor = await _distributorRepo.GetByIdAsync(distributorId, ct)
            ?? throw new NotFoundException("Distributor", distributorId);

        var cacheKey = ProductCategoryPricingCacheKeys.ForCategory(distributor.Category);
        var cached = await _cache.GetAsync<List<ProductPriceForDistributorDto>>(cacheKey, ct);
        if (cached is not null) return cached;

        var result = (await _repo.GetForCategoryAsync(distributor.Category, ct)).ToList();
        await _cache.SetAsync(cacheKey, result, ProductCategoryPricingCacheKeys.Ttl, ct);
        return result;
    }

    public async Task BulkUpsertAsync(BulkUpsertPricingRequest request, int callerId, CancellationToken ct = default)
    {
        await _repo.BulkUpsertAsync(request.Items, callerId, ct);
        await _cache.RemoveByPrefixAsync(ProductCategoryPricingCacheKeys.Prefix, ct);
        _logger.LogInformation("Product category pricing bulk upserted by caller {CallerId}, {Count} product(s)",
            callerId, request.Items.Count());
    }
}
