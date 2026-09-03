using sfa_api.Features.Reports.Enums;

namespace sfa_api.Features.Reports.Requests;

/// <summary>
/// Query parameters for the sales-summary report.
/// <para>
/// <paramref name="From"/>/<paramref name="To"/> are inclusive business dates (Asia/Colombo) matched
/// against <c>Billing.BillingDate</c>, which is a <c>DateOnly</c> column — no timezone conversion is
/// needed or wanted here. Every id filter is optional and combinable; all of them are AND-ed.
/// </para>
/// </summary>
public record SalesSummaryQuery(
    SalesSummaryGroupBy GroupBy,
    DateOnly From,
    DateOnly To,
    int? RegionId      = null,
    int? AreaId        = null,
    int? TerritoryId   = null,
    int? DivisionId    = null,
    int? RouteId       = null,
    int? DistributorId = null,
    int? SalesRepId    = null,
    int? SupervisorId  = null,
    // The three org levels above Supervisor. Unlike RouteId these do NOT suppress targets:
    // SalesTarget carries AsmUserId/RsmUserId/NsmUserId too, so targets stay attributable.
    int? AsmId         = null,
    int? RsmId         = null,
    int? NsmId         = null,
    int? ProductId     = null);
