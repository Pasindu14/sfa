using Microsoft.EntityFrameworkCore.Storage;
using sfa_api.Features.PricingStructures.DTOs;
using sfa_api.Features.PricingStructures.Entities;

namespace sfa_api.Features.PricingStructures.Repositories;

public interface IPricingStructureRepository
{
    /// <summary>Tracked, non-deleted structure.</summary>
    Task<PricingStructure?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(List<PricingStructureDto> Items, int TotalCount)> GetAllAsync(int skip, int take, string? search, bool? isActive, CancellationToken ct = default);
    Task<int> GetPricedCountAsync(int id, CancellationToken ct = default);
    /// <summary>Case-insensitive name clash among non-deleted structures.</summary>
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default);
    /// <summary>Tracked current default (non-deleted).</summary>
    Task<PricingStructure?> GetDefaultAsync(CancellationToken ct = default);
    Task<int?> GetDefaultIdAsync(CancellationToken ct = default);
    /// <summary>Ids from <paramref name="ids"/> that exist at all — deleted or inactive included (offline bills can be days old).</summary>
    Task<HashSet<int>> GetExistingIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    /// <summary>Current prices for the given structure/product pairs, keyed by (structureId, productId).</summary>
    Task<Dictionary<(int StructureId, int ProductId), PricingStructurePriceDto>> GetPricesAsync(
        IReadOnlyCollection<int> structureIds, IReadOnlyCollection<int> productIds, CancellationToken ct = default);

    Task AddAsync(PricingStructure structure, CancellationToken ct = default);
    void ApplyConcurrencyToken(PricingStructure structure, uint rowVersion);

    Task<List<PricingStructureItemRowDto>> GetItemRowsAsync(int structureId, CancellationToken ct = default);
    Task<List<PricingStructureItem>> GetItemsForProductsAsync(int structureId, IReadOnlyCollection<int> productIds, CancellationToken ct = default);
    Task<List<PricingStructureItem>> GetItemsAsync(int structureId, CancellationToken ct = default);
    Task AddItemsAsync(IEnumerable<PricingStructureItem> items, CancellationToken ct = default);
    /// <summary>Ids from <paramref name="productIds"/> that belong to non-deleted products.</summary>
    Task<HashSet<int>> GetExistingProductIdsAsync(IReadOnlyCollection<int> productIds, CancellationToken ct = default);
    Task<DefaultPricingStructurePricesDto?> GetDefaultPricesAsync(CancellationToken ct = default);

    IExecutionStrategy CreateExecutionStrategy();
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
