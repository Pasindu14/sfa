using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sfa_api.Common.Errors;
using sfa_api.Features.Dashboard.Services;

namespace sfa_api.Features.Dashboard.Controllers;

/// <summary>
/// Company-wide admin dashboard for one business day (default today, Asia/Colombo). Served as
/// sections (sales, activity, trend, breakdown) so the client can load them in parallel and render
/// each independently.
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Roles = "Admin")]
public class DashboardController(IDashboardService service) : ControllerBase
{
    private readonly IDashboardService _service = service;

    private string CorrelationId => HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

    /// <summary>GET /api/v1/dashboard/sales[?date=YYYY-MM-DD] — the day's and month-to-date revenue vs target, discounts, returns, bills and the regional breakdown.</summary>
    [HttpGet("sales")]
    public async Task<IActionResult> GetSales([FromQuery] DateOnly? date = null, CancellationToken ct = default)
        => Ok(ResponseHelper.Ok(await _service.GetSalesAsync(date, ct), CorrelationId));

    /// <summary>GET /api/v1/dashboard/activity[?date=YYYY-MM-DD] — active reps, outlet/customer counts, 45-day billed-outlet coverage and new outlets.</summary>
    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity([FromQuery] DateOnly? date = null, CancellationToken ct = default)
        => Ok(ResponseHelper.Ok(await _service.GetActivityAsync(date, ct), CorrelationId));

    /// <summary>GET /api/v1/dashboard/trend[?date=YYYY-MM-DD] — revenue per day for the month up to the date.</summary>
    [HttpGet("trend")]
    public async Task<IActionResult> GetTrend([FromQuery] DateOnly? date = null, CancellationToken ct = default)
        => Ok(ResponseHelper.Ok(await _service.GetTrendAsync(date, ct), CorrelationId));

    /// <summary>GET /api/v1/dashboard/breakdown[?date=YYYY-MM-DD] — month-to-date top products, reps and distributors, and no-sale reasons.</summary>
    [HttpGet("breakdown")]
    public async Task<IActionResult> GetBreakdown([FromQuery] DateOnly? date = null, CancellationToken ct = default)
        => Ok(ResponseHelper.Ok(await _service.GetBreakdownAsync(date, ct), CorrelationId));
}
