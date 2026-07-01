using sfa_api.Features.Divisions.Entities;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.Features.Routes.Repositories;

public interface IRouteRepository
{
    Task<RouteEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<RouteEntity> Routes, int TotalCount)> GetAllAsync(int skip, int take, bool? isActive = null, string? search = null, CancellationToken ct = default);
    Task<IEnumerable<RouteEntity>> GetAllActiveAsync(CancellationToken ct = default);
    Task<IEnumerable<RouteEntity>> GetActiveByDivisionIdAsync(int divisionId, CancellationToken ct = default);
    Task<Division?> GetDivisionWithAncestorsAsync(int divisionId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int divisionId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int divisionId, int excludeId, CancellationToken ct = default);
    /// <summary>
    /// Returns the set of pin colours currently in use by non-deleted routes, so a unique
    /// colour can be assigned on create/update. Pass <paramref name="excludeRouteId"/> when
    /// updating so the route keeps its own colour. Comparison is case-insensitive.
    /// </summary>
    Task<HashSet<string>> GetUsedPinColorsAsync(int? excludeRouteId = null, CancellationToken ct = default);
    Task CreateAsync(RouteEntity route, CancellationToken ct = default);
    Task UpdateAsync(RouteEntity route, CancellationToken ct = default);
    /// <summary>True if the route still has at least one active (not deleted) outlet under it.</summary>
    Task<bool> HasActiveOutletsAsync(int routeId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
