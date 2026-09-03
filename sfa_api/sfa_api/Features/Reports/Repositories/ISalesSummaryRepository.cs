using sfa_api.Features.Reports.DTOs;
using sfa_api.Features.Reports.Requests;

namespace sfa_api.Features.Reports.Repositories;

public interface ISalesSummaryRepository
{
    /// <summary>
    /// Aggregates sales facts over <c>BillingItems</c> for the requested range, filters and grouping
    /// dimension. Returns at most <paramref name="maxGroups"/> + 1 rows so the caller can detect an
    /// overflow without materialising an unbounded result.
    /// </summary>
    Task<List<SalesSummarySalesAgg>> GetSalesAggregatesAsync(
        SalesSummaryQuery query, int maxGroups, CancellationToken ct = default);

    /// <summary>
    /// Aggregates sales targets to (group, year, month) grain — one query per month so the
    /// (dimension, Year, Month) composite indexes stay usable as equality seeks.
    /// <para>
    /// Returns an empty list when targets cannot be attributed: grouping by Route or Outlet
    /// (SalesTarget has no such column), or filtering by RouteId for the same reason.
    /// </para>
    /// </summary>
    Task<List<SalesSummaryTargetAgg>> GetTargetAggregatesAsync(
        SalesSummaryQuery query,
        IReadOnlyList<(int Year, int Month)> months,
        int maxGroups,
        CancellationToken ct = default);

    /// <summary>
    /// Resolves display labels for the given group keys.
    /// <para>
    /// Uses <c>IgnoreQueryFilters()</c> deliberately. Outlets, routes and geo entities carry a global
    /// <c>IsActive &amp;&amp; !IsDeleted</c> filter; honouring it here would drop the label — and, if the
    /// lookup were done through a required navigation instead, the whole fact row — for anything
    /// since deactivated. A bill written to a retired outlet is still real revenue.
    /// </para>
    /// </summary>
    Task<Dictionary<int, SalesSummaryLabel>> GetLabelsAsync(
        Enums.SalesSummaryGroupBy groupBy, IReadOnlyList<int> ids, CancellationToken ct = default);
}
