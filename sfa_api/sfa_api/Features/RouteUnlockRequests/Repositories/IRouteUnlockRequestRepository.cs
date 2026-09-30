using sfa_api.Features.RouteUnlockRequests.DTOs;
using sfa_api.Features.RouteUnlockRequests.Entities;
using sfa_api.Features.RouteUnlockRequests.Requests;

namespace sfa_api.Features.RouteUnlockRequests.Repositories;

public interface IRouteUnlockRequestRepository
{
    /// Tracked, for transitions.
    Task<RouteUnlockRequest?> GetForUpdateAsync(int id, CancellationToken ct = default);

    /// Untracked with every name navigation loaded, for DTOs.
    Task<RouteUnlockRequest?> GetWithNamesAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// The approved unlock covering <paramref name="atUtc"/> for this rep on this
    /// route, or null. Read only by ProximityPolicyResolver for enforcement.
    /// </summary>
    Task<RouteUnlockRequest?> GetEffectiveAsync(int userId, int routeId, DateTime atUtc, CancellationToken ct = default);

    Task<RouteUnlockRequest?> GetLatestForRepOnDateAsync(int userId, DateOnly date, CancellationToken ct = default);
    Task<int> CountForRepOnDateAsync(int userId, DateOnly date, CancellationToken ct = default);
    Task<bool> HasOpenForRepOnDateAsync(int userId, DateOnly date, CancellationToken ct = default);

    /// <param name="scopeUserIds">Null = every rep (Admin); otherwise only these reps.</param>
    Task<(IReadOnlyList<RouteUnlockRequest> Items, int TotalCount)> GetPagedAsync(
        RouteUnlockListQuery query, IReadOnlyCollection<int>? scopeUserIds,
        DateOnly today, DateTime nowUtc, int skip, int take, CancellationToken ct = default);

    Task<int> CountPendingAsync(IReadOnlyCollection<int>? scopeUserIds, DateOnly today, CancellationToken ct = default);

    Task<IReadOnlyList<RouteUnlockRequestEvent>> GetEventsAsync(int requestId, CancellationToken ct = default);
    Task<IReadOnlyList<RouteUnlockBillDto>> GetBillsAsync(int requestId, CancellationToken ct = default);

    Task AddAsync(RouteUnlockRequest entity, CancellationToken ct = default);
    void ApplyConcurrencyToken(RouteUnlockRequest entity, uint rowVersion);
    Task SaveChangesAsync(CancellationToken ct = default);
}
