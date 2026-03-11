using sfa_api.Features.Divisions.Entities;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.Features.Routes.Repositories;

public interface IRouteRepository
{
    Task<RouteEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<RouteEntity> Routes, int TotalCount)> GetAllAsync(int skip, int take, bool? isActive = null, string? search = null, CancellationToken ct = default);
    Task<IEnumerable<RouteEntity>> GetAllActiveAsync(CancellationToken ct = default);
    Task<Division?> GetDivisionWithAncestorsAsync(int divisionId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int divisionId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int divisionId, int excludeId, CancellationToken ct = default);
    Task CreateAsync(RouteEntity route, CancellationToken ct = default);
    Task UpdateAsync(RouteEntity route, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
