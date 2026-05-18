using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Features.Billings.Services;

namespace sfa_api.Features.Billings.Controllers;

[ApiController]
[Route("api/v1/billings")]
[Authorize(Roles = "Admin,NSM,RSM,ASM,Supervisor,SalesRep,Distributor")]
public class RepBillingController(IBillingService billingService) : ControllerBase
{
    private int GetCallerId()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        return id;
    }

    /// <summary>GET /api/v1/billings/my-monthly-sales — MTD sales total for the calling user.</summary>
    [HttpGet("my-monthly-sales")]
    public async Task<IActionResult> GetMyMonthlySales(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await billingService.GetRepMonthlySalesAsync(GetCallerId(), year, month, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>GET /api/v1/billings/my-daily-sales?date=YYYY-MM-DD — today's approved and pending sales totals for the calling rep.</summary>
    [HttpGet("my-daily-sales")]
    public async Task<IActionResult> GetMyDailySales(
        [FromQuery] DateOnly date,
        CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await billingService.GetRepDailySalesAsync(GetCallerId(), date, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>GET /api/v1/billings/my-monthly-sales-itemwise — per-product target vs sold for the calling rep.</summary>
    [HttpGet("my-monthly-sales-itemwise")]
    public async Task<IActionResult> GetMyMonthlySalesItemwise(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await billingService.GetRepMonthlySalesItemwiseAsync(GetCallerId(), year, month, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
