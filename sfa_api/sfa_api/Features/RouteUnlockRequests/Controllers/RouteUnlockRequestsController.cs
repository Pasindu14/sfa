using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.RouteUnlockRequests.Requests;
using sfa_api.Features.RouteUnlockRequests.Services;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.RouteUnlockRequests.Controllers;

/// <summary>
/// Rep asks to lift the billing geofence for today's route; the rep's supervisor
/// or an admin decides. Role attributes are the coarse gate; supervisor-to-rep
/// scoping is enforced in the service.
/// </summary>
[ApiController]
[Route("api/v1/route-unlock-requests")]
[Authorize]
public class RouteUnlockRequestsController(
    IRouteUnlockRequestService service,
    IValidator<CreateRouteUnlockRequest> createValidator,
    IValidator<ApproveRouteUnlockRequest> approveValidator,
    IValidator<ReasonedRouteUnlockRequest> reasonValidator,
    IValidator<CancelRouteUnlockRequest> cancelValidator) : ControllerBase
{
    private readonly IRouteUnlockRequestService _service = service;
    private readonly IValidator<CreateRouteUnlockRequest> _createValidator = createValidator;
    private readonly IValidator<ApproveRouteUnlockRequest> _approveValidator = approveValidator;
    private readonly IValidator<ReasonedRouteUnlockRequest> _reasonValidator = reasonValidator;
    private readonly IValidator<CancelRouteUnlockRequest> _cancelValidator = cancelValidator;

    private string CorrelationId => HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

    private int GetCallerId()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        return id;
    }

    private UserRole GetCallerRole()
    {
        if (!Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var role))
            throw new AuthorizationException("route unlock requests");
        return role;
    }

    // ── Rep ───────────────────────────────────────────────────────────────

    /// <summary>POST /api/v1/route-unlock-requests — ask to unlock today's route.</summary>
    [HttpPost]
    [Authorize(Roles = "SalesRep")]
    public async Task<IActionResult> Create([FromBody] CreateRouteUnlockRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateOrThrowAsync(request, ct);
        var result = await _service.CreateAsync(request, GetCallerId(), ct);
        return Ok(ResponseHelper.Created(result, CorrelationId));
    }

    /// <summary>GET /api/v1/route-unlock-requests/my/today — latest request for today, or null.</summary>
    [HttpGet("my/today")]
    [Authorize(Roles = "SalesRep")]
    public async Task<IActionResult> GetMyToday(CancellationToken ct)
    {
        var result = await _service.GetMyTodayAsync(GetCallerId(), ct);
        return Ok(ResponseHelper.Ok(result, CorrelationId));
    }

    /// <summary>POST /api/v1/route-unlock-requests/{id}/cancel — rep withdraws a pending request.</summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "SalesRep")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelRouteUnlockRequest request, CancellationToken ct)
    {
        await _cancelValidator.ValidateOrThrowAsync(request, ct);
        var result = await _service.CancelAsync(id, request, GetCallerId(), ct);
        return Ok(ResponseHelper.Ok(result, CorrelationId));
    }

    // ── Reviewers ─────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/route-unlock-requests — paged; supervisors see only their reps.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetPaged([FromQuery] RouteUnlockListQuery query, CancellationToken ct)
    {
        var result = await _service.GetPagedAsync(query, GetCallerId(), GetCallerRole(), ct);
        return Ok(ResponseHelper.Paged(
            result.Items, result.Page, result.PageSize, result.TotalCount, CorrelationId));
    }

    /// <summary>GET /api/v1/route-unlock-requests/pending-count — today's pending, scoped.</summary>
    [HttpGet("pending-count")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetPendingCount(CancellationToken ct)
    {
        var result = await _service.GetPendingCountAsync(GetCallerId(), GetCallerRole(), ct);
        return Ok(ResponseHelper.Ok(result, CorrelationId));
    }

    /// <summary>GET /api/v1/route-unlock-requests/{id} — request, timeline and bills that used it.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Supervisor,SalesRep")]
    public async Task<IActionResult> GetDetail(int id, CancellationToken ct)
    {
        var result = await _service.GetDetailAsync(id, GetCallerId(), GetCallerRole(), ct);
        return Ok(ResponseHelper.Ok(result, CorrelationId));
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveRouteUnlockRequest request, CancellationToken ct)
    {
        await _approveValidator.ValidateOrThrowAsync(request, ct);
        var result = await _service.ApproveAsync(id, request, GetCallerId(), GetCallerRole(), ct);
        return Ok(ResponseHelper.Ok(result, CorrelationId));
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> Reject(int id, [FromBody] ReasonedRouteUnlockRequest request, CancellationToken ct)
    {
        await _reasonValidator.ValidateOrThrowAsync(request, ct);
        var result = await _service.RejectAsync(id, request, GetCallerId(), GetCallerRole(), ct);
        return Ok(ResponseHelper.Ok(result, CorrelationId));
    }

    /// Not a DELETE: the row is audit history and is never removed.
    [HttpPost("{id:int}/revoke")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> Revoke(int id, [FromBody] ReasonedRouteUnlockRequest request, CancellationToken ct)
    {
        await _reasonValidator.ValidateOrThrowAsync(request, ct);
        var result = await _service.RevokeAsync(id, request, GetCallerId(), GetCallerRole(), ct);
        return Ok(ResponseHelper.Ok(result, CorrelationId));
    }
}
