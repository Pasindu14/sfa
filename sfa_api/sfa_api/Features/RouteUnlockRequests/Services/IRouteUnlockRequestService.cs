using sfa_api.Features.RouteUnlockRequests.DTOs;
using sfa_api.Features.RouteUnlockRequests.Requests;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.RouteUnlockRequests.Services;

public interface IRouteUnlockRequestService
{
    // Rep
    Task<RouteUnlockRequestDto> CreateAsync(CreateRouteUnlockRequest request, int callerId, CancellationToken ct = default);
    Task<RouteUnlockRequestDto?> GetMyTodayAsync(int callerId, CancellationToken ct = default);
    Task<RouteUnlockRequestDto> CancelAsync(int id, CancelRouteUnlockRequest request, int callerId, CancellationToken ct = default);

    // Reviewers (Supervisor for their own reps, Admin for anyone)
    Task<RouteUnlockPagedResult> GetPagedAsync(RouteUnlockListQuery query, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<RouteUnlockPendingCountDto> GetPendingCountAsync(int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<RouteUnlockRequestDetailDto> GetDetailAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<RouteUnlockRequestDto> ApproveAsync(int id, ApproveRouteUnlockRequest request, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<RouteUnlockRequestDto> RejectAsync(int id, ReasonedRouteUnlockRequest request, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<RouteUnlockRequestDto> RevokeAsync(int id, ReasonedRouteUnlockRequest request, int callerId, UserRole callerRole, CancellationToken ct = default);
}
