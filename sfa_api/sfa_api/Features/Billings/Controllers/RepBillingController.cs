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

    /// <summary>GET /api/v1/billings/my-bills — paginated bill list for the calling rep. Filter by date range or exact bill number.</summary>
    [HttpGet("my-bills")]
    public async Task<IActionResult> GetMyBills(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? billNo = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var salesRepId = GetCallerId();

        DateOnly? parsedDateFrom = DateOnly.TryParse(dateFrom, out var df) ? df : null;
        DateOnly? parsedDateTo   = DateOnly.TryParse(dateTo,   out var dt) ? dt : null;

        var (items, total) = await billingService.GetListAsync(
            page, pageSize,
            repStatus: null, distributorStatus: null,
            outletId: null, distributorId: null,
            salesRepId: salesRepId,
            parsedDateFrom, parsedDateTo,
            billNo: billNo,
            ct: ct);

        return Ok(ResponseHelper.Paged(items, page, pageSize, total, correlationId));
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
