using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sfa_api.Common.Errors;
using sfa_api.Features.ProductCategoryPricings.Services;

namespace sfa_api.Features.ProductCategoryPricings.Controllers;

[ApiController]
[Route("api/v1/product-category-pricings")]
[Authorize(Roles = "Distributor")]
public class ProductCategoryPricingPortalController(
    IProductCategoryPricingService pricingService) : ControllerBase
{
    private readonly IProductCategoryPricingService _pricingService = pricingService;

    /// <summary>
    /// GET /api/v1/product-category-pricings/portal
    /// Distributor only — returns all product category pricing rows (priceA/B/C/D).
    /// Used by the distributor portal create page to auto-fill unit prices.
    /// </summary>
    [HttpGet("portal")]
    public async Task<IActionResult> GetForPortal(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _pricingService.GetAllAsync(ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
