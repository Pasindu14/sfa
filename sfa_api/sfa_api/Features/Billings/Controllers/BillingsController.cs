using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Billings.Requests;
using sfa_api.Features.Billings.Services;
using sfa_api.Features.Users.Repositories;

namespace sfa_api.Features.Billings.Controllers;

[ApiController]
[Route("api/v1/billings")]
[Authorize]
public class BillingsController(
    IBillingService billingService,
    IValidator<CreateBillingRequest> createValidator,
    IUserRepository userRepo) : ControllerBase
{
    private readonly IBillingService _billingService = billingService;
    private readonly IValidator<CreateBillingRequest> _createValidator = createValidator;
    private readonly IUserRepository _userRepo = userRepo;

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
        [FromQuery] string? repStatus = null,
        [FromQuery] string? distributorStatus = null,
        [FromQuery] int? outletId = null,
        [FromQuery] int? distributorId = null,
        [FromQuery] int? salesRepId = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? paymentType = null,
        [FromQuery] bool? isCashCollected = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        RepBillingStatus? parsedRepStatus                 = Enum.TryParse<RepBillingStatus>(repStatus, true, out var rs) ? rs : null;
        DistributorBillingStatus? parsedDistributorStatus = Enum.TryParse<DistributorBillingStatus>(distributorStatus, true, out var ds) ? ds : null;
        DateOnly? parsedDateFrom                          = DateOnly.TryParse(dateFrom, out var df) ? df : null;
        DateOnly? parsedDateTo                            = DateOnly.TryParse(dateTo, out var dt) ? dt : null;
        PaymentType? parsedPaymentType                    = Enum.TryParse<PaymentType>(paymentType, true, out var pt) ? pt : null;

        var (items, total) = await _billingService.GetListAsync(
            page, pageSize, parsedRepStatus, parsedDistributorStatus,
            outletId, distributorId, salesRepId,
            parsedDateFrom, parsedDateTo, parsedPaymentType, isCashCollected, ct);

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
    /// GET /api/v1/billings/outlet-summary
    /// Returns billings grouped and aggregated by outlet for the caller's route and date range.
    /// SalesRep is resolved from the JWT — no salesRepId param needed from mobile.
    /// </summary>
    [HttpGet("outlet-summary")]
    public async Task<IActionResult> GetOutletSummary(
        [FromQuery] int? routeId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        if (!routeId.HasValue)
            return BadRequest(new { code = "VALIDATION_ERROR", message = "routeId is required." });

        if (!DateOnly.TryParse(dateFrom, out var parsedFrom) || !DateOnly.TryParse(dateTo, out var parsedTo))
            return BadRequest(new { code = "VALIDATION_ERROR", message = "dateFrom and dateTo must be valid dates (YYYY-MM-DD)." });

        var result = await _billingService.GetOutletSummaryAsync(
            GetCallerId(), routeId.Value, parsedFrom, parsedTo, ct);

        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// PATCH /api/v1/billings/{id}/cancel
    /// SalesRep only — cancels a submitted billing they created.
    /// Only Submitted billings can be cancelled; Approved/already-Cancelled are rejected.
    /// </summary>
    [HttpPatch("{id:int}/cancel")]
    [Authorize(Roles = "SalesRep")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var billing = await _billingService.CancelAsync(id, GetCallerId(), ct);
        return Ok(ResponseHelper.Ok(billing, correlationId));
    }

    /// <summary>
    /// GET /api/v1/billings/portal
    /// Distributor only — returns paginated billings for the logged-in distributor.
    /// DistributorId is resolved from JWT, not accepted from the client.
    /// </summary>
    [HttpGet("portal")]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> GetPortalBillings(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? repStatus = null,
        [FromQuery] string? distributorStatus = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? paymentType = null,
        [FromQuery] bool? isCashCollected = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var user = await _userRepo.GetUserByIdAsync(userId, ct);
        if (user?.DistributorId == null)
            throw new BusinessRuleException("NO_DISTRIBUTOR_LINKED",
                "Your account is not linked to a distributor.");
        RepBillingStatus? parsedRepStatus                 = Enum.TryParse<RepBillingStatus>(repStatus, true, out var rs) ? rs : null;
        DistributorBillingStatus? parsedDistributorStatus = Enum.TryParse<DistributorBillingStatus>(distributorStatus, true, out var ds) ? ds : null;
        DateOnly? parsedFrom                              = DateOnly.TryParse(dateFrom, out var df) ? df : null;
        DateOnly? parsedTo                                = DateOnly.TryParse(dateTo, out var dt) ? dt : null;
        PaymentType? parsedPaymentType                    = Enum.TryParse<PaymentType>(paymentType, true, out var pt) ? pt : null;
        var (items, total) = await _billingService.GetListAsync(
            page, pageSize, parsedRepStatus, parsedDistributorStatus,
            null, user.DistributorId.Value, null, parsedFrom, parsedTo,
            parsedPaymentType, isCashCollected, ct);
        return Ok(ResponseHelper.Paged(items, page, pageSize, total, correlationId));
    }

    /// <summary>
    /// GET /api/v1/billings/portal/{id}
    /// Distributor only — returns full billing detail, enforcing ownership.
    /// </summary>
    [HttpGet("portal/{id:int}")]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> GetPortalBillingDetail(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var user = await _userRepo.GetUserByIdAsync(userId, ct);
        if (user?.DistributorId == null)
            throw new BusinessRuleException("NO_DISTRIBUTOR_LINKED",
                "Your account is not linked to a distributor.");
        var detail = await _billingService.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Billing", id);
        if (detail.DistributorId != user.DistributorId.Value)
            throw new BusinessRuleException("BILLING_NOT_FOUND", "Billing not found.");
        return Ok(ResponseHelper.Ok(detail, correlationId));
    }

    /// <summary>
    /// PATCH /api/v1/billings/{id}/approve
    /// Distributor only — approves a submitted billing.
    /// </summary>
    [HttpPatch("{id:int}/approve")]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var billing = await _billingService.ApproveAsync(id, GetCallerId(), ct);
        return Ok(ResponseHelper.Ok(billing, correlationId));
    }

    /// <summary>
    /// PATCH /api/v1/billings/{id}/reject
    /// Distributor only — rejects a submitted billing with an optional reason.
    /// </summary>
    [HttpPatch("{id:int}/reject")]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectBillingRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var billing = await _billingService.RejectAsync(id, GetCallerId(), request.Reason, ct);
        return Ok(ResponseHelper.Ok(billing, correlationId));
    }

    /// <summary>
    /// PATCH /api/v1/billings/{id}/payment-type
    /// Distributor only — updates the payment type (Cash / Credit) for a billing.
    /// </summary>
    [HttpPatch("{id:int}/payment-type")]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> UpdatePaymentType(int id, [FromBody] UpdatePaymentTypeRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var billing = await _billingService.UpdatePaymentTypeAsync(id, GetCallerId(), request.PaymentType, ct);
        return Ok(ResponseHelper.Ok(billing, correlationId));
    }

    /// <summary>
    /// PATCH /api/v1/billings/{id}/cash-collected
    /// Distributor only — marks whether cash has been collected for this billing.
    /// </summary>
    [HttpPatch("{id:int}/cash-collected")]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> UpdateCashCollected(int id, [FromBody] UpdateCashCollectedRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var billing = await _billingService.UpdateCashCollectedAsync(id, GetCallerId(), request.IsCashCollected, ct);
        return Ok(ResponseHelper.Ok(billing, correlationId));
    }

    /// <summary>
    /// POST /api/v1/billings
    /// SalesRep only — creates a billing for an outlet and deducts stock atomically.
    ///
    /// <para><b>Offline-sync contract (mobile):</b></para>
    /// <list type="bullet">
    ///   <item>Send the header <c>X-Idempotency-Key: {UUID}</c> — the client-generated bill id.
    ///   The same key may be retried any number of times; the server caches the response and
    ///   returns it verbatim, so a flaky connection can't create duplicate bills.</item>
    ///   <item>Send <c>billingDate</c> in the body to preserve the date the rep wrote the bill
    ///   offline (otherwise the server stamps the sync time). Must be within the last 7 days.</item>
    /// </list>
    ///
    /// <para><b>Stock-out error shape (HTTP 422, code <c>INSUFFICIENT_STOCK</c>):</b>
    /// <c>ApiError.Fields</c> contains one entry per missing product, keyed <c>product:{id}</c>,
    /// with a human-readable message including the product name, requested, and available.</para>
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
