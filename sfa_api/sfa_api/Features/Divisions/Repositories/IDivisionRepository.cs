using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Territories.Entities;

namespace sfa_api.Features.Divisions.Repositories;

public interface IDivisionRepository
{
    Task<Division?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<Division> Divisions, int TotalCount)> GetAllAsync(int skip, int take, int? territoryId = null, int? areaId = null, int? regionId = null, bool? isActive = null, CancellationToken ct = default);
    Task<IEnumerable<Division>> GetAllActiveAsync(int? territoryId = null, CancellationToken ct = default);
    Task<Territory?> GetTerritoryWithAncestorsAsync(int territoryId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int territoryId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int territoryId, int excludeId, CancellationToken ct = default);
    Task<bool> TerritoryExistsAsync(int territoryId, CancellationToken ct = default);
    Task CreateAsync(Division division, CancellationToken ct = default);
    Task UpdateAsync(Division division, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
