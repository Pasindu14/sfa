using sfa_api.Features.DailyRouteAssignments.Entities;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.DailyRouteAssignments.Repositories;

public interface IDailyRouteAssignmentRepository
{
    Task<DailyRouteAssignment?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<(IEnumerable<DailyRouteAssignment> Items, int TotalCount)> GetAllAsync(
        int skip,
        int take,
        int? userId = null,
        int? routeId = null,
        DateOnly? date = null,
        CancellationToken ct = default);

    /// <summary>Returns reps (SalesRep role) who have an active reporting line to the given supervisor.</summary>
    Task<IEnumerable<User>> GetRepsByReportsToAsync(int supervisorId, CancellationToken ct = default);

    /// <summary>Returns active routes in the division assigned to a given rep via UserGeoAssignment.</summary>
    Task<IEnumerable<RouteEntity>> GetActiveRoutesByRepIdAsync(int userId, CancellationToken ct = default);

    Task<bool> IsRepAlreadyAssignedOnDateAsync(int userId, DateOnly date, CancellationToken ct = default);
    Task<DailyRouteAssignment?> GetByRouteAndDateAsync(int routeId, DateOnly date, CancellationToken ct = default);

    Task<bool> UserExistsAsync(int userId, CancellationToken ct = default);
    Task<bool> RouteExistsAsync(int routeId, CancellationToken ct = default);

    Task CreateAsync(DailyRouteAssignment entity, CancellationToken ct = default);
    Task UpdateAsync(DailyRouteAssignment entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
