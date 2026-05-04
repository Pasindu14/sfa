using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Supervisor.Services;

namespace sfa_api.Features.Supervisor.Controllers;

[ApiController]
[Route("api/v1/supervisor")]
[Authorize(Roles = "Supervisor")]
public class SupervisorController(ISupervisorService service) : ControllerBase
{
    private readonly ISupervisorService _service = service;

    /// <summary>GET /api/v1/supervisor/summary?date=YYYY-MM-DD</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateOnly? date = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var supervisorId);
        var summaryDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _service.GetSummaryAsync(supervisorId, summaryDate, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
