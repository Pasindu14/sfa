using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.UserGeoAssignments.Entities;
using sfa_api.Features.UserReportingLines.Entities;
using sfa_api.Features.Users.Entities;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.Features.UserGeoAssignments.Repositories;

public interface IUserGeoAssignmentRepository
{
    Task<UserGeoAssignment?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<UserGeoAssignment?> GetActiveByUserIdAsync(int userId, CancellationToken ct = default);

    Task<(IEnumerable<UserGeoAssignment> Items, int TotalCount)> GetAllAsync(
        int skip,
        int take,
        string? search = null,
        string? role = null,
        int? regionId = null,
        int? areaId = null,
        int? territoryId = null,
        int? divisionId = null,
        bool? isActive = null,
        CancellationToken ct = default);

    /// <summary>
    /// Batch-fetches the active reporting line (with manager navigation) for a set of user IDs.
    /// Used to enrich geo-assignment DTOs without N+1 queries.
    /// </summary>
    Task<IEnumerable<UserReportingLine>> GetActiveReportingLinesByUserIdsAsync(
        IEnumerable<int> userIds, CancellationToken ct = default);

    Task<(int Total, int Active, int ActiveTerritories, int ThisMonth)> GetStatsAsync(
        CancellationToken ct = default);

    /// <summary>Returns a Division with its Territory/Area/Region for ancestor-ID denormalization.</summary>
    Task<Division?> GetDivisionWithAncestorsAsync(int divisionId, CancellationToken ct = default);

    /// <summary>Returns active routes belonging to a specific division.</summary>
    Task<IEnumerable<RouteEntity>> GetActiveRoutesByDivisionIdAsync(int divisionId, CancellationToken ct = default);

    Task<bool> UserExistsAsync(int userId, CancellationToken ct = default);
    Task<bool> IsAdminOrDistributorAsync(int userId, CancellationToken ct = default);
    Task<UserRole?> GetUserRoleAsync(int userId, CancellationToken ct = default);
    Task<bool> DivisionExistsAsync(int divisionId, CancellationToken ct = default);

    Task CreateAsync(UserGeoAssignment entity, CancellationToken ct = default);
    Task UpdateAsync(UserGeoAssignment entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
