using sfa_api.Features.Products.Entities;

namespace sfa_api.Features.Products.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<Product> Products, int TotalCount)> GetAllAsync(int skip, int take, string? search = null, CancellationToken ct = default);
    Task<HashSet<int>> GetActiveProductIdsInSetAsync(IEnumerable<int> ids, CancellationToken ct = default);

    /// <summary>Returns Id → (Code, ItemDescription, PacksPerCase) for the requested IDs (no IsActive filter).
    /// PacksPerCase is sourced from <c>PiecesPerPack</c>, which the codebase semantically uses as packs-per-case.</summary>
    Task<Dictionary<int, (string Code, string Name, int PacksPerCase)>> GetCodeAndNameByIdsAsync(
        IEnumerable<int> ids, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, int excludeProductId, CancellationToken ct = default);
    Task CreateAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
