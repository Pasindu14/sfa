using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.ProductCategoryPricings.Requests;
using sfa_api.Features.ProductCategoryPricings.Services;

namespace sfa_api.Features.ProductCategoryPricings.Controllers;

[ApiController]
[Route("api/v1/product-category-pricings")]
[Authorize(Roles = "Admin")]
public class ProductCategoryPricingsController(
    IProductCategoryPricingService service,
    IValidator<BulkUpsertPricingRequest> bulkUpsertValidator) : ControllerBase
{
    private readonly IProductCategoryPricingService _service = service;
    private readonly IValidator<BulkUpsertPricingRequest> _bulkUpsertValidator = bulkUpsertValidator;

    /// <summary>
    /// GET /api/v1/product-category-pricings
    /// Returns all active products with their A/B/C/D category prices.
    /// Products without any pricing rows are included with prices of 0.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetAllAsync(ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// PUT /api/v1/product-category-pricings
    /// Bulk upserts category prices for all submitted products.
    /// Missing rows are created; existing rows are updated.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> BulkUpsert([FromBody] BulkUpsertPricingRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _bulkUpsertValidator.ValidateOrThrowAsync(request, ct);

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        await _service.BulkUpsertAsync(request, callerId, ct);

        return Ok(ResponseHelper.Ok("Pricing saved successfully.", correlationId));
    }
}
