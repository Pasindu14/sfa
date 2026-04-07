using sfa_api.Features.Areas.Entities;

namespace sfa_api.Features.Areas.Repositories;

public interface IAreaRepository
{
    Task<Area?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Area?> GetByIdTrackedAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<Area> Areas, int TotalCount)> GetAllAsync(int skip, int take, int? regionId = null, bool? isActive = null, string? search = null, CancellationToken ct = default);
    Task<IEnumerable<Area>> GetAllActiveAsync(int? regionId = null, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int regionId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int regionId, int excludeId, CancellationToken ct = default);
    Task<bool> RegionExistsAsync(int regionId, CancellationToken ct = default);
    Task CreateAsync(Area area, CancellationToken ct = default);
    Task UpdateAsync(Area area, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
