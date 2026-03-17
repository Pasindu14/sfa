using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.PricingStructures.Requests;
using sfa_api.Features.PricingStructures.Services;

namespace sfa_api.Features.PricingStructures.Controllers;

[ApiController]
[Route("api/v1/pricing-structures")]
public class PricingStructuresController(
    IPricingStructureService pricingStructureService,
    IValidator<CreatePricingStructureRequest> createValidator,
    IValidator<UpdatePricingStructureRequest> updateValidator,
    IValidator<BulkUpdateItemsRequest> bulkUpdateItemsValidator) : ControllerBase
{
    private readonly IPricingStructureService _pricingStructureService = pricingStructureService;
    private readonly IValidator<CreatePricingStructureRequest> _createValidator = createValidator;
    private readonly IValidator<UpdatePricingStructureRequest> _updateValidator = updateValidator;
    private readonly IValidator<BulkUpdateItemsRequest> _bulkUpdateItemsValidator = bulkUpdateItemsValidator;

    /// <summary>
    /// GET /api/v1/pricing-structures
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllPricingStructures(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _pricingStructureService.GetAllAsync(page, pageSize, search, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/pricing-structures/default
    /// All authenticated roles — used by Create Order product selector
    /// </summary>
    [HttpGet("default")]
    [Authorize]
    public async Task<IActionResult> GetDefaultPricingStructure(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _pricingStructureService.GetDefaultAsync(ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/pricing-structures/{id}
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPricingStructureById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _pricingStructureService.GetByIdAsync(id, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/pricing-structures
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePricingStructure([FromBody] CreatePricingStructureRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var fields = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Common.Errors.ValidationException(fields);
        }

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _pricingStructureService.CreateAsync(request, callerId, ct);
        return CreatedAtAction(nameof(GetPricingStructureById), new { id = result.Id },
            ResponseHelper.Created(result, correlationId));
    }

    /// <summary>
    /// PUT /api/v1/pricing-structures/{id}
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePricingStructure(int id, [FromBody] UpdatePricingStructureRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var fields = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Common.Errors.ValidationException(fields);
        }

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _pricingStructureService.UpdateAsync(id, request, callerId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// DELETE /api/v1/pricing-structures/{id}
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePricingStructure(int id, CancellationToken ct)
    {
        await _pricingStructureService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// POST /api/v1/pricing-structures/{id}/activate
    /// </summary>
    [HttpPost("{id}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActivatePricingStructure(int id, CancellationToken ct)
    {
        await _pricingStructureService.ActivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// GET /api/v1/pricing-structures/{id}/items
    /// </summary>
    [HttpGet("{id}/items")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetItems(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _pricingStructureService.GetItemsAsync(id, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// PUT /api/v1/pricing-structures/{id}/items
    /// </summary>
    [HttpPut("{id}/items")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkUpdateItems(int id, [FromBody] BulkUpdateItemsRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        var validation = await _bulkUpdateItemsValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var fields = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Common.Errors.ValidationException(fields);
        }

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _pricingStructureService.BulkReplaceItemsAsync(id, request, callerId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
