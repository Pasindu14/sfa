using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.DailyRouteAssignments.Repositories;
using sfa_api.Features.RouteUnlockRequests.DTOs;
using sfa_api.Features.RouteUnlockRequests.Entities;
using sfa_api.Features.RouteUnlockRequests.Options;
using sfa_api.Features.RouteUnlockRequests.Repositories;
using sfa_api.Features.RouteUnlockRequests.Requests;
using sfa_api.Features.Supervisor.Services;
using sfa_api.Features.UserProximityExemptions.Services;
using sfa_api.Features.UserReportingLines.Repositories;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Locking;
using sfa_api.Infrastructure.Notifications;

namespace sfa_api.Features.RouteUnlockRequests.Services;

/// <summary>
/// Rep-initiated, supervisor/admin-approved relief from the billing geofence for
/// every outlet on the rep's route for today.
///
/// Every transition follows the same shape: take the per-request lock, check the
/// state guard and the xmin RowVersion, mutate the row and append a timeline
/// event in ONE SaveChanges (so no manual transaction and no execution-strategy
/// trap), then notify — notifications save through the shared DbContext, so they
/// must run after our own save.
/// </summary>
public class RouteUnlockRequestService(
    IRouteUnlockRequestRepository repo,
    IDailyRouteAssignmentRepository assignmentRepo,
    IUserReportingLineRepository reportingLineRepo,
    ISupervisorService supervisorService,
    IProximityPolicyResolver policyResolver,
    IDistributedLockService lockService,
    INotificationService notifications,
    IHttpContextAccessor httpContextAccessor,
    IOptions<RouteUnlockOptions> options,
    ILogger<RouteUnlockRequestService> logger) : IRouteUnlockRequestService
{
    private readonly IRouteUnlockRequestRepository _repo = repo;
    private readonly IDailyRouteAssignmentRepository _assignmentRepo = assignmentRepo;
    private readonly IUserReportingLineRepository _reportingLineRepo = reportingLineRepo;
    private readonly ISupervisorService _supervisorService = supervisorService;
    private readonly IProximityPolicyResolver _policyResolver = policyResolver;
    private readonly IDistributedLockService _lockService = lockService;
    private readonly INotificationService _notifications = notifications;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly RouteUnlockOptions _options = options.Value;
    private readonly ILogger<RouteUnlockRequestService> _logger = logger;

    // ── Rep ───────────────────────────────────────────────────────────────

    public async Task<RouteUnlockRequestDto> CreateAsync(
        CreateRouteUnlockRequest request, int callerId, CancellationToken ct = default)
    {
        // Serialises one rep's creates so the daily cap count cannot be raced.
        await using var @lock = await _lockService.AcquireAsync($"route-unlock:rep:{callerId}", ct)
            ?? throw RouteUnlockConflictException.Busy();

        var today = SriLankaTime.Today;

        // The route always comes from the server's own assignment — the client
        // never names it, so a rep cannot unlock a route they are not on.
        var assignment = await _assignmentRepo.GetActiveTodayAssignmentForRepAsync(callerId, ct)
            ?? throw new BusinessRuleException(
                "ROUTE_UNLOCK_NO_ASSIGNMENT",
                "You have no route assigned for today, so there is nothing to unlock.");

        var policy = await _policyResolver.ResolveAsync(callerId, assignment.RouteId, ct: ct);
        if (!policy.Enforced)
            throw new BusinessRuleException(
                "ROUTE_UNLOCK_ALREADY_EXEMPT",
                "The distance check is already off for you today.");

        if (await _repo.HasOpenForRepOnDateAsync(callerId, today, ct))
            throw RouteUnlockConflictException.AlreadyOpen();

        if (await _repo.CountForRepOnDateAsync(callerId, today, ct) >= _options.MaxRequestsPerDay)
            throw new BusinessRuleException(
                "ROUTE_UNLOCK_DAILY_LIMIT",
                $"You can send at most {_options.MaxRequestsPerDay} unlock requests a day. Contact your supervisor directly.");

        var supervisorId = (await _reportingLineRepo.GetActiveByUserIdAsync(callerId, ct))?.ReportsToUserId;

        var now = DateTime.UtcNow;
        var reason = request.Reason.Trim();
        var entity = new RouteUnlockRequest
        {
            UserId = callerId,
            RouteId = assignment.RouteId,
            DailyRouteAssignmentId = assignment.Id,
            BusinessDate = today,
            Status = RouteUnlockStatus.Pending,
            RequestReason = reason,
            RequestedAt = now,
            RequestLatitude = request.Latitude,
            RequestLongitude = request.Longitude,
            RequestGpsAccuracyMeters = request.GpsAccuracyMeters,
            SupervisorUserId = supervisorId,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = callerId,
            UpdatedBy = callerId
        };
        entity.Events.Add(NewEvent(RouteUnlockAction.Requested, null, RouteUnlockStatus.Pending,
            callerId, nameof(UserRole.SalesRep), reason, now));

        await _repo.AddAsync(entity, ct);
        try
        {
            await _repo.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // The partial unique index caught a double-submit the lock missed
            // (e.g. two API replicas without a shared lock).
            if (await _repo.HasOpenForRepOnDateAsync(callerId, today, CancellationToken.None))
                throw RouteUnlockConflictException.AlreadyOpen();
            throw;
        }

        _logger.LogInformation(
            "Route unlock {RequestId} requested by rep {UserId} for route {RouteId} on {Date}; routed to {SupervisorId}",
            entity.Id, callerId, entity.RouteId, today, supervisorId);

        var dto = await LoadDtoAsync(entity.Id, ct);

        if (supervisorId is { } supId)
            await _notifications.SendToUserAsync(supId,
                "Route unlock request",
                $"{dto.UserName} is asking to unlock all outlets on {dto.RouteName} for today.",
                Data("ROUTE_UNLOCK_REQUESTED", entity.Id), ct);

        return dto;
    }

    public async Task<RouteUnlockRequestDto?> GetMyTodayAsync(int callerId, CancellationToken ct = default)
    {
        var latest = await _repo.GetLatestForRepOnDateAsync(callerId, SriLankaTime.Today, ct);
        return latest is null ? null : MapToDto(latest, DateTime.UtcNow);
    }

    public async Task<RouteUnlockRequestDto> CancelAsync(
        int id, CancelRouteUnlockRequest request, int callerId, CancellationToken ct = default)
    {
        await using var @lock = await AcquireRequestLockAsync(id, ct);

        var entity = await _repo.GetForUpdateAsync(id, ct)
            ?? throw new NotFoundException("RouteUnlockRequest", id);

        if (entity.UserId != callerId)
            throw new AuthorizationException("this unlock request");
        if (entity.Status != RouteUnlockStatus.Pending)
            throw RouteUnlockConflictException.InvalidState(entity.Status.ToString());

        _repo.ApplyConcurrencyToken(entity, request.RowVersion);

        var now = DateTime.UtcNow;
        entity.Status = RouteUnlockStatus.Cancelled;
        entity.CancelledAt = now;
        Touch(entity, callerId, now);
        entity.Events.Add(NewEvent(RouteUnlockAction.Cancelled, RouteUnlockStatus.Pending,
            RouteUnlockStatus.Cancelled, callerId, nameof(UserRole.SalesRep), null, now));

        await _repo.SaveChangesAsync(ct);

        var dto = await LoadDtoAsync(id, ct);
        if (entity.SupervisorUserId is { } supId)
            await _notifications.SendToUserAsync(supId,
                "Unlock request cancelled",
                $"{dto.UserName} cancelled their unlock request for {dto.RouteName}.",
                Data("ROUTE_UNLOCK_CANCELLED", id), ct);
        return dto;
    }

    // ── Reviewers ─────────────────────────────────────────────────────────

    public async Task<RouteUnlockPagedResult> GetPagedAsync(
        RouteUnlockListQuery query, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;
        var scope = await ResolveScopeAsync(callerId, callerRole, ct);
        var now = DateTime.UtcNow;

        var (items, total) = await _repo.GetPagedAsync(
            query, scope, SriLankaTime.Today, now, (page - 1) * pageSize, pageSize, ct);

        return new RouteUnlockPagedResult(
            items.Select(x => MapToDto(x, now)).ToList(), total, page, pageSize);
    }

    public async Task<RouteUnlockPendingCountDto> GetPendingCountAsync(
        int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        var scope = await ResolveScopeAsync(callerId, callerRole, ct);
        return new RouteUnlockPendingCountDto(await _repo.CountPendingAsync(scope, SriLankaTime.Today, ct));
    }

    public async Task<RouteUnlockRequestDetailDto> GetDetailAsync(
        int id, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        var entity = await _repo.GetWithNamesAsync(id, ct)
            ?? throw new NotFoundException("RouteUnlockRequest", id);

        if (callerRole == UserRole.SalesRep)
        {
            if (entity.UserId != callerId)
                throw new AuthorizationException("this unlock request");
        }
        else
        {
            await EnsureCanReviewAsync(entity.UserId, callerId, callerRole, ct);
        }

        var events = await _repo.GetEventsAsync(id, ct);
        var bills = await _repo.GetBillsAsync(id, ct);

        return new RouteUnlockRequestDetailDto(
            MapToDto(entity, DateTime.UtcNow),
            events.Select(MapEvent).ToList(),
            bills);
    }

    public async Task<RouteUnlockRequestDto> ApproveAsync(
        int id, ApproveRouteUnlockRequest request, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        await using var @lock = await AcquireRequestLockAsync(id, ct);

        var entity = await _repo.GetForUpdateAsync(id, ct)
            ?? throw new NotFoundException("RouteUnlockRequest", id);

        await EnsureCanReviewAsync(entity.UserId, callerId, callerRole, ct);

        if (entity.Status != RouteUnlockStatus.Pending)
            throw RouteUnlockConflictException.InvalidState(entity.Status.ToString());

        var today = SriLankaTime.Today;
        if (entity.BusinessDate != today)
            throw new BusinessRuleException(
                "ROUTE_UNLOCK_EXPIRED",
                "This request was for an earlier day and can no longer be approved.");

        // Approving an unlock for a route the rep is no longer on would be a
        // silent no-op on the new route — refuse it so the reviewer knows.
        var assignment = await _assignmentRepo.GetActiveTodayAssignmentForRepAsync(entity.UserId, ct);
        if (assignment is null || assignment.RouteId != entity.RouteId)
            throw new BusinessRuleException(
                "ROUTE_UNLOCK_ASSIGNMENT_CHANGED",
                "The rep's route for today has changed since they asked. Ask them to send a new request.");

        _repo.ApplyConcurrencyToken(entity, request.RowVersion);

        var now = DateTime.UtcNow;
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        entity.Status = RouteUnlockStatus.Approved;
        entity.ReviewedByUserId = callerId;
        entity.ReviewedByRole = callerRole.ToString();
        entity.ReviewedAt = now;
        entity.ReviewNote = note;
        entity.ValidFrom = now;
        // Exclusive Colombo midnight — a UTC-midnight cutoff would end it at 05:30.
        entity.ValidTo = SriLankaTime.StartOfDayUtc(today.AddDays(1));
        Touch(entity, callerId, now);
        entity.Events.Add(NewEvent(RouteUnlockAction.Approved, RouteUnlockStatus.Pending,
            RouteUnlockStatus.Approved, callerId, callerRole.ToString(), note, now));

        await _repo.SaveChangesAsync(ct);

        _logger.LogWarning(
            "Route unlock {RequestId} APPROVED by {CallerRole} {CallerId} for rep {UserId} on route {RouteId} until {ValidTo:o}",
            id, callerRole, callerId, entity.UserId, entity.RouteId, entity.ValidTo);

        var dto = await LoadDtoAsync(id, ct);
        await _notifications.SendToUserAsync(entity.UserId,
            "Route unlocked",
            $"{dto.ReviewedByName ?? "Your supervisor"} unlocked all outlets on {dto.RouteName} for today.",
            Data("ROUTE_UNLOCK_APPROVED", id), ct);
        await NotifySupervisorOfAdminActionAsync(entity, callerId, callerRole, "approved", dto, ct);
        return dto;
    }

    public async Task<RouteUnlockRequestDto> RejectAsync(
        int id, ReasonedRouteUnlockRequest request, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        await using var @lock = await AcquireRequestLockAsync(id, ct);

        var entity = await _repo.GetForUpdateAsync(id, ct)
            ?? throw new NotFoundException("RouteUnlockRequest", id);

        await EnsureCanReviewAsync(entity.UserId, callerId, callerRole, ct);

        if (entity.Status != RouteUnlockStatus.Pending)
            throw RouteUnlockConflictException.InvalidState(entity.Status.ToString());

        _repo.ApplyConcurrencyToken(entity, request.RowVersion);

        var now = DateTime.UtcNow;
        var reason = request.Reason.Trim();
        entity.Status = RouteUnlockStatus.Rejected;
        entity.ReviewedByUserId = callerId;
        entity.ReviewedByRole = callerRole.ToString();
        entity.ReviewedAt = now;
        entity.ReviewNote = reason;
        Touch(entity, callerId, now);
        entity.Events.Add(NewEvent(RouteUnlockAction.Rejected, RouteUnlockStatus.Pending,
            RouteUnlockStatus.Rejected, callerId, callerRole.ToString(), reason, now));

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Route unlock {RequestId} rejected by {CallerRole} {CallerId}", id, callerRole, callerId);

        var dto = await LoadDtoAsync(id, ct);
        await _notifications.SendToUserAsync(entity.UserId,
            "Unlock request rejected",
            $"{dto.ReviewedByName ?? "Your supervisor"} rejected your unlock request: {reason}",
            Data("ROUTE_UNLOCK_REJECTED", id), ct);
        await NotifySupervisorOfAdminActionAsync(entity, callerId, callerRole, "rejected", dto, ct);
        return dto;
    }

    public async Task<RouteUnlockRequestDto> RevokeAsync(
        int id, ReasonedRouteUnlockRequest request, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        await using var @lock = await AcquireRequestLockAsync(id, ct);

        var entity = await _repo.GetForUpdateAsync(id, ct)
            ?? throw new NotFoundException("RouteUnlockRequest", id);

        await EnsureCanReviewAsync(entity.UserId, callerId, callerRole, ct);

        var now = DateTime.UtcNow;
        // Only a live unlock can be revoked — an expired one has already ended.
        if (entity.Status != RouteUnlockStatus.Approved || entity.ValidTo <= now)
            throw RouteUnlockConflictException.InvalidState(
                entity.Status == RouteUnlockStatus.Approved ? "Expired" : entity.Status.ToString());

        _repo.ApplyConcurrencyToken(entity, request.RowVersion);

        var reason = request.Reason.Trim();
        entity.Status = RouteUnlockStatus.Revoked;
        entity.RevokedByUserId = callerId;
        entity.RevokedAt = now;
        entity.RevokeReason = reason;
        Touch(entity, callerId, now);
        entity.Events.Add(NewEvent(RouteUnlockAction.Revoked, RouteUnlockStatus.Approved,
            RouteUnlockStatus.Revoked, callerId, callerRole.ToString(), reason, now));

        await _repo.SaveChangesAsync(ct);

        _logger.LogWarning(
            "Route unlock {RequestId} REVOKED by {CallerRole} {CallerId}", id, callerRole, callerId);

        // Nothing to invalidate: the policy is resolved per request, never cached
        // with the per-route outlet list.
        var dto = await LoadDtoAsync(id, ct);
        await _notifications.SendToUserAsync(entity.UserId,
            "Route unlock ended",
            $"The unlock for {dto.RouteName} was ended by {dto.RevokedByName ?? "your supervisor"}: {reason}",
            Data("ROUTE_UNLOCK_REVOKED", id), ct);
        await NotifySupervisorOfAdminActionAsync(entity, callerId, callerRole, "revoked", dto, ct);
        return dto;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<IAsyncDisposable> AcquireRequestLockAsync(int id, CancellationToken ct)
        => await _lockService.AcquireAsync($"route-unlock:{id}", ct)
           ?? throw RouteUnlockConflictException.Busy();

    /// Admin → anyone. Supervisor → only reps who currently report to them.
    private async Task EnsureCanReviewAsync(int repId, int callerId, UserRole callerRole, CancellationToken ct)
    {
        switch (callerRole)
        {
            case UserRole.Admin:
                return;
            case UserRole.Supervisor:
                await _supervisorService.EnsureRepUnderSupervisorAsync(callerId, repId, ct);
                return;
            default:
                throw new AuthorizationException("route unlock requests");
        }
    }

    /// Null = unrestricted (Admin); otherwise the caller's direct reports.
    private async Task<IReadOnlyCollection<int>?> ResolveScopeAsync(int callerId, UserRole callerRole, CancellationToken ct)
    {
        return callerRole switch
        {
            UserRole.Admin => null,
            UserRole.Supervisor => (await _reportingLineRepo.GetDirectReportsAsync(callerId, ct))
                .Select(l => l.UserId).Distinct().ToList(),
            _ => throw new AuthorizationException("route unlock requests")
        };
    }

    /// When an admin acts on a rep who has a supervisor, tell the supervisor so
    /// they are not left looking at a request that silently vanished.
    private async Task NotifySupervisorOfAdminActionAsync(
        RouteUnlockRequest entity, int callerId, UserRole callerRole, string verb,
        RouteUnlockRequestDto dto, CancellationToken ct)
    {
        if (callerRole != UserRole.Admin || entity.SupervisorUserId is not { } supId || supId == callerId)
            return;

        var type = verb switch
        {
            "approved" => "ROUTE_UNLOCK_APPROVED",
            "rejected" => "ROUTE_UNLOCK_REJECTED",
            _ => "ROUTE_UNLOCK_REVOKED"
        };
        await _notifications.SendToUserAsync(supId,
            $"Unlock request {verb} by admin",
            $"An admin {verb} {dto.UserName}'s unlock request for {dto.RouteName}.",
            Data(type, entity.Id), ct);
    }

    private RouteUnlockRequestEvent NewEvent(
        RouteUnlockAction action, RouteUnlockStatus? from, RouteUnlockStatus to,
        int performedBy, string role, string? note, DateTime at)
    {
        var http = _httpContextAccessor.HttpContext;
        return new RouteUnlockRequestEvent
        {
            Action = action,
            FromStatus = from,
            ToStatus = to,
            PerformedByUserId = performedBy,
            PerformedByRole = role,
            PerformedAt = at,
            Note = note,
            IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
            CorrelationId = http?.Items["CorrelationId"]?.ToString()
        };
    }

    private static void Touch(RouteUnlockRequest entity, int callerId, DateTime now)
    {
        entity.UpdatedAt = now;
        entity.UpdatedBy = callerId;
    }

    private static Dictionary<string, string> Data(string type, int requestId) => new()
    {
        ["type"] = type,
        ["requestId"] = requestId.ToString()
    };

    /// Re-read after every save: xmin changed, and the client needs the new
    /// RowVersion (and the reviewer's name) to take its next action.
    private async Task<RouteUnlockRequestDto> LoadDtoAsync(int id, CancellationToken ct)
    {
        var fresh = await _repo.GetWithNamesAsync(id, ct)
            ?? throw new NotFoundException("RouteUnlockRequest", id);
        return MapToDto(fresh, DateTime.UtcNow);
    }

    public static string EffectiveStatusOf(RouteUnlockRequest e, DateOnly today, DateTime nowUtc) => e.Status switch
    {
        RouteUnlockStatus.Pending when e.BusinessDate < today => "Expired",
        RouteUnlockStatus.Approved when e.ValidTo <= nowUtc => "Expired",
        _ => e.Status.ToString()
    };

    private static RouteUnlockRequestDto MapToDto(RouteUnlockRequest e, DateTime nowUtc)
        => new(
            Id: e.Id,
            UserId: e.UserId,
            UserName: e.User?.Name ?? string.Empty,
            LoginName: e.User?.Username ?? string.Empty,
            RouteId: e.RouteId,
            RouteName: e.Route?.Name ?? string.Empty,
            BusinessDate: e.BusinessDate,
            Status: e.Status.ToString(),
            EffectiveStatus: EffectiveStatusOf(e, SriLankaTime.BusinessDateOf(nowUtc), nowUtc),
            IsCurrentlyEffective: e.Status == RouteUnlockStatus.Approved
                                  && e.ValidFrom <= nowUtc && e.ValidTo > nowUtc,
            RequestReason: e.RequestReason,
            RequestedAt: e.RequestedAt,
            RequestLatitude: e.RequestLatitude,
            RequestLongitude: e.RequestLongitude,
            RequestGpsAccuracyMeters: e.RequestGpsAccuracyMeters,
            SupervisorUserId: e.SupervisorUserId,
            SupervisorName: e.SupervisorUser?.Name,
            ReviewedByUserId: e.ReviewedByUserId,
            ReviewedByName: e.ReviewedByUser?.Name,
            ReviewedByRole: e.ReviewedByRole,
            ReviewedAt: e.ReviewedAt,
            ReviewNote: e.ReviewNote,
            ValidFrom: e.ValidFrom,
            ValidTo: e.ValidTo,
            RevokedByUserId: e.RevokedByUserId,
            RevokedByName: e.RevokedByUser?.Name,
            RevokedAt: e.RevokedAt,
            RevokeReason: e.RevokeReason,
            CancelledAt: e.CancelledAt,
            RowVersion: e.RowVersion);

    private static RouteUnlockRequestEventDto MapEvent(RouteUnlockRequestEvent e)
        => new(
            e.Id,
            e.Action.ToString(),
            e.FromStatus?.ToString(),
            e.ToStatus.ToString(),
            e.PerformedByUserId,
            e.PerformedByUser?.Name,
            e.PerformedByRole,
            e.PerformedAt,
            e.Note,
            e.IpAddress);
}
