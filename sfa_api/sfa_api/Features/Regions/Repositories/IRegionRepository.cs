using sfa_api.Features.Regions.Entities;

namespace sfa_api.Features.Regions.Repositories;

public interface IRegionRepository
{
    Task<Region?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<Region> Regions, int TotalCount)> GetAllAsync(int skip, int take, CancellationToken ct = default);
    Task<IEnumerable<Region>> GetAllActiveAsync(CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int excludeId, CancellationToken ct = default);
    Task CreateAsync(Region region, CancellationToken ct = default);
    Task UpdateAsync(Region region, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
