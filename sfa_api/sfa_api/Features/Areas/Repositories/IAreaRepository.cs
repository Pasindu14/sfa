using Microsoft.EntityFrameworkCore.Storage;
using sfa_api.Features.Areas.DTOs;
using sfa_api.Features.Areas.Entities;

namespace sfa_api.Features.Areas.Repositories;

public interface IAreaRepository
{
    Task<Area?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Area?> GetByIdTrackedAsync(int id, CancellationToken ct = default);
    Task<(IReadOnlyList<Area> Areas, int TotalCount)> GetAllAsync(int skip, int take, int? regionId = null, bool? isActive = null, string? search = null, CancellationToken ct = default);
    Task<IReadOnlyList<AreaDto>> GetAllActiveAsync(int? regionId = null, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int regionId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int regionId, int excludeId, CancellationToken ct = default);
    Task<bool> RegionExistsAsync(int regionId, CancellationToken ct = default);
    Task CreateAsync(Area area, CancellationToken ct = default);
    Task UpdateAsync(Area area);
    void ApplyConcurrencyToken(Area area, uint rowVersion);
    /// <summary>True if the area still has at least one active (not deleted) territory under it.</summary>
    Task<bool> HasActiveTerritoriesAsync(int areaId, CancellationToken ct = default);
    /// <summary>Opens an explicit transaction so a re-parent + its geo cascade commit atomically.</summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
