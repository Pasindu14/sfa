using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Supervisor.Services;
using sfa_api.Features.Billings.Services;
using sfa_api.Features.SalesTargets.Services;

namespace sfa_api.Features.Supervisor.Controllers;

[ApiController]
[Route("api/v1/supervisor")]
[Authorize(Roles = "Supervisor")]
public class SupervisorController(
    ISupervisorService service,
    IBillingService billingService,
    ISalesTargetService salesTargetService) : ControllerBase
{
    private readonly ISupervisorService _service = service;
    private readonly IBillingService _billingService = billingService;
    private readonly ISalesTargetService _salesTargetService = salesTargetService;

    private int GetSupervisorId()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        return id;
    }

    /// <summary>GET /api/v1/supervisor/summary?date=YYYY-MM-DD</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateOnly? date = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var supervisorId);
        var summaryDate = date ?? SriLankaTime.Today;
        var result = await _service.GetSummaryAsync(supervisorId, summaryDate, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>GET /api/v1/supervisor/rep-achievement-itemwise?userId=X&amp;year=Y&amp;month=M</summary>
    [HttpGet("rep-achievement-itemwise")]
    public async Task<IActionResult> GetRepAchievementItemwise(
        [FromQuery] int userId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        await _service.EnsureRepUnderSupervisorAsync(GetSupervisorId(), userId, ct);
        var result = await _billingService.GetRepMonthlySalesItemwiseAsync(userId, year, month, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>GET /api/v1/supervisor/rep-monthly-sales?userId=X&amp;year=Y&amp;month=M</summary>
    [HttpGet("rep-monthly-sales")]
    public async Task<IActionResult> GetRepMonthlySales(
        [FromQuery] int userId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        await _service.EnsureRepUnderSupervisorAsync(GetSupervisorId(), userId, ct);
        var result = await _billingService.GetRepMonthlySalesAsync(userId, year, month, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>GET /api/v1/supervisor/rep-monthly-target?userId=X&amp;year=Y&amp;month=M</summary>
    [HttpGet("rep-monthly-target")]
    public async Task<IActionResult> GetRepMonthlyTarget(
        [FromQuery] int userId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        await _service.EnsureRepUnderSupervisorAsync(GetSupervisorId(), userId, ct);
        var result = await _salesTargetService.GetRepMonthlyTargetAsync(userId, year, month, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
