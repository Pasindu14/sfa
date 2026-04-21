using sfa_api.Features.ProductCategories.DTOs;
using sfa_api.Features.ProductCategories.Requests;

namespace sfa_api.Features.ProductCategories.Services;

public interface IProductCategoryService
{
    Task<ProductCategoryDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ProductCategoryListDto> GetAllAsync(int page, int pageSize, string? search = null, CancellationToken ct = default);
    Task<IEnumerable<ProductCategoryDto>> GetAllActiveAsync(CancellationToken ct = default);
    Task<ProductCategoryDto> CreateAsync(CreateProductCategoryRequest request, int? callerId, CancellationToken ct = default);
    Task<ProductCategoryDto> UpdateAsync(int id, UpdateProductCategoryRequest request, int? callerId, CancellationToken ct = default);
    Task ActivateAsync(int id, int? callerId, CancellationToken ct = default);
    Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default);
}
