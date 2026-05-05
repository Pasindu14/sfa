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
}
