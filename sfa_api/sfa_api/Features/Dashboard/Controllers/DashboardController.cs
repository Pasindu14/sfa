using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sfa_api.Common.Errors;
using sfa_api.Features.Dashboard.Services;

namespace sfa_api.Features.Dashboard.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Roles = "Admin")]
public class DashboardController(IDashboardService service) : ControllerBase
{
    private readonly IDashboardService _service = service;

    /// <summary>
    /// GET /api/v1/dashboard[?date=YYYY-MM-DD]
    /// Company-wide monitoring snapshot for one business day (default today, Asia/Colombo):
    /// the day's and month-to-date revenue vs target, discounts, returns, active reps, outlet counts,
    /// 45-day billed-outlet coverage, the month's daily trend and a regional breakdown.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateOnly? date = null, CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetAsync(date, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
