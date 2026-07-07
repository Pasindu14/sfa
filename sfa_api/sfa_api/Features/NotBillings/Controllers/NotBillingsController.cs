using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.NotBillings.Enums;
using sfa_api.Features.NotBillings.Requests;
using sfa_api.Features.NotBillings.Services;

namespace sfa_api.Features.NotBillings.Controllers;

[ApiController]
[Route("api/v1/not-billings")]
[Authorize]
public class NotBillingsController(
    INotBillingService notBillingService,
    IValidator<CreateNotBillingRequest> createValidator) : ControllerBase
{
    private readonly INotBillingService _notBillingService = notBillingService;
    private readonly IValidator<CreateNotBillingRequest> _createValidator = createValidator;

    private int GetCallerId()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        return callerId;
    }

    /// <summary>
    /// GET /api/v1/not-billings
    /// Returns a paginated list of not-billing records with optional filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? outletId = null,
        [FromQuery] int? salesRepId = null,
        [FromQuery] string? reason = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        // A SalesRep may only see their own records; the client-supplied salesRepId is ignored for reps.
        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (string.Equals(callerRole, "SalesRep", StringComparison.OrdinalIgnoreCase))
            salesRepId = GetCallerId();

        NotBillingReason? parsedReason = Enum.TryParse<NotBillingReason>(reason, true, out var r) ? r : null;
        DateOnly? parsedDateFrom = DateOnly.TryParse(dateFrom, out var df) ? df : null;
        DateOnly? parsedDateTo   = DateOnly.TryParse(dateTo, out var dt) ? dt : null;

        var (items, total) = await _notBillingService.GetListAsync(
            page, pageSize,
            outletId, salesRepId, parsedReason,
            parsedDateFrom, parsedDateTo, ct);

        return Ok(ResponseHelper.Paged(items, page, pageSize, total, correlationId));
    }

    /// <summary>
    /// GET /api/v1/not-billings/{id}
    /// Returns a single not-billing record with full org and geo chain.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var notBilling = await _notBillingService.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("NotBilling", id);

        // A SalesRep may only read their own records.
        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (string.Equals(callerRole, "SalesRep", StringComparison.OrdinalIgnoreCase)
            && notBilling.SalesRepId != GetCallerId())
            throw new AuthorizationException("this not-billing record");

        return Ok(ResponseHelper.Ok(notBilling, correlationId));
    }

    /// <summary>
    /// POST /api/v1/not-billings
    /// SalesRep only — records a non-sale outlet visit with a reason.
    ///
    /// <para><b>Offline-sync contract (mobile):</b></para>
    /// <list type="bullet">
    ///   <item><b>Required.</b> Send the header <c>X-Idempotency-Key: {UUID}</c> — the client-generated
    ///   record id. It is mandatory: a request without it is rejected with <c>400 VALIDATION_FAILED</c>.
    ///   The same key may be retried any number of times without creating duplicate records — the
    ///   server returns the original record even across a day boundary or after the cache TTL.</item>
    ///   <item>Send <c>notBillingDate</c> in the body to preserve the date the rep logged the visit
    ///   offline. Must be within the last 7 days.</item>
    /// </list>
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SalesRep")]
    public async Task<IActionResult> Create([FromBody] CreateNotBillingRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _createValidator.ValidateOrThrowAsync(request, ct);

        // Mandatory client-generated id — the durable duplicate guard (fast-path + filtered unique
        // index) keys on it, so a missing key would let an offline replay duplicate a compliance
        // record. Reject up front (finding #6), matching the billings contract.
        var clientRecordId = Request.Headers["X-Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(clientRecordId))
            throw new sfa_api.Common.Errors.ValidationException(new Dictionary<string, string[]>
            {
                ["X-Idempotency-Key"] = new[]
                {
                    "The X-Idempotency-Key header is required. Send your client-generated record id " +
                    "(a UUID) so retries and offline replays cannot create duplicate records."
                }
            });

        var notBilling = await _notBillingService.CreateAsync(request, GetCallerId(), clientRecordId, ct);
        return CreatedAtAction(nameof(GetById), new { id = notBilling.Id }, ResponseHelper.Ok(notBilling, correlationId));
    }
}
