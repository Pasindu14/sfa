using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.UserProximityExemptions.Requests;
using sfa_api.Features.UserProximityExemptions.Services;

namespace sfa_api.Features.UserProximityExemptions.Controllers;

/// <summary>
/// Admin-only management of billing-geofence exemptions.
///
/// Authorization sits on the endpoints, not on whether the web UI draws the
/// button - hiding a control is cosmetic, this is the actual gate.
/// </summary>
[ApiController]
[Route("api/v1/proximity-exemptions")]
[Authorize(Roles = "Admin")]
public class ProximityExemptionsController(
    IUserProximityExemptionService service,
    IValidator<GrantProximityExemptionRequest> grantValidator,
    IValidator<RevokeProximityExemptionRequest> revokeValidator) : ControllerBase
{
    private readonly IUserProximityExemptionService _service = service;
    private readonly IValidator<GrantProximityExemptionRequest> _grantValidator = grantValidator;
    private readonly IValidator<RevokeProximityExemptionRequest> _revokeValidator = revokeValidator;

    private string CorrelationId => HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

    private int GetCallerId()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        return id;
    }

    /// <summary>GET /api/v1/proximity-exemptions - every rep currently exempt.</summary>
    [HttpGet]
    public async Task<IActionResult> GetActive(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _service.GetActiveAsync(page, pageSize, search, ct);
        return Ok(ResponseHelper.Paged(
            result.Items, result.Page, result.PageSize, result.TotalCount, CorrelationId));
    }

    /// <summary>GET /api/v1/proximity-exemptions/user/{userId} - grant history for one rep.</summary>
    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetHistory(int userId, CancellationToken ct)
    {
        var result = await _service.GetHistoryAsync(userId, ct);
        return Ok(ResponseHelper.Ok(result, CorrelationId));
    }

    /// <summary>
    /// GET /api/v1/proximity-exemptions/user/{userId}/current - the grant in force
    /// now, or 200 with null data when the rep is not exempt.
    /// </summary>
    [HttpGet("user/{userId:int}/current")]
    public async Task<IActionResult> GetCurrent(int userId, CancellationToken ct)
    {
        var result = await _service.GetCurrentAsync(userId, ct);
        return Ok(ResponseHelper.Ok(result, CorrelationId));
    }

    /// <summary>POST /api/v1/proximity-exemptions/user/{userId} - grant an exemption.</summary>
    [HttpPost("user/{userId:int}")]
    public async Task<IActionResult> Grant(
        int userId, [FromBody] GrantProximityExemptionRequest request, CancellationToken ct)
    {
        await _grantValidator.ValidateOrThrowAsync(request, ct);
        var result = await _service.GrantAsync(userId, request, GetCallerId(), ct);
        return Ok(ResponseHelper.Ok(result, CorrelationId));
    }

    /// <summary>
    /// POST /api/v1/proximity-exemptions/{id}/revoke - end a grant early.
    /// Not a DELETE: the row is audit history and is never removed.
    /// </summary>
    [HttpPost("{id:int}/revoke")]
    public async Task<IActionResult> Revoke(
        int id, [FromBody] RevokeProximityExemptionRequest request, CancellationToken ct)
    {
        await _revokeValidator.ValidateOrThrowAsync(request, ct);
        var result = await _service.RevokeAsync(id, request, GetCallerId(), ct);
        return Ok(ResponseHelper.Ok(result, CorrelationId));
    }
}
