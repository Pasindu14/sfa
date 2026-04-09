using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sfa_api.Common.Errors;
using sfa_api.Features.MobileSync.Services;

namespace sfa_api.Features.MobileSync.Controllers;

[ApiController]
[Route("api/v1/mobile")]
[Authorize(Roles = "SalesRep")]
public class MobileSyncController(IMobileSyncService mobileSyncService) : ControllerBase
{
    private readonly IMobileSyncService _mobileSyncService = mobileSyncService;

    /// <summary>
    /// GET /api/v1/mobile/products
    /// Returns all active products for mobile catalog sync.
    /// Cached server-side for 1 hour; evicted on any product mutation.
    /// </summary>
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _mobileSyncService.GetProductsAsync(ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
