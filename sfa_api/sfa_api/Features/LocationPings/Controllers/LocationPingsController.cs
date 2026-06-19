using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using sfa_api.Common.Errors;
using sfa_api.Features.LocationPings.Requests;
using sfa_api.Features.LocationPings.Services;

namespace sfa_api.Features.LocationPings.Controllers;

[ApiController]
[Route("api/v1/location-pings")]
[Authorize]
[EnableRateLimiting("user")]
public class LocationPingsController(
    ILocationPingService service,
    IValidator<CreateLocationPingsRequest> validator) : ControllerBase
{
    /// <summary>
    /// Accepts a batch of location pings from the rep's mobile app.
    /// RepId is resolved server-side from the JWT — never trusted from the body.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateLocationPingsRequest request,
        CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var fields = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return BadRequest(new ApiErrorResponse(false,
                new ApiError("VALIDATION_FAILED", "Validation failed.", null, fields, null, correlationId, DateTime.UtcNow)));
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var repId))
            return Unauthorized();

        var count = await service.RecordAsync(repId, request, ct);
        return CreatedAtAction(nameof(Create), ResponseHelper.Created(new { accepted = count }, correlationId));
    }

    /// <summary>
    /// Returns the most-recent location ping for every active rep.
    /// Admin only — used by the supervisor live map dashboard.
    /// </summary>
    [HttpGet("latest")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetLatest(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await service.GetLatestPerRepAsync(ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
