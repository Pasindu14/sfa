using sfa_api.Features.PricingStructures.Entities;

namespace sfa_api.Features.PricingStructures.Repositories;

public interface IPricingStructureRepository
{
    Task<PricingStructure?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PricingStructure?> GetByIdWithItemsAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<PricingStructure>, int TotalCount)> GetAllAsync(int skip, int take, string? search = null, CancellationToken ct = default);
    Task<PricingStructure?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<PricingStructure?> GetCurrentDefaultAsync(CancellationToken ct = default);
    Task<IEnumerable<PricingStructureItem>> GetItemsAsync(int pricingStructureId, CancellationToken ct = default);
    Task<IEnumerable<PricingStructure>> GetAllActiveWithItemsAsync(CancellationToken ct = default);
    Task CreateAsync(PricingStructure entity, CancellationToken ct = default);
    Task UpdateAsync(PricingStructure entity, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task ActivateAsync(int id, CancellationToken ct = default);
    Task BulkReplaceItemsAsync(int pricingStructureId, IEnumerable<PricingStructureItem> newItems, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
