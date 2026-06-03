using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using sfa_api.Common.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Features.Products.Repositories;
using sfa_api.Features.StockTaking.Requests;
using sfa_api.Features.StockTaking.Services;
using sfa_api.Features.Users.Repositories;

namespace sfa_api.Features.StockTaking.Controllers;

[ApiController]
[Route("api/v1/stock-taking")]
[Authorize]
public class StockTakingController(
    IStockTakingService service,
    IUserRepository userRepo,
    IProductRepository productRepository,
    IValidator<CreatePeriodRequest> createPeriodValidator,
    IValidator<UpsertSubmissionRequest> upsertSubmissionValidator,
    IValidator<AdjustLineRequest> adjustLineValidator) : ControllerBase
{
    private readonly IStockTakingService _service                   = service;
    private readonly IUserRepository     _userRepo                  = userRepo;
    private readonly IProductRepository  _productRepository         = productRepository;
    private readonly IValidator<CreatePeriodRequest>       _createPeriodValidator      = createPeriodValidator;
    private readonly IValidator<UpsertSubmissionRequest>   _upsertSubmissionValidator  = upsertSubmissionValidator;
    private readonly IValidator<AdjustLineRequest>         _adjustLineValidator        = adjustLineValidator;

    // ── Admin endpoints ───────────────────────────────────────────────────

    /// <summary>GET /api/v1/stock-taking/periods — paged list of all periods (Admin)</summary>
    [HttpGet("periods")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPeriods(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (items, total) = await _service.GetPeriodsAsync(page, pageSize, search, ct);
        return Ok(ResponseHelper.Paged(items, page, pageSize, total, correlationId));
    }

    /// <summary>GET /api/v1/stock-taking/periods/{id} — period detail (Admin)</summary>
    [HttpGet("periods/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPeriodById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetPeriodByIdAsync(id, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>POST /api/v1/stock-taking/periods — create a new period (Admin)</summary>
    [HttpPost("periods")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePeriod(
        [FromBody] CreatePeriodRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        await _createPeriodValidator.ValidateOrThrowAsync(request, ct);
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.CreatePeriodAsync(request, callerId, ct);
        return CreatedAtAction(nameof(GetPeriodById), new { id = result.Id },
            ResponseHelper.Created(result, correlationId));
    }

    /// <summary>POST /api/v1/stock-taking/periods/{id}/lock — lock period (Admin)</summary>
    [HttpPost("periods/{id:int}/lock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> LockPeriod(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.LockPeriodAsync(id, callerId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>POST /api/v1/stock-taking/periods/{id}/unlock — unlock period (Admin)</summary>
    [HttpPost("periods/{id:int}/unlock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UnlockPeriod(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.UnlockPeriodAsync(id, callerId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/stock-taking/periods/{id}/submissions?distributorId=X
    /// Returns a distributor's submission + lines for admin review (Admin)
    /// </summary>
    [HttpGet("periods/{id:int}/submissions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSubmissionForAdmin(
        int id, [FromQuery] int distributorId, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetSubmissionForAdminAsync(id, distributorId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>POST /api/v1/stock-taking/lines/{lineId}/adjust — adjust a line (Admin)</summary>
    [HttpPost("lines/{lineId:int}/adjust")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdjustLine(
        int lineId, [FromBody] AdjustLineRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        await _adjustLineValidator.ValidateOrThrowAsync(request, ct);
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.AdjustLineAsync(lineId, request, callerId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    // ── Distributor portal endpoints ──────────────────────────────────────

    /// <summary>GET /api/v1/stock-taking/portal/periods — open periods (Distributor)</summary>
    [HttpGet("portal/periods")]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> GetOpenPeriods(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetOpenPeriodsAsync(ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/stock-taking/portal/submissions/{periodId}
    /// Returns the calling distributor's draft/submission for the given period (Distributor)
    /// </summary>
    [HttpGet("portal/submissions/{periodId:int}")]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> GetMySubmission(int periodId, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var distributorId = await ResolveDistributorIdAsync(ct);
        var result = await _service.GetMySubmissionAsync(periodId, distributorId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/stock-taking/portal/submissions
    /// Upsert draft lines (Distributor) — never accepts distributorId from body
    /// </summary>
    [HttpPost("portal/submissions")]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> UpsertDraft(
        [FromBody] UpsertSubmissionRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        await _upsertSubmissionValidator.ValidateOrThrowAsync(request, ct);
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var distributorId = await ResolveDistributorIdAsync(ct);
        var result = await _service.UpsertDraftAsync(request, distributorId, userId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/stock-taking/portal/submissions/{periodId}/submit
    /// Finalise and snapshot system qty (Distributor)
    /// </summary>
    [HttpPost("portal/submissions/{periodId:int}/submit")]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> Submit(int periodId, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var distributorId = await ResolveDistributorIdAsync(ct);
        var result = await _service.SubmitAsync(periodId, distributorId, userId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/stock-taking/portal/products?search=X
    /// Returns active products for the distributor product picker (Distributor)
    /// </summary>
    [HttpGet("portal/products")]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> SearchPortalProducts(
        [FromQuery] string? search = null,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (products, _) = await _productRepository.GetAllAsync(0, pageSize, search, ct);
        var dtos = products.Select(p => new
        {
            p.Id,
            p.Code,
            p.ItemDescription,
        }).ToList();
        return Ok(ResponseHelper.Ok(dtos, correlationId));
    }

    // ── Private ───────────────────────────────────────────────────────────

    private async Task<int> ResolveDistributorIdAsync(CancellationToken ct)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var user = await _userRepo.GetUserByIdAsync(userId, ct);
        if (user?.DistributorId == null)
            throw new BusinessRuleException("NO_DISTRIBUTOR_LINKED",
                "Your account is not linked to a distributor.");
        return user.DistributorId.Value;
    }
}
