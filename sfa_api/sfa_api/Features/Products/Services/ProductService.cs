using sfa_api.Common.Errors;
using sfa_api.Features.Products.DTOs;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Products.Repositories;
using sfa_api.Features.Products.Requests;

namespace sfa_api.Features.Products.Services;

public class ProductService(
    IProductRepository repo,
    ILogger<ProductService> logger) : IProductService
{
    private readonly IProductRepository _repo = repo;
    private readonly ILogger<ProductService> _logger = logger;

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var product = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Product", id);
        return MapToDto(product);
    }

    public async Task<ProductListDto> GetAllAsync(int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (products, totalCount) = await _repo.GetAllAsync(skip, pageSize, search, ct);
        return new ProductListDto(
            Products: products.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, int? callerId, CancellationToken ct = default)
    {
        if (await _repo.ExistsByCodeAsync(request.Code, ct))
            throw new DuplicateResourceException("Code");

        var product = new Product
        {
            Code = request.Code,
            ItemDescription = request.ItemDescription,
            PrintDescription = request.PrintDescription,
            PiecesPerPack = request.PiecesPerPack,
            ImageUrl = request.ImageUrl,
            Remarks = request.Remarks,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(product, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} created with code {Code}", product.Id, product.Code);
        return MapToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, int? callerId, CancellationToken ct = default)
    {
        var product = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Product", id);

        if (await _repo.ExistsByCodeAsync(request.Code, id, ct))
            throw new DuplicateResourceException("Code");

        product.Code = request.Code;
        product.ItemDescription = request.ItemDescription;
        product.PrintDescription = request.PrintDescription;
        product.PiecesPerPack = request.PiecesPerPack;
        product.ImageUrl = request.ImageUrl;
        product.Remarks = request.Remarks;
        product.UpdatedBy = callerId;
        product.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(product, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} updated", id);
        return MapToDto(product);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _ = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Product", id);

        await _repo.DeleteAsync(id, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} deactivated", id);
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
    }

    private static ProductDto MapToDto(Product product) => new(
        Id: product.Id,
        Code: product.Code,
        ItemDescription: product.ItemDescription,
        PrintDescription: product.PrintDescription,
        PiecesPerPack: product.PiecesPerPack,
        ImageUrl: product.ImageUrl,
        Remarks: product.Remarks,
        IsActive: product.IsActive,
        CreatedAt: product.CreatedAt,
        UpdatedAt: product.UpdatedAt
    );
}
