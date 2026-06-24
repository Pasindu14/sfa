using sfa_api.Common.Errors;
using sfa_api.Features.Fleets.Repositories;
using sfa_api.Features.ProductCategories.Repositories;
using sfa_api.Features.Products.DTOs;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Products.Repositories;
using sfa_api.Features.Products.Requests;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.Products.Services;

public class ProductService(
    IProductRepository repo,
    IFleetRepository fleetRepo,
    IProductCategoryRepository categoryRepo,
    ICacheService cache,
    ILogger<ProductService> logger) : IProductService
{
    private readonly IProductRepository _repo = repo;
    private readonly IFleetRepository _fleetRepo = fleetRepo;
    private readonly IProductCategoryRepository _categoryRepo = categoryRepo;
    private readonly ICacheService _cache = cache;
    private readonly ILogger<ProductService> _logger = logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string ListCachePrefix = "products:list:";

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var product = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Product", id);
        return MapToDto(product);
    }

    public async Task<ProductListDto> GetAllAsync(int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        (page, pageSize) = sfa_api.Common.Extensions.PaginationHelper.Clamp(page, pageSize);
        var cacheKey = $"products:list:{page}:{pageSize}:{search}";
        var cached = await _cache.GetAsync<ProductListDto>(cacheKey, ct);
        if (cached is not null) return cached;

        var skip = (page - 1) * pageSize;
        var (products, totalCount) = await _repo.GetAllAsync(skip, pageSize, search, ct);
        var result = new ProductListDto(
            Products: products.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );

        await _cache.SetAsync(cacheKey, result, CacheTtl, ct);
        return result;
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, int? callerId, CancellationToken ct = default)
    {
        if (await _repo.ExistsByCodeAsync(request.Code, ct))
            throw new DuplicateResourceException("Code");

        if (request.FleetId.HasValue && !await _fleetRepo.ExistsByIdAsync(request.FleetId.Value, ct))
            throw new NotFoundException("Fleet", request.FleetId.Value);

        if (request.CategoryId.HasValue && !await _categoryRepo.ExistsByIdAsync(request.CategoryId.Value, ct))
            throw new NotFoundException("ProductCategory", request.CategoryId.Value);

        var product = new Product
        {
            Code = request.Code,
            ItemDescription = request.ItemDescription,
            PrintDescription = request.PrintDescription,
            PiecesPerPack = request.PiecesPerPack,
            ImageUrl = request.ImageUrl,
            Remarks = request.Remarks,
            FleetId = request.FleetId,
            CategoryId = request.CategoryId,
            DealerPackPrice = request.DealerPackPrice,
            DealerCasePrice = request.DealerCasePrice,
            Mrp = request.Mrp,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(product, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} created with code {Code}", product.Id, product.Code);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
        await _cache.RemoveAsync("mobile:products", ct);
        return MapToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, int? callerId, CancellationToken ct = default)
    {
        var product = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Product", id);

        if (await _repo.ExistsByCodeAsync(request.Code, id, ct))
            throw new DuplicateResourceException("Code");

        if (request.FleetId.HasValue && !await _fleetRepo.ExistsByIdAsync(request.FleetId.Value, ct))
            throw new NotFoundException("Fleet", request.FleetId.Value);

        if (request.CategoryId.HasValue && !await _categoryRepo.ExistsByIdAsync(request.CategoryId.Value, ct))
            throw new NotFoundException("ProductCategory", request.CategoryId.Value);

        // Tell EF to use the client's RowVersion as the OriginalValue in the WHERE xmin = $token clause.
        // Setting product.RowVersion directly only changes CurrentValue — OriginalValue is what EF checks.
        _repo.ApplyConcurrencyToken(product, request.RowVersion);
        product.Code = request.Code;
        product.ItemDescription = request.ItemDescription;
        product.PrintDescription = request.PrintDescription;
        product.PiecesPerPack = request.PiecesPerPack;
        product.ImageUrl = request.ImageUrl;
        product.Remarks = request.Remarks;
        product.FleetId = request.FleetId;
        product.CategoryId = request.CategoryId;
        product.DealerPackPrice = request.DealerPackPrice;
        product.DealerCasePrice = request.DealerCasePrice;
        product.Mrp = request.Mrp;
        product.UpdatedBy = callerId;
        product.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(product, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} updated", id);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
        await _cache.RemoveAsync("mobile:products", ct);
        return MapToDto(product);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        _ = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Product", id);

        await _repo.DeactivateAsync(id, ct);
        _logger.LogInformation("Product {ProductId} deactivated", id);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
        await _cache.RemoveAsync("mobile:products", ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _ = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Product", id);

        await _repo.DeleteAsync(id, ct);
        await _repo.SaveChangesAsync(ct);
        _logger.LogInformation("Product {ProductId} deleted", id);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
        await _cache.RemoveAsync("mobile:products", ct);
    }

    public async Task ActivateAsync(int id, CancellationToken ct = default)
    {
        var product = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Product", id);

        product.IsActive = true;
        product.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(product, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} activated", id);
        await _cache.RemoveByPrefixAsync(ListCachePrefix, ct);
        await _cache.RemoveAsync("mobile:products", ct);
    }

    private static ProductDto MapToDto(Product product) => new(
        Id: product.Id,
        Code: product.Code,
        ItemDescription: product.ItemDescription,
        PrintDescription: product.PrintDescription,
        PiecesPerPack: product.PiecesPerPack,
        ImageUrl: product.ImageUrl,
        Remarks: product.Remarks,
        FleetId: product.FleetId,
        FleetName: product.Fleet?.Name,
        CategoryId: product.CategoryId,
        CategoryName: product.Category?.Name,
        IsActive: product.IsActive,
        DealerPackPrice: product.DealerPackPrice,
        DealerCasePrice: product.DealerCasePrice,
        Mrp: product.Mrp,
        RowVersion: product.RowVersion,
        CreatedAt: product.CreatedAt,
        UpdatedAt: product.UpdatedAt
    );
}
