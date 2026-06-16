using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Territories.Entities;

namespace sfa_api.Features.Territories.Repositories;

public interface ITerritoryRepository
{
    Task<Territory?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<Territory> Territories, int TotalCount)> GetAllAsync(int skip, int take, int? areaId = null, bool? isActive = null, string? search = null, CancellationToken ct = default);
    Task<IEnumerable<Territory>> GetAllActiveAsync(int? areaId = null, CancellationToken ct = default);
    Task<Area?> GetAreaWithRegionAsync(int areaId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int areaId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int areaId, int excludeId, CancellationToken ct = default);
    Task<bool> AreaExistsAsync(int areaId, CancellationToken ct = default);
    Task CreateAsync(Territory territory, CancellationToken ct = default);
    Task UpdateAsync(Territory territory, CancellationToken ct = default);
    void ApplyConcurrencyToken(Territory territory, uint rowVersion);
    Task SaveChangesAsync(CancellationToken ct = default);
}
