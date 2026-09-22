using sfa_api.Features.Products.DTOs;
using sfa_api.Features.Products.Entities;

namespace sfa_api.Features.Products.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<Product> Products, int TotalCount)> GetAllAsync(int skip, int take, string? search = null, bool? isActive = null, CancellationToken ct = default);
    /// <summary>All active, non-deleted products as slim lookup rows, ordered by description.
    /// Capped at <paramref name="max"/> as a safety net — the catalogue is expected to stay in the hundreds.</summary>
    Task<List<ProductLookupDto>> GetActiveLookupAsync(int max, CancellationToken ct = default);
    Task<HashSet<int>> GetActiveProductIdsInSetAsync(IEnumerable<int> ids, CancellationToken ct = default);

    /// <summary>Returns Id → (Code, ItemDescription, PacksPerCase) for the requested IDs (no IsActive filter).
    /// PacksPerCase is sourced from <c>PiecesPerPack</c>, which the codebase semantically uses as packs-per-case.</summary>
    Task<Dictionary<int, (string Code, string Name, int PacksPerCase)>> GetCodeAndNameByIdsAsync(
        IEnumerable<int> ids, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, int excludeProductId, CancellationToken ct = default);
    Task CreateAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    void ApplyConcurrencyToken(Product product, uint rowVersion);
    Task DeactivateAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
