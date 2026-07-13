using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sfa_api.Common.Errors;
using sfa_api.Features.GeoConsistency.Services;

namespace sfa_api.Features.GeoConsistency.Controllers;

/// <summary>
/// Geo hierarchy self-consistency (companion to the re-parent cascade): verifies that every live
/// descendant's denormalized ancestor IDs still match its parent, and repairs drift if a cascade was
/// ever missed. Read-only checks + an idempotent backfill; admin-only. A nightly job runs the check
/// unattended. Never touches the frozen transaction tables (Billing / NotBilling / SalesTarget).
/// </summary>
[ApiController]
[Route("api/v1/geo-consistency")]
[Authorize(Roles = "Admin")]
public class GeoConsistencyController(IGeoConsistencyService service) : ControllerBase
{
    private readonly IGeoConsistencyService _service = service;

    /// <summary>GET /api/v1/geo-consistency — runs a drift scan now and returns the result.</summary>
    [HttpGet]
    public async Task<IActionResult> Run(CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _service.RunAsync($"manual:{userId}", ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>GET /api/v1/geo-consistency/latest — most recent persisted run (dashboard tile). 404 if none.</summary>
    [HttpGet("latest")]
    public async Task<IActionResult> Latest(CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetLatestRunAsync(ct)
            ?? throw new NotFoundException("GeoConsistencyRun", 0);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/geo-consistency/repair — idempotent top-down backfill that re-derives every live
    /// descendant's ancestor IDs from its parent. Corrects any pre-existing drift (e.g. legacy rows
    /// created before the cascade existed). Safe to run repeatedly.
    /// </summary>
    [HttpPost("repair")]
    public async Task<IActionResult> Repair(CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.RepairAsync(ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
