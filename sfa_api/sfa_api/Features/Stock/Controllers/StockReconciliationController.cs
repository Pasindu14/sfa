using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sfa_api.Common.Errors;
using sfa_api.Features.Stock.Services;

namespace sfa_api.Features.Stock.Controllers;

/// <summary>
/// Internal stock-ledger reconciliation (review finding #4): recomputes expected on-hand from the
/// transaction ledger and flags any group where it disagrees with the live DistributorStock balance.
/// Read-only over live data; admin-only. A nightly job runs the same check unattended.
/// </summary>
[ApiController]
[Route("api/v1/stock/reconciliation")]
[Authorize(Roles = "Admin")]
public class StockReconciliationController(IStockReconciliationService service) : ControllerBase
{
    private readonly IStockReconciliationService _service = service;

    /// <summary>
    /// GET /api/v1/stock/reconciliation?distributorId=&amp;productId=
    /// Runs a reconciliation pass now (optionally scoped) and returns the discrepancies.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Run(
        [FromQuery] int? distributorId,
        [FromQuery] int? productId,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _service.RunAsync(distributorId, productId, $"manual:{userId}", ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/stock/reconciliation/latest
    /// Returns the most recent persisted run (dashboard tile). 404 if none has run yet.
    /// </summary>
    [HttpGet("latest")]
    public async Task<IActionResult> Latest(CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetLatestRunAsync(ct)
            ?? throw new NotFoundException("StockReconciliationRun", 0);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
