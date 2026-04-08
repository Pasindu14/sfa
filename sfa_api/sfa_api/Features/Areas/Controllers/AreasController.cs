using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Areas.Requests;
using sfa_api.Features.Areas.Services;

namespace sfa_api.Features.Areas.Controllers;

[ApiController]
[Route("api/v1/areas")]
public class AreasController(
    IAreaService service,
    IValidator<CreateAreaRequest> createValidator,
    IValidator<UpdateAreaRequest> updateValidator) : ControllerBase
{
    private readonly IAreaService _service = service;
    private readonly IValidator<CreateAreaRequest> _createValidator = createValidator;
    private readonly IValidator<UpdateAreaRequest> _updateValidator = updateValidator;

    private int GetCallerId()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        return callerId;
    }

    /// <summary>
    /// GET /api/v1/areas/{id}
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/areas
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? regionId = null,
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
        var result = await _service.GetAllAsync(page, pageSize, regionId, isActive, search, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/areas/active
    /// </summary>
    [HttpGet("active")]
    [Authorize]
    public async Task<IActionResult> GetAllActive(
        [FromQuery] int? regionId = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetAllActiveAsync(regionId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/areas
    /// Callers should send X-Idempotency-Key to prevent duplicate submissions on retries.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("user")]
    public async Task<IActionResult> Create([FromBody] CreateAreaRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _createValidator.ValidateOrThrowAsync(request, ct);

        var result = await _service.CreateAsync(request, GetCallerId(), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ResponseHelper.Created(result, correlationId));
    }

    /// <summary>
    /// PUT /api/v1/areas/{id}
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("user")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAreaRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _updateValidator.ValidateOrThrowAsync(request, ct);

        var result = await _service.UpdateAsync(id, request, GetCallerId(), ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/areas/{id}/activate
    /// </summary>
    [HttpPost("{id}/activate")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("user")]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        await _service.ActivateAsync(id, GetCallerId(), ct);
        return NoContent();
    }

    /// <summary>
    /// POST /api/v1/areas/{id}/deactivate
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("user")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        await _service.DeactivateAsync(id, GetCallerId(), ct);
        return NoContent();
    }

    /// <summary>
    /// DELETE /api/v1/areas/{id}
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("user")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, GetCallerId(), ct);
        return NoContent();
    }
}
