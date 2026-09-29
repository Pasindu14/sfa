using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Reports.Enums;
using sfa_api.Features.Reports.Requests;
using sfa_api.Features.Reports.Services;

namespace sfa_api.Features.Reports.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public class ReportsController(
    ISalesSummaryService salesSummaryService,
    IValidator<SalesSummaryQuery> salesSummaryValidator) : ControllerBase
{
    private readonly ISalesSummaryService _salesSummaryService = salesSummaryService;
    private readonly IValidator<SalesSummaryQuery> _salesSummaryValidator = salesSummaryValidator;

    /// <summary>
    /// GET /api/v1/reports/sales-summary?from=YYYY-MM-DD&amp;to=YYYY-MM-DD&amp;groupBy=SalesRep[&amp;groupBy=Territory…]
    /// Targets vs gross sales, returns, discounts and net sales over a date range, grouped by one or
    /// more dimensions (repeat groupBy, in order; default SalesRep). All quantities are in packs.
    /// Optional id filters narrow the population; every one of them is AND-ed.
    /// </summary>
    [HttpGet("sales-summary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSalesSummary(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] SalesSummaryGroupBy[]? groupBy = null,
        [FromQuery] int? regionId = null,
        [FromQuery] int? areaId = null,
        [FromQuery] int? territoryId = null,
        [FromQuery] int? divisionId = null,
        [FromQuery] int? routeId = null,
        [FromQuery] int? distributorId = null,
        [FromQuery] int? salesRepId = null,
        [FromQuery] int? supervisorId = null,
        [FromQuery] int? asmId = null,
        [FromQuery] int? rsmId = null,
        [FromQuery] int? nsmId = null,
        [FromQuery] int? productId = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        var dims = groupBy is { Length: > 0 } ? groupBy : [SalesSummaryGroupBy.SalesRep];

        var query = new SalesSummaryQuery(
            dims[0], from, to,
            regionId, areaId, territoryId, divisionId, routeId,
            distributorId, salesRepId, supervisorId, asmId, rsmId, nsmId, productId,
            ThenBy: dims.Length > 1 ? dims[1..] : null);

        await _salesSummaryValidator.ValidateOrThrowAsync(query, ct);

        var result = await _salesSummaryService.GetSalesSummaryAsync(query, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
