using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Routes.Requests;
using sfa_api.Features.Routes.Services;

namespace sfa_api.Features.Routes.Controllers;

[ApiController]
[Route("api/v1/routes")]
public class RoutesController(
    IRouteService service,
    IValidator<CreateRouteRequest> createValidator,
    IValidator<UpdateRouteRequest> updateValidator) : ControllerBase
{
    private readonly IRouteService _service = service;
    private readonly IValidator<CreateRouteRequest> _createValidator = createValidator;
    private readonly IValidator<UpdateRouteRequest> _updateValidator = updateValidator;

    /// <summary>
    /// GET /api/v1/routes/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/routes
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
        var result = await _service.GetAllAsync(page, pageSize, isActive, search, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/routes/active
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
    /// GET /api/v1/routes/by-division/{divisionId} — active routes for a specific division
    /// </summary>
    [HttpGet("by-division/{divisionId:int}")]
    [Authorize]
    public async Task<IActionResult> GetByDivision(int divisionId, CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetActiveByDivisionIdAsync(divisionId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/routes
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateRouteRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _createValidator.ValidateOrThrowAsync(request, ct);

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.CreateAsync(request, callerId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ResponseHelper.Created(result, correlationId));
    }

    /// <summary>
    /// PUT /api/v1/routes/{id}
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRouteRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _updateValidator.ValidateOrThrowAsync(request, ct);

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.UpdateAsync(id, request, callerId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/routes/{id}/activate
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
    /// POST /api/v1/routes/{id}/deactivate
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
