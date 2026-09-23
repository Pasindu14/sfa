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
/// Admin-only transfer of a closed distributor's remaining stock into an active distributor.
/// POST is covered by IdempotencyMiddleware when the client sends X-Idempotency-Key.
/// </summary>
[ApiController]
[Route("api/v1/stock-transfers")]
[Authorize(Roles = "Admin")]
public class StockTransfersController(
    IStockTransferService service,
    IValidator<CreateStockTransferRequest> createValidator) : ControllerBase
{
    private readonly IStockTransferService _service = service;
    private readonly IValidator<CreateStockTransferRequest> _createValidator = createValidator;

    private int GetCallerId()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        return callerId;
    }

    /// <summary>
    /// GET /api/v1/stock-transfers?page=1&amp;pageSize=20&amp;distributorId=
    /// Paged history, newest first. distributorId matches the source OR the target.
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

    /// <summary>GET /api/v1/stock-transfers/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var transfer = await _service.GetByIdAsync(id, ct);
        return Ok(ResponseHelper.Ok(transfer, correlationId));
    }

    /// <summary>
    /// POST /api/v1/stock-transfers
    /// Moves the given quantities (pieces) from an inactive source to an active target distributor.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStockTransferRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _createValidator.ValidateOrThrowAsync(request, ct);

        var transfer = await _service.CreateAsync(request, GetCallerId(), ct);
        return CreatedAtAction(nameof(GetById), new { id = transfer.Id }, ResponseHelper.Created(transfer, correlationId));
    }
}
