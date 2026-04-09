using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Billings.Requests;
using sfa_api.Features.Billings.Services;

namespace sfa_api.Features.Billings.Controllers;

[ApiController]
[Route("api/v1/billings")]
[Authorize]
public class BillingsController(
    IBillingService billingService,
    IValidator<CreateBillingRequest> createValidator) : ControllerBase
{
    private readonly IBillingService _billingService = billingService;
    private readonly IValidator<CreateBillingRequest> _createValidator = createValidator;

    private int GetCallerId()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        return callerId;
    }

    /// <summary>
    /// GET /api/v1/billings
    /// Returns a paginated list of billings with optional filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? billingType = null,
        [FromQuery] string? status = null,
        [FromQuery] int? outletId = null,
        [FromQuery] int? distributorId = null,
        [FromQuery] int? salesRepId = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        BillingType? parsedType = Enum.TryParse<BillingType>(billingType, true, out var bt) ? bt : null;
        BillingStatus? parsedStatus = Enum.TryParse<BillingStatus>(status, true, out var bs) ? bs : null;
        DateOnly? parsedDateFrom = DateOnly.TryParse(dateFrom, out var df) ? df : null;
        DateOnly? parsedDateTo   = DateOnly.TryParse(dateTo, out var dt) ? dt : null;

        var (items, total) = await _billingService.GetListAsync(
            page, pageSize, parsedType, parsedStatus,
            outletId, distributorId, salesRepId,
            parsedDateFrom, parsedDateTo, ct);

        return Ok(ResponseHelper.Paged(items, page, pageSize, total, correlationId));
    }

    /// <summary>
    /// GET /api/v1/billings/{id}
    /// Returns a billing with its line items.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var billing = await _billingService.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Billing", id);
        return Ok(ResponseHelper.Ok(billing, correlationId));
    }

    /// <summary>
    /// POST /api/v1/billings
    /// SalesRep only — creates a billing for an outlet and deducts stock atomically.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SalesRep")]
    public async Task<IActionResult> Create([FromBody] CreateBillingRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _createValidator.ValidateOrThrowAsync(request, ct);

        var billing = await _billingService.CreateAsync(request, GetCallerId(), ct);
        return CreatedAtAction(nameof(GetById), new { id = billing.Id }, ResponseHelper.Ok(billing, correlationId));
    }
}
