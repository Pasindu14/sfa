using sfa_api.Features.ProductCategories.Entities;

namespace sfa_api.Features.ProductCategories.Repositories;

public interface IProductCategoryRepository
{
    Task<ProductCategory?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<ProductCategory> ProductCategories, int TotalCount)> GetAllAsync(int skip, int take, string? search = null, CancellationToken ct = default);
    Task<IEnumerable<ProductCategory>> GetAllActiveAsync(CancellationToken ct = default);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int excludeId, CancellationToken ct = default);
    Task CreateAsync(ProductCategory category, CancellationToken ct = default);
    Task UpdateAsync(ProductCategory category, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
