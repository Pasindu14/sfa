using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Stock.Requests;
using sfa_api.Features.Stock.Services;

namespace sfa_api.Features.Stock.Controllers;

/// <summary>
/// Admin-only correction of a distributor's stock balance (active or closed distributor).
/// POST is covered by IdempotencyMiddleware when the client sends X-Idempotency-Key.
/// </summary>
[ApiController]
[Route("api/v1/stock-adjustments")]
[Authorize(Roles = "Admin")]
public class StockAdjustmentsController(
    IStockAdjustmentService service,
    IValidator<CreateStockAdjustmentRequest> createValidator) : ControllerBase
{
    private readonly IStockAdjustmentService _service = service;
    private readonly IValidator<CreateStockAdjustmentRequest> _createValidator = createValidator;

    private int GetCallerId()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        return callerId;
    }

    /// <summary>
    /// GET /api/v1/stock-adjustments?page=1&amp;pageSize=20&amp;distributorId=
    /// Paged history, newest first.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? distributorId = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        (page, pageSize) = PaginationHelper.Clamp(page, pageSize);
        var (items, total) = await _service.GetPagedAsync(page, pageSize, distributorId, ct);
        return Ok(ResponseHelper.Paged(items, page, pageSize, total, correlationId));
    }

    /// <summary>GET /api/v1/stock-adjustments/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var adjustment = await _service.GetByIdAsync(id, ct);
        return Ok(ResponseHelper.Ok(adjustment, correlationId));
    }

    /// <summary>
    /// POST /api/v1/stock-adjustments
    /// Sets each line's balance to NewQuantity (pieces), posting the signed difference to the ledger.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStockAdjustmentRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _createValidator.ValidateOrThrowAsync(request, ct);

        var adjustment = await _service.CreateAsync(request, GetCallerId(), ct);
        return CreatedAtAction(nameof(GetById), new { id = adjustment.Id }, ResponseHelper.Created(adjustment, correlationId));
    }
}
