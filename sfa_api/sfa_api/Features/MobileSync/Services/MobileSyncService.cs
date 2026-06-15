using sfa_api.Features.MobileSync.DTOs;
using sfa_api.Features.MobileSync.Repositories;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.MobileSync.Services;

public class MobileSyncService(
    IMobileSyncRepository repo,
    ICacheService cache,
    ILogger<MobileSyncService> logger) : IMobileSyncService
{
    private readonly IMobileSyncRepository _repo = repo;
    private readonly ICacheService _cache = cache;
    private readonly ILogger<MobileSyncService> _logger = logger;

    internal const string ProductsCacheKey = "mobile:products";
    internal const string CategoriesCacheKey = "mobile:product-categories";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    public async Task<MobileProductListDto> GetProductsAsync(CancellationToken ct = default)
    {
        var cached = await _cache.GetAsync<MobileProductListDto>(ProductsCacheKey, ct);
        if (cached is not null) return cached;

        var products = await _repo.GetActiveProductsAsync(ct);
        if (products.Count >= MobileSyncRepository.MaxCatalogProducts)
            _logger.LogWarning(
                "Mobile product catalog hit the safety cap of {Cap}; the sync may be truncated. " +
                "Move the catalog sync to paged/delta sync.", MobileSyncRepository.MaxCatalogProducts);
        var result = new MobileProductListDto(products, products.Count, DateTime.UtcNow);

        await _cache.SetAsync(ProductsCacheKey, result, CacheTtl, ct);
        return result;
    }

    public async Task<MobileProductCategoryListDto> GetProductCategoriesAsync(CancellationToken ct = default)
    {
        var cached = await _cache.GetAsync<MobileProductCategoryListDto>(CategoriesCacheKey, ct);
        if (cached is not null) return cached;

        var categories = await _repo.GetActiveProductCategoriesAsync(ct);
        var result = new MobileProductCategoryListDto(categories, categories.Count, DateTime.UtcNow);

        await _cache.SetAsync(CategoriesCacheKey, result, CacheTtl, ct);
        return result;
    }
}
