using Microsoft.EntityFrameworkCore.Storage;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Territories.Entities;

namespace sfa_api.Features.Divisions.Repositories;

public interface IDivisionRepository
{
    Task<Division?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<Division> Divisions, int TotalCount)> GetAllAsync(int skip, int take, int? territoryId = null, int? areaId = null, int? regionId = null, bool? isActive = null, string? search = null, CancellationToken ct = default);
    Task<IEnumerable<Division>> GetAllActiveAsync(int? territoryId = null, CancellationToken ct = default);
    Task<Territory?> GetTerritoryWithAncestorsAsync(int territoryId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int territoryId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int territoryId, int excludeId, CancellationToken ct = default);
    Task<bool> TerritoryExistsAsync(int territoryId, CancellationToken ct = default);
    Task CreateAsync(Division division, CancellationToken ct = default);
    Task UpdateAsync(Division division, CancellationToken ct = default);
    void ApplyConcurrencyToken(Division division, uint rowVersion);
    /// <summary>Opens an explicit transaction so a re-parent + its geo cascade commit atomically.</summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
    /// <summary>Execution strategy to wrap the manual transaction — required because EnableRetryOnFailure is on.</summary>
    IExecutionStrategy CreateExecutionStrategy();
    Task SaveChangesAsync(CancellationToken ct = default);
}
