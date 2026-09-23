using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Stock.Requests;
using sfa_api.Features.Stock.Services;

namespace sfa_api.Features.Stock.Controllers;

/// <summary>Admin-only, read-only audit log over the stock ledger across all distributors.</summary>
[ApiController]
[Route("api/v1/stock/activity")]
[Authorize(Roles = "Admin")]
public class StockActivityController(
    IStockActivityService service,
    IValidator<StockActivityQuery> queryValidator) : ControllerBase
{
    private readonly IStockActivityService _service = service;
    private readonly IValidator<StockActivityQuery> _queryValidator = queryValidator;

    /// <summary>
    /// GET /api/v1/stock/activity?from=YYYY-MM-DD&amp;to=YYYY-MM-DD&amp;distributorId=&amp;productId=&amp;userId=&amp;transactionType=&amp;direction=&amp;page=1&amp;pageSize=50
    /// Ledger rows newest first. from/to are inclusive business dates (max 93 days apart).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetActivity(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? distributorId = null,
        [FromQuery] int? productId = null,
        [FromQuery] int? userId = null,
        [FromQuery] string? transactionType = null,
        [FromQuery] string? direction = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var query = new StockActivityQuery(from, to, distributorId, productId, userId, transactionType, direction, page, pageSize);
        await _queryValidator.ValidateOrThrowAsync(query, ct);

        var (items, total) = await _service.GetActivityAsync(query, ct);
        return Ok(ResponseHelper.Paged(items, page, pageSize, total, correlationId));
    }

    /// <summary>
    /// GET /api/v1/stock/activity/users
    /// Users who appear on at least one ledger row — options for the log's user filter.
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var users = await _service.GetUsersAsync(ct);
        return Ok(ResponseHelper.Ok(users, correlationId));
    }
}
