using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Features.SalesTargets.Services;

namespace sfa_api.Features.SalesTargets.Controllers;

[ApiController]
[Route("api/v1/sales-targets")]
[Authorize(Roles = "Admin,NSM,RSM,ASM,Supervisor,SalesRep,Distributor")]
public class RepSalesTargetController(ISalesTargetService targetService) : ControllerBase
{
    private int GetCallerId()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        return id;
    }

    /// <summary>GET /api/v1/sales-targets/my-monthly-target — monetary target total for the calling user.</summary>
    [HttpGet("my-monthly-target")]
    public async Task<IActionResult> GetMyMonthlyTarget(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await targetService.GetRepMonthlyTargetAsync(GetCallerId(), year, month, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
