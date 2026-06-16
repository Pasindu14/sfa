using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.UserReportingLines.Requests;
using sfa_api.Features.UserReportingLines.Services;

namespace sfa_api.Features.UserReportingLines.Controllers;

[ApiController]
[Route("api/v1/user-reporting-lines")]
[Authorize(Roles = "Admin")]
public class UserReportingLinesController(
    IUserReportingLineService service,
    IValidator<CreateUserReportingLineRequest> createValidator,
    IValidator<UpdateUserReportingLineRequest> updateValidator) : ControllerBase
{
    private readonly IUserReportingLineService _service = service;
    private readonly IValidator<CreateUserReportingLineRequest> _createValidator = createValidator;
    private readonly IValidator<UpdateUserReportingLineRequest> _updateValidator = updateValidator;

    /// <summary>GET /api/v1/user-reporting-lines/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/user-reporting-lines
    /// Supports: ?search= ?role= ?reportsToUserId= ?isActive=
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] int? reportsToUserId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetAllAsync(page, pageSize, search, role, reportsToUserId, isActive, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/user-reporting-lines/{userId}/subordinates
    /// Returns direct reports when ?depth=1; full subtree otherwise.
    /// </summary>
    [HttpGet("{userId:int}/subordinates")]
    public async Task<IActionResult> GetSubordinates(
        int userId,
        [FromQuery] int? depth = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var directOnly = depth == 1;
        var result = await _service.GetSubordinatesAsync(userId, directOnly, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>POST /api/v1/user-reporting-lines</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserReportingLineRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        await _createValidator.ValidateOrThrowAsync(request, ct);
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.CreateAsync(request, callerId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ResponseHelper.Created(result, correlationId));
    }

    /// <summary>PUT /api/v1/user-reporting-lines/{id}</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserReportingLineRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        await _updateValidator.ValidateOrThrowAsync(request, ct);
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.UpdateAsync(id, request, callerId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>DELETE /api/v1/user-reporting-lines/{id} — deactivate (IsActive = false)</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        await _service.DeleteAsync(id, callerId, ct);
        return NoContent();
    }

    /// <summary>POST /api/v1/user-reporting-lines/{id}/activate — reactivate (IsActive = true)</summary>
    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        await _service.ActivateAsync(id, callerId, ct);
        return NoContent();
    }
}
