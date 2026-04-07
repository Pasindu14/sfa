using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.DailyRouteAssignments.Requests;
using sfa_api.Features.DailyRouteAssignments.Services;

namespace sfa_api.Features.DailyRouteAssignments.Controllers;

[ApiController]
[Route("api/v1/daily-route-assignments")]
[Authorize(Roles = "Admin,Supervisor")]
public class DailyRouteAssignmentsController(
    IDailyRouteAssignmentService service,
    IValidator<CreateDailyRouteAssignmentRequest> createValidator) : ControllerBase
{
    private readonly IDailyRouteAssignmentService _service = service;
    private readonly IValidator<CreateDailyRouteAssignmentRequest> _createValidator = createValidator;

    /// <summary>GET /api/v1/daily-route-assignments — list (filterable by userId, routeId, date)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? userId = null,
        [FromQuery] int? routeId = null,
        [FromQuery] DateOnly? date = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetAllAsync(page, pageSize, userId, routeId, date, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>GET /api/v1/daily-route-assignments/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/daily-route-assignments/my-reps
    /// Returns sales reps with an active reporting line to the current supervisor.
    /// </summary>
    [HttpGet("my-reps")]
    [Authorize(Roles = "Supervisor")]
    public async Task<IActionResult> GetMyReps(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var supervisorId);
        var result = await _service.GetMyRepsAsync(supervisorId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/daily-route-assignments/rep-routes/{userId}
    /// Returns active routes in the rep's assigned division.
    /// </summary>
    [HttpGet("rep-routes/{userId:int}")]
    public async Task<IActionResult> GetRepRoutes(int userId, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetRepRoutesAsync(userId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>POST /api/v1/daily-route-assignments — create a new daily route assignment</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDailyRouteAssignmentRequest request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        await _createValidator.ValidateOrThrowAsync(request, ct);
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.CreateAsync(request, callerId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ResponseHelper.Created(result, correlationId));
    }

    /// <summary>DELETE /api/v1/daily-route-assignments/{id} — cancel / soft-delete an assignment</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        await _service.DeleteAsync(id, callerId, ct);
        return NoContent();
    }
}
