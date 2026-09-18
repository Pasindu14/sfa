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
    /// Conditional GET: 200 carries a weak <c>ETag</c>; a matching <c>If-None-Match</c> gets
    /// 304 Not Modified with an empty body. Without If-None-Match the response is unchanged.
    /// </summary>
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _mobileSyncService.GetProductsAsync(ct);

        var etag = MobileSyncETag.Compute(result.Products);
        Response.Headers.ETag = etag;
        if (MobileSyncETag.IfNoneMatchMatches(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);

        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/mobile/product-categories
    /// Returns all active product categories for mobile catalog sync.
    /// Cached server-side for 1 hour.
    /// Conditional GET: same ETag / If-None-Match / 304 contract as /products.
    /// </summary>
    [HttpGet("product-categories")]
    public async Task<IActionResult> GetProductCategories(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _mobileSyncService.GetProductCategoriesAsync(ct);

        var etag = MobileSyncETag.Compute(result.Categories);
        Response.Headers.ETag = etag;
        if (MobileSyncETag.IfNoneMatchMatches(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);

        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/mobile/pricing-structures
    /// Every ACTIVE pricing structure with its priced products (inactive structures never reach the
    /// phone). The default is flagged <c>isDefault</c> and listed first.
    /// Cached server-side for 1 hour; evicted on any structure, item or product mutation.
    /// Conditional GET: same ETag / If-None-Match / 304 contract as /products.
    /// </summary>
    [HttpGet("pricing-structures")]
    public async Task<IActionResult> GetPricingStructures(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _mobileSyncService.GetPricingStructuresAsync(ct);

        var etag = MobileSyncETag.Compute(result.PricingStructures);
        Response.Headers.ETag = etag;
        if (MobileSyncETag.IfNoneMatchMatches(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);

        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
