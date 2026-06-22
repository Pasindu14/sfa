using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Outlets.Requests;
using sfa_api.Features.Outlets.Services;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.Outlets.Controllers;

[ApiController]
[Route("api/v1/outlets")]
public class OutletsController(
    IOutletService service,
    IValidator<CreateOutletRequest> createValidator,
    IValidator<UpdateOutletRequest> updateValidator) : ControllerBase
{
    private readonly IOutletService _service = service;
    private readonly IValidator<CreateOutletRequest> _createValidator = createValidator;
    private readonly IValidator<UpdateOutletRequest> _updateValidator = updateValidator;

    private (int callerId, UserRole callerRole) GetCallerInfo()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role) ?? string.Empty, out var callerRole);
        return (callerId, callerRole);
    }

    /// <summary>
    /// GET /api/v1/outlets/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _service.GetByIdAsync(id, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/outlets
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var isActive = status?.ToLower() switch
        {
            "active" => (bool?)true,
            "inactive" => (bool?)false,
            _ => null
        };
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _service.GetAllAsync(page, pageSize, callerId, callerRole, isActive, search, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/outlets/active
    /// </summary>
    [HttpGet("active")]
    [Authorize]
    public async Task<IActionResult> GetAllActive(CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetAllActiveAsync(ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/outlets/by-route/{routeId}
    /// Returns all active outlets for a route — used by mobile for offline sync.
    /// </summary>
    [HttpGet("by-route/{routeId:int}")]
    [Authorize]
    public async Task<IActionResult> GetByRoute(int routeId, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetByRouteIdAsync(routeId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/outlets/map-points
    /// Returns slim {id, name, latitude, longitude} for all active outlets with valid coordinates.
    /// </summary>
    [HttpGet("map-points")]
    [Authorize]
    public async Task<IActionResult> GetMapPoints(CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetMapPointsAsync(ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/outlets
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SalesRep")]
    public async Task<IActionResult> Create([FromBody] CreateOutletRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _createValidator.ValidateOrThrowAsync(request, ct);

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.CreateAsync(request, callerId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ResponseHelper.Created(result, correlationId));
    }

    /// <summary>
    /// PUT /api/v1/outlets/{id}
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOutletRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _updateValidator.ValidateOrThrowAsync(request, ct);

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.UpdateAsync(id, request, callerId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// DELETE /api/v1/outlets/{id}
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// POST /api/v1/outlets/{id}/activate
    /// </summary>
    [HttpPost("{id:int}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        await _service.ActivateAsync(id, callerId, ct);
        return NoContent();
    }

    /// <summary>
    /// POST /api/v1/outlets/{id}/deactivate
    /// </summary>
    [HttpPost("{id:int}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        await _service.DeactivateAsync(id, callerId, ct);
        return NoContent();
    }
}
