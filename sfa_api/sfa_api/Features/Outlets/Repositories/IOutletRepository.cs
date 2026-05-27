using sfa_api.Features.Outlets.DTOs;
using sfa_api.Features.Outlets.Entities;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.Features.Outlets.Repositories;

public interface IOutletRepository
{
    Task<Outlet?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<Outlet> Outlets, int TotalCount)> GetAllAsync(int skip, int take, bool? isActive = null, string? search = null, CancellationToken ct = default);
    Task<(IEnumerable<Outlet> Outlets, int TotalCount)> GetAllByTerritoryAsync(int territoryId, int skip, int take, bool? isActive = null, string? search = null, CancellationToken ct = default);
    Task<IEnumerable<Outlet>> GetAllActiveAsync(CancellationToken ct = default);
    Task<IEnumerable<OutletMapPointDto>> GetMapPointsAsync(CancellationToken ct = default);
    Task<IEnumerable<Outlet>> GetByRouteIdAsync(int routeId, CancellationToken ct = default);
    Task<RouteEntity?> GetRouteWithAncestorsAsync(int routeId, CancellationToken ct = default);
    Task<bool> ExistsByNicNoAsync(string nicNo, CancellationToken ct = default);
    Task<bool> ExistsByNicNoAsync(string nicNo, int excludeId, CancellationToken ct = default);
    Task CreateAsync(Outlet outlet, CancellationToken ct = default);
    Task UpdateAsync(Outlet outlet, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
