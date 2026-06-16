using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Distributors.Requests;
using sfa_api.Features.Distributors.Services;

namespace sfa_api.Features.Distributors.Controllers;

[ApiController]
[Route("api/v1/distributors")]
[Authorize(Roles = "Admin")]
public class DistributorsController(
    IDistributorService distributorService,
    IValidator<CreateDistributorRequest> createValidator,
    IValidator<UpdateDistributorRequest> updateValidator) : ControllerBase
{
    private readonly IDistributorService _distributorService = distributorService;
    private readonly IValidator<CreateDistributorRequest> _createValidator = createValidator;
    private readonly IValidator<UpdateDistributorRequest> _updateValidator = updateValidator;

    /// <summary>
    /// GET /api/v1/distributors/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _distributorService.GetByIdAsync(id, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/distributors
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var isActive = status?.ToLower() switch
        {
            "active" => (bool?)true,
            "inactive" => (bool?)false,
            _ => null
        };
        var result = await _distributorService.GetAllAsync(page, pageSize, search, isActive, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/distributors
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDistributorRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _createValidator.ValidateOrThrowAsync(request, ct);

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _distributorService.CreateAsync(request, callerId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ResponseHelper.Created(result, correlationId));
    }

    /// <summary>
    /// PUT /api/v1/distributors/{id}
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDistributorRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _updateValidator.ValidateOrThrowAsync(request, ct);

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _distributorService.UpdateAsync(id, request, callerId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// DELETE /api/v1/distributors/{id}
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _distributorService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// POST /api/v1/distributors/{id}/activate
    /// </summary>
    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        await _distributorService.ActivateAsync(id, callerId, ct);
        return NoContent();
    }

    /// <summary>
    /// POST /api/v1/distributors/{id}/deactivate
    /// </summary>
    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        await _distributorService.DeactivateAsync(id, callerId, ct);
        return NoContent();
    }
}
