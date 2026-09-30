using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sfa_api.Common.Errors;
using sfa_api.Features.RepTimelines.Services;

namespace sfa_api.Features.RepTimelines.Controllers;

[ApiController]
[Route("api/v1/rep-timeline")]
[Authorize(Roles = "Admin")]
public class RepTimelineController(IRepTimelineService service) : ControllerBase
{
    private readonly IRepTimelineService _service = service;

    /// <summary>
    /// GET /api/v1/rep-timeline/{repId}?date=yyyy-MM-dd — one rep's business day: GPS route plus
    /// every bill, no-sale visit, GPS gap, unexplained stop and unlock event, in order.
    /// Admin only — this is staff movement history.
    /// </summary>
    [HttpGet("{repId:int}")]
    public async Task<IActionResult> Get(int repId, [FromQuery] DateOnly date, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetAsync(repId, date, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
