using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.UserGeoAssignments.Requests;
using sfa_api.Features.UserGeoAssignments.Services;

namespace sfa_api.Features.UserGeoAssignments.Controllers;

[ApiController]
[Route("api/v1/user-assignments")]
[Authorize(Roles = "Admin")]
public class UserAssignmentsController(
    IUserGeoAssignmentService service,
    IValidator<CreateUserAssignmentRequest> createValidator,
    IValidator<UpdateUserAssignmentRequest> updateValidator) : ControllerBase
{
    private readonly IUserGeoAssignmentService _service = service;
    private readonly IValidator<CreateUserAssignmentRequest> _createValidator = createValidator;
    private readonly IValidator<UpdateUserAssignmentRequest> _updateValidator = updateValidator;

    /// <summary>GET /api/v1/user-assignments/stats — 4 stat card numbers</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetStatsAsync(ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>GET /api/v1/user-assignments/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/user-assignments
    /// Supports: ?search= ?role= ?regionId= ?isActive=
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] int? regionId = null,
        [FromQuery] int? areaId = null,
        [FromQuery] int? territoryId = null,
        [FromQuery] int? divisionId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetAllAsync(page, pageSize, search, role, regionId, areaId, territoryId, divisionId, isActive, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>POST /api/v1/user-assignments — create, writes to both tables atomically</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserAssignmentRequest request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        await _createValidator.ValidateOrThrowAsync(request, ct);
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.CreateAsync(request, callerId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ResponseHelper.Created(result, correlationId));
    }

    /// <summary>PUT /api/v1/user-assignments/{id} — update both tables atomically</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateUserAssignmentRequest request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        await _updateValidator.ValidateOrThrowAsync(request, ct);
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.UpdateAsync(id, request, callerId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>DELETE /api/v1/user-assignments/{id} — deactivate (IsActive = false)</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        await _service.DeleteAsync(id, callerId, ct);
        return NoContent();
    }

    /// <summary>POST /api/v1/user-assignments/{id}/activate — reactivate a deactivated assignment</summary>
    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.ActivateAsync(id, callerId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
