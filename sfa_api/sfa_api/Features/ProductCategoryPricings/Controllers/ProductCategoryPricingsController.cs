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
[Authorize]
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetAllAsync(ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/product-category-pricings/for-distributor/{distributorId}
    /// Returns all active products with a single resolved unit price for the distributor's category.
    /// Accessible by Supervisor and Admin roles.
    /// </summary>
    [HttpGet("for-distributor/{distributorId:int}")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetForDistributor(int distributorId, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetForDistributorAsync(distributorId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// PUT /api/v1/product-category-pricings
    /// Bulk upserts category prices for all submitted products.
    /// Missing rows are created; existing rows are updated.
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkUpsert([FromBody] BulkUpsertPricingRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _bulkUpsertValidator.ValidateOrThrowAsync(request, ct);

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        await _service.BulkUpsertAsync(request, callerId, ct);

        return Ok(ResponseHelper.Ok("Pricing saved successfully.", correlationId));
    }
}
