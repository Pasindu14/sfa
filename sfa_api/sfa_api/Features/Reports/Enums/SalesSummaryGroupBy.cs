namespace sfa_api.Features.Reports.Enums;

/// <summary>
/// The dimension one sales-summary row is grouped by, chosen per request.
/// <para>
/// Every value except <see cref="Route"/> and <see cref="Outlet"/> also exists as a column on
/// <c>SalesTarget</c>. Those two do not, so target columns come back <c>null</c> for them —
/// see <c>SalesSummaryRepository.TargetKey</c>, which is the single source of truth for
/// "are targets available for this dimension".
/// </para>
/// </summary>
public enum SalesSummaryGroupBy
{
    SalesRep    = 0,
    Supervisor  = 1,
    Asm         = 2,
    Rsm         = 3,
    Nsm         = 4,
    Distributor = 5,
    Outlet      = 6,
    Route       = 7,
    Division    = 8,
    Territory   = 9,
    Area        = 10,
    Region      = 11,
    Product     = 12,
}
