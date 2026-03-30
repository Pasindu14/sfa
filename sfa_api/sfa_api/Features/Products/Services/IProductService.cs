using sfa_api.Features.Products.DTOs;
using sfa_api.Features.Products.Requests;

namespace sfa_api.Features.Products.Services;

public interface IProductService
{
    Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ProductListDto> GetAllAsync(int page, int pageSize, string? search = null, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, int? callerId, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, int? callerId, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task ActivateAsync(int id, CancellationToken ct = default);
}
