using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.GRNs.Requests;
using sfa_api.Features.GRNs.Services;

namespace sfa_api.Features.GRNs.Controllers;

[ApiController]
[Route("api/v1/grns")]
[Authorize]
public class GrnsController(
    IGrnService grnService,
    IValidator<CreateGrnRequest> createValidator,
    IValidator<ConfirmGrnRequest> confirmValidator) : ControllerBase
{
    private readonly IGrnService _grnService = grnService;
    private readonly IValidator<CreateGrnRequest> _createValidator = createValidator;
    private readonly IValidator<ConfirmGrnRequest> _confirmValidator = confirmValidator;

    private int GetCallerId()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        return callerId;
    }

    /// <summary>
    /// GET /api/v1/grns
    /// Returns a paginated list of GRNs, optionally filtered by status or distributor.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] int? distributorId = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (items, total) = await _grnService.GetListAsync(page, pageSize, status, distributorId, ct);
        return Ok(ResponseHelper.Paged(items, page, pageSize, total, correlationId));
    }

    /// <summary>
    /// GET /api/v1/grns/{id}
    /// Returns a GRN with its items.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var grn = await _grnService.GetByIdAsync(id, ct);
        return Ok(ResponseHelper.Ok(grn, correlationId));
    }

    /// <summary>
    /// POST /api/v1/grns
    /// Admin only — creates a GRN for a Pending sales invoice and copies its items.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateGrnRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _createValidator.ValidateOrThrowAsync(request, ct);

        var grn = await _grnService.CreateAsync(request, GetCallerId(), ct);
        return CreatedAtAction(nameof(GetById), new { id = grn.Id }, ResponseHelper.Ok(grn, correlationId));
    }

    /// <summary>
    /// PATCH /api/v1/grns/{id}/confirm
    /// Admin only — confirms a Pending GRN, updates stock, and appends ledger entries.
    /// Uses distributed advisory lock + SELECT FOR UPDATE to handle concurrent confirms safely.
    /// </summary>
    [HttpPatch("{id:int}/confirm")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Confirm(int id, [FromBody] ConfirmGrnRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _confirmValidator.ValidateOrThrowAsync(request, ct);

        var grn = await _grnService.ConfirmAsync(id, request, GetCallerId(), ct);
        return Ok(ResponseHelper.Ok(grn, correlationId));
    }
}
