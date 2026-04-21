using sfa_api.Features.Fleets.Entities;

namespace sfa_api.Features.Fleets.Repositories;

public interface IFleetRepository
{
    Task<Fleet?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<Fleet> Fleets, int TotalCount)> GetAllAsync(int skip, int take, string? search = null, CancellationToken ct = default);
    Task<IEnumerable<Fleet>> GetAllActiveAsync(CancellationToken ct = default);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int excludeId, CancellationToken ct = default);
    Task CreateAsync(Fleet fleet, CancellationToken ct = default);
    Task UpdateAsync(Fleet fleet, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
