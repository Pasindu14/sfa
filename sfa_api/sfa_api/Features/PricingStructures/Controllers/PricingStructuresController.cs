using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.PricingStructures.Requests;
using sfa_api.Features.PricingStructures.Services;

namespace sfa_api.Features.PricingStructures.Controllers;

[ApiController]
[Route("api/v1/pricing-structures")]
[Authorize]
public class PricingStructuresController(
    IPricingStructureService service,
    IValidator<CreatePricingStructureRequest> createValidator,
    IValidator<UpdatePricingStructureRequest> updateValidator,
    IValidator<DuplicatePricingStructureRequest> duplicateValidator,
    IValidator<SetDefaultPricingStructureRequest> setDefaultValidator,
    IValidator<BulkUpsertPricingStructureItemsRequest> upsertItemsValidator) : ControllerBase
{
    private readonly IPricingStructureService _service = service;

    private string CorrelationId => HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

    private int? CallerId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>GET /api/v1/pricing-structures</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
        => Ok(ResponseHelper.Ok(await _service.GetAllAsync(page, pageSize, search, isActive, ct), CorrelationId));

    /// <summary>
    /// GET /api/v1/pricing-structures/default/prices
    /// The default structure's priced items — back-office screens that need "the" price (staff PO editors).
    /// </summary>
    [HttpGet("default/prices")]
    [Authorize(Roles = "Admin,NSM,RSM,ASM,Supervisor")]
    public async Task<IActionResult> GetDefaultPrices(CancellationToken ct)
        => Ok(ResponseHelper.Ok(await _service.GetDefaultPricesAsync(ct), CorrelationId));

    /// <summary>GET /api/v1/pricing-structures/{id}</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Ok(ResponseHelper.Ok(await _service.GetByIdAsync(id, ct), CorrelationId));

    /// <summary>POST /api/v1/pricing-structures — new structures start inactive (the first one becomes the default).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreatePricingStructureRequest request, CancellationToken ct)
    {
        await createValidator.ValidateOrThrowAsync(request, ct);
        var result = await _service.CreateAsync(request, CallerId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ResponseHelper.Created(result, CorrelationId));
    }

    /// <summary>PUT /api/v1/pricing-structures/{id}</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePricingStructureRequest request, CancellationToken ct)
    {
        await updateValidator.ValidateOrThrowAsync(request, ct);
        return Ok(ResponseHelper.Ok(await _service.UpdateAsync(id, request, CallerId, ct), CorrelationId));
    }

    /// <summary>POST /api/v1/pricing-structures/{id}/duplicate — copy with every product price; the copy is inactive.</summary>
    [HttpPost("{id:int}/duplicate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Duplicate(int id, [FromBody] DuplicatePricingStructureRequest request, CancellationToken ct)
    {
        await duplicateValidator.ValidateOrThrowAsync(request, ct);
        var result = await _service.DuplicateAsync(id, request, CallerId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ResponseHelper.Created(result, CorrelationId));
    }

    /// <summary>POST /api/v1/pricing-structures/{id}/set-default</summary>
    [HttpPost("{id:int}/set-default")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetDefault(int id, [FromBody] SetDefaultPricingStructureRequest request, CancellationToken ct)
    {
        await setDefaultValidator.ValidateOrThrowAsync(request, ct);
        return Ok(ResponseHelper.Ok(await _service.SetDefaultAsync(id, request, CallerId, ct), CorrelationId));
    }

    /// <summary>POST /api/v1/pricing-structures/{id}/activate</summary>
    [HttpPost("{id:int}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
    {
        await _service.ActivateAsync(id, CallerId, ct);
        return NoContent();
    }

    /// <summary>POST /api/v1/pricing-structures/{id}/deactivate — refused for the default.</summary>
    [HttpPost("{id:int}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await _service.DeactivateAsync(id, CallerId, ct);
        return NoContent();
    }

    /// <summary>DELETE /api/v1/pricing-structures/{id} — soft delete; refused for the default.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, CallerId, ct);
        return NoContent();
    }

    /// <summary>GET /api/v1/pricing-structures/{id}/items — every non-deleted product with its prices (null = unpriced).</summary>
    [HttpGet("{id:int}/items")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetItems(int id, CancellationToken ct)
        => Ok(ResponseHelper.Ok(await _service.GetItemsAsync(id, ct), CorrelationId));

    /// <summary>PUT /api/v1/pricing-structures/{id}/items — upsert changed rows only.</summary>
    [HttpPut("{id:int}/items")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpsertItems(int id, [FromBody] BulkUpsertPricingStructureItemsRequest request, CancellationToken ct)
    {
        await upsertItemsValidator.ValidateOrThrowAsync(request, ct);
        return Ok(ResponseHelper.Ok(await _service.UpsertItemsAsync(id, request, CallerId, ct), CorrelationId));
    }
}
