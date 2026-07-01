using sfa_api.Features.Regions.Entities;

namespace sfa_api.Features.Regions.Repositories;

public interface IRegionRepository
{
    Task<Region?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<Region> Regions, int TotalCount)> GetAllAsync(int skip, int take, string? search = null, CancellationToken ct = default);
    Task<IEnumerable<Region>> GetAllActiveAsync(CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int excludeId, CancellationToken ct = default);
    Task CreateAsync(Region region, CancellationToken ct = default);
    Task UpdateAsync(Region region, CancellationToken ct = default);
    void ApplyConcurrencyToken(Region region, uint rowVersion);
    /// <summary>True if the region still has at least one active (not deleted) area under it.</summary>
    Task<bool> HasActiveAreasAsync(int regionId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
