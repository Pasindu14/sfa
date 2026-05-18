using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.GRNs.Requests;
using sfa_api.Features.GRNs.Services;
using sfa_api.Features.Users.Repositories;

namespace sfa_api.Features.GRNs.Controllers;

[ApiController]
[Route("api/v1/grns")]
[Authorize]
public class GrnsController(
    IGrnService grnService,
    IValidator<CreateGrnRequest> createValidator,
    IValidator<ConfirmGrnRequest> confirmValidator,
    IUserRepository userRepo) : ControllerBase
{
    private readonly IGrnService _grnService = grnService;
    private readonly IValidator<CreateGrnRequest> _createValidator = createValidator;
    private readonly IValidator<ConfirmGrnRequest> _confirmValidator = confirmValidator;
    private readonly IUserRepository _userRepo = userRepo;

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
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        DateOnly? parsedDateFrom = DateOnly.TryParse(dateFrom, out var df) ? df : null;
        DateOnly? parsedDateTo = DateOnly.TryParse(dateTo, out var dt) ? dt : null;
        var (items, total) = await _grnService.GetListAsync(page, pageSize, status, distributorId, parsedDateFrom, parsedDateTo, search, ct);
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
    /// Admin or Distributor — confirms a Pending GRN, updates stock, and appends ledger entries.
    /// Distributors may only confirm GRNs belonging to their own account (ownership enforced here).
    /// Uses distributed advisory lock + SELECT FOR UPDATE to handle concurrent confirms safely.
    /// </summary>
    [HttpPatch("{id:int}/confirm")]
    [Authorize(Roles = "Admin,Distributor")]
    public async Task<IActionResult> Confirm(int id, [FromBody] ConfirmGrnRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _confirmValidator.ValidateOrThrowAsync(request, ct);

        if (User.IsInRole("Distributor"))
        {
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
            var user = await _userRepo.GetUserByIdAsync(userId, ct);
            if (user?.DistributorId == null)
                throw new BusinessRuleException("NO_DISTRIBUTOR_LINKED",
                    "Your account is not linked to a distributor.");
            var existing = await _grnService.GetByIdAsync(id, ct);
            if (existing.DistributorId != user.DistributorId.Value)
                throw new BusinessRuleException("GRN_NOT_FOUND", "GRN not found.");
        }

        var grn = await _grnService.ConfirmAsync(id, request, GetCallerId(), ct);
        return Ok(ResponseHelper.Ok(grn, correlationId));
    }

    /// <summary>
    /// GET /api/v1/grns/portal
    /// Distributor only — returns paginated GRNs for the logged-in distributor.
    /// DistributorId is resolved from the JWT sub claim + database; never accepted from the client.
    /// </summary>
    [HttpGet("portal")]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> GetPortalGrns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var user = await _userRepo.GetUserByIdAsync(userId, ct);
        if (user?.DistributorId == null)
            throw new BusinessRuleException("NO_DISTRIBUTOR_LINKED",
                "Your account is not linked to a distributor.");
        DateOnly? parsedDateFrom = DateOnly.TryParse(dateFrom, out var df) ? df : null;
        DateOnly? parsedDateTo   = DateOnly.TryParse(dateTo, out var dt) ? dt : null;
        var (items, total) = await _grnService.GetListAsync(
            page, pageSize, status, user.DistributorId.Value, parsedDateFrom, parsedDateTo, search, ct);
        return Ok(ResponseHelper.Paged(items, page, pageSize, total, correlationId));
    }

    /// <summary>
    /// GET /api/v1/grns/portal/{id}
    /// Distributor only — returns full GRN detail with stock items, enforcing ownership.
    /// </summary>
    [HttpGet("portal/{id:int}")]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> GetPortalGrnDetail(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var user = await _userRepo.GetUserByIdAsync(userId, ct);
        if (user?.DistributorId == null)
            throw new BusinessRuleException("NO_DISTRIBUTOR_LINKED",
                "Your account is not linked to a distributor.");
        var grn = await _grnService.GetByIdAsync(id, ct);
        if (grn.DistributorId != user.DistributorId.Value)
            throw new BusinessRuleException("GRN_NOT_FOUND", "GRN not found.");
        return Ok(ResponseHelper.Ok(grn, correlationId));
    }
}
