using sfa_api.Common.Errors;
using sfa_api.Features.ProductCategories.DTOs;
using sfa_api.Features.ProductCategories.Entities;
using sfa_api.Features.ProductCategories.Repositories;
using sfa_api.Features.ProductCategories.Requests;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.ProductCategories.Services;

public class ProductCategoryService(
    IProductCategoryRepository repo,
    ICacheService cache,
    ILogger<ProductCategoryService> logger) : IProductCategoryService
{
    private readonly IProductCategoryRepository _repo = repo;
    private readonly ICacheService _cache = cache;
    private readonly ILogger<ProductCategoryService> _logger = logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string ListCachePrefix = "product-categories:list:";
    private const string AllActiveCacheKey = "product-categories:all-active";

    public async Task<ProductCategoryDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var category = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("ProductCategory", id);
        return MapToDto(category);
    }

    public async Task<ProductCategoryListDto> GetAllAsync(int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var cacheKey = $"{ListCachePrefix}{page}:{pageSize}:{search}";
        var cached = await _cache.GetAsync<ProductCategoryListDto>(cacheKey, ct);
        if (cached is not null) return cached;

        var skip = (page - 1) * pageSize;
        var (items, totalCount) = await _repo.GetAllAsync(skip, pageSize, search, ct);
        var result = new ProductCategoryListDto(
            ProductCategories: items.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );

        await _cache.SetAsync(cacheKey, result, CacheTtl, ct);
        return result;
    }

    public async Task<IEnumerable<ProductCategoryDto>> GetAllActiveAsync(CancellationToken ct = default)
    {
        var cached = await _cache.GetAsync<IEnumerable<ProductCategoryDto>>(AllActiveCacheKey, ct);
        if (cached is not null) return cached;

        var items = await _repo.GetAllActiveAsync(ct);
        var result = items.Select(MapToDto).ToList();

        await _cache.SetAsync(AllActiveCacheKey, result, CacheTtl, ct);
        return result;
    }

    public async Task<ProductCategoryDto> CreateAsync(CreateProductCategoryRequest request, int? callerId, CancellationToken ct = default)
    {
        if (await _repo.ExistsByNameAsync(request.Name, ct))
            throw new DuplicateResourceException("Name");

        var category = new ProductCategory
        {
            Name = request.Name,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(category, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("ProductCategory {CategoryId} created", category.Id);
        await InvalidateCacheAsync(ct);
        return MapToDto(category);
    }

    public async Task<ProductCategoryDto> UpdateAsync(int id, UpdateProductCategoryRequest request, int? callerId, CancellationToken ct = default)
    {
        var category = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("ProductCategory", id);

        if (await _repo.ExistsByNameAsync(request.Name, id, ct))
            throw new DuplicateResourceException("Name");

        category.Name = request.Name;
        category.UpdatedBy = callerId;
        category.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(category, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("ProductCategory {CategoryId} updated", id);
        await InvalidateCacheAsync(ct);
        return MapToDto(category);
    }

    public async Task ActivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var category = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("ProductCategory", id);

        category.IsActive = true;
        category.UpdatedBy = callerId;
        category.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(category, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("ProductCategory {CategoryId} activated", id);
        await InvalidateCacheAsync(ct);
    }

    public async Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var category = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("ProductCategory", id);

        category.IsActive = false;
        category.UpdatedBy = callerId;
        category.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(category, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("ProductCategory {CategoryId} deactivated", id);
        await InvalidateCacheAsync(ct);
    }

    private async Task InvalidateCacheAsync(CancellationToken ct)
    {
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
        await _cache.RemoveAsync(AllActiveCacheKey, ct);
    }

    private static ProductCategoryDto MapToDto(ProductCategory category) => new(
        Id: category.Id,
        Name: category.Name,
        IsActive: category.IsActive,
        CreatedAt: category.CreatedAt,
        UpdatedAt: category.UpdatedAt
    );
}
