using sfa_api.Common.Errors;
using sfa_api.Features.Reports.DTOs;
using sfa_api.Features.Reports.Enums;
using sfa_api.Features.Reports.Repositories;
using sfa_api.Features.Reports.Requests;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.Reports.Services;

/// <summary>
/// Builds the sales-summary report. The repository returns raw SUMs; every column formula lives
/// here, in memory, so the arithmetic is unit-testable without a database — which matters more than
/// usual because the SQLite test provider cannot translate SUM over decimal and therefore cannot
/// exercise the repository at all.
/// </summary>
public class SalesSummaryService(
    ISalesSummaryRepository repository,
    ICacheService cache,
    ILogger<SalesSummaryService> logger) : ISalesSummaryService
{
    private readonly ISalesSummaryRepository _repository = repository;
    private readonly ICacheService _cache = cache;
    private readonly ILogger<SalesSummaryService> _logger = logger;

    /// <summary>
    /// Tripwire, not a page size. Grouping by Outlet company-wide is 20k–50k rows, which would blow
    /// the response size budget; everything else is comfortably under a few thousand.
    /// </summary>
    private const int MaxGroups = 5000;

    /// <summary>Matches the 5-minute convention used by every other sales aggregate (BillingService.cs:53).</summary>
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private const string RouteOutletReason =
        "Sales targets are recorded per sales rep and product, not per route or outlet, so they cannot be attributed to this grouping.";

    private const string RouteFilterReason =
        "Sales targets carry no route, so they cannot be filtered to a single route. Remove the route filter to see targets.";

    public async Task<SalesSummaryResponseDto> GetSalesSummaryAsync(
        SalesSummaryQuery q, CancellationToken ct = default)
    {
        var cacheKey = BuildCacheKey(q);

        var cached = await _cache.GetAsync<SalesSummaryResponseDto>(cacheKey, ct);
        if (cached is not null) return cached;

        // Why targets may be missing, decided once and reported to the UI so it can explain the
        // dashes rather than leaving the reader to guess.
        var targetsUnavailableReason =
            q.GroupBy is SalesSummaryGroupBy.Route or SalesSummaryGroupBy.Outlet ? RouteOutletReason
            : q.RouteId is not null                                              ? RouteFilterReason
            : null;
        var targetsAvailable = targetsUnavailableReason is null;

        var months = SalesSummaryProration.MonthWeights(q.From, q.To);

        // Sequential — AppDbContext does not support concurrent operations (SupervisorService.cs:19).
        var sales = await _repository.GetSalesAggregatesAsync(q, MaxGroups, ct);
        GuardGroupCount(sales.Count, q);

        var targetRows = targetsAvailable
            ? await _repository.GetTargetAggregatesAsync(
                q, [.. months.Select(m => (m.Year, m.Month))], MaxGroups, ct)
            : [];

        var factor = months.ToDictionary(m => (m.Year, m.Month), m => m.Factor);

        // ILookup, not Dictionary. The grouping key is nullable — Billing.RouteId, TerritoryId,
        // DivisionId and friends are nullable columns, and their null bucket is a real
        // "(Unassigned)" row that must survive or the report stops summing to the company total.
        // Dictionary<TKey,TValue> rejects a null key even when TKey is a nullable value type
        // (TryInsert null-checks the key before ever consulting the comparer); ToLookup allows it.
        var salesByKey   = sales.ToLookup(s => s.GroupKey);
        var targetsByKey = targetRows.ToLookup(t => t.GroupKey);

        // Full-outer-join emulation (BillingService.cs:863). A group with a target and zero sales
        // must still produce a row — that 0%-achievement line is the point of the report — and a
        // group with sales but no imported target must appear too.
        var keys = salesByKey.Select(g => g.Key)
            .Union(targetsByKey.Select(g => g.Key))
            .ToList();
        GuardGroupCount(keys.Count, q);

        var labels = await _repository.GetLabelsAsync(
            q.GroupBy, [.. keys.Where(k => k.HasValue).Select(k => k!.Value)], ct);

        var rows = new List<SalesSummaryRowDto>(keys.Count);
        foreach (var key in keys)
        {
            var s = salesByKey[key].FirstOrDefault() ?? EmptySales(key);

            // Pro-rate each month's target by the share of its days the requested range covers,
            // then roll the months up. Rounded because a decimal factor like 16/30 does not divide
            // exactly — without this the response carries 28 digits of division noise.
            var slices = targetsByKey[key].ToList();
            (decimal Qty, decimal Value)? target = slices.Count == 0
                ? null
                : (Math.Round(slices.Sum(x => x.TargetQtyPacks * factor[(x.Year, x.Month)]), 2),
                   Math.Round(slices.Sum(x => x.TargetValue    * factor[(x.Year, x.Month)]), 2));

            rows.Add(BuildRow(key, s, target, targetsAvailable, labels));
        }

        // Largest contributor first — the order a manager reads this report in. Name breaks ties so
        // the output is deterministic across runs (important for the Excel export diffing cleanly).
        var ordered = rows
            .OrderByDescending(r => r.NetSaleValue)
            .ThenBy(r => r.GroupName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var response = new SalesSummaryResponseDto(
            q.GroupBy, q.From, q.To,
            targetsAvailable, targetsUnavailableReason,
            ordered.Count,
            ordered,
            BuildTotals(ordered, targetsAvailable));

        await _cache.SetAsync(cacheKey, response, CacheTtl, ct);
        return response;
    }

    // ── Column math ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The 12 report columns.
    /// <para>
    /// Note two deliberate asymmetries, both from the report's own specification — do NOT "fix" them:
    /// Gross Sale Value nets off Good Returns but Net Sale Value does not deduct them again, and
    /// Net Sale Qty subtracts Good Return Qty only — Market Return Qty is never deducted from a
    /// quantity, because damage/expiry stock never re-enters saleable inventory.
    /// </para>
    /// <para>
    /// Substituting Billing.TotalAmount = SubTotal - BillDiscount - GoodReturn
    /// (BillingService.cs:250) collapses these to an identity worth asserting in tests:
    /// <c>NetSaleValue == SUM(Billing.TotalAmount) - MarketReturn - SUM(Billing.FreeIssueValueDistributor)</c>.
    /// </para>
    /// </summary>
    private static SalesSummaryRowDto BuildRow(
        int? key,
        SalesSummarySalesAgg s,
        (decimal Qty, decimal Value)? target,
        bool targetsAvailable,
        IReadOnlyDictionary<int, SalesSummaryLabel> labels)
    {
        // Rounded to money scale. The pro-rata bill-discount term is a division
        // (TotalPrice × rate / 100), so without this it drags 20+ digits of scale through
        // Discount and Net Sale Value and out into the JSON.
        var grossSaleValue = Money(s.SaleGross - s.GoodReturnValue);
        var discount       = Money(s.ItemWiseDiscount + s.BillDiscount);
        var netSaleValue   = Money(grossSaleValue - s.MarketReturnValue - (s.DbDiscount + discount));
        var netSaleQty     = s.SaleQty - s.GoodReturnQty;   // both are direct SUMs — no scale drift

        // null, never 0m: a zero renders as "0% achievement", which is a wrong answer rather than a
        // missing one.
        decimal? targetValue = targetsAvailable ? target?.Value ?? 0m : null;
        decimal? targetQty   = targetsAvailable ? target?.Qty   ?? 0m : null;

        var (code, name) = ResolveLabel(key, labels);

        return new SalesSummaryRowDto(
            key, code, name,
            targetValue, targetQty,
            grossSaleValue, s.SaleQty,
            Money(s.GoodReturnValue), s.GoodReturnQty,
            Money(s.MarketReturnValue), s.MarketReturnQty,
            Money(s.DbDiscount), discount,
            netSaleValue, netSaleQty,
            Achievement(netSaleValue, targetValue));
    }

    private static SalesSummaryTotalsDto BuildTotals(
        IReadOnlyList<SalesSummaryRowDto> rows, bool targetsAvailable)
    {
        decimal? targetValue = targetsAvailable ? rows.Sum(r => r.TargetValue ?? 0m) : null;
        decimal? targetQty   = targetsAvailable ? rows.Sum(r => r.TargetQty   ?? 0m) : null;
        var netSaleValue     = rows.Sum(r => r.NetSaleValue);

        return new SalesSummaryTotalsDto(
            targetValue, targetQty,
            rows.Sum(r => r.GrossSaleValue),
            rows.Sum(r => r.SaleQty),
            rows.Sum(r => r.GoodReturn),
            rows.Sum(r => r.GoodReturnQty),
            rows.Sum(r => r.MarketReturn),
            rows.Sum(r => r.MarketReturnQty),
            rows.Sum(r => r.DbDiscount),
            rows.Sum(r => r.Discount),
            netSaleValue,
            rows.Sum(r => r.NetSaleQty),
            Achievement(netSaleValue, targetValue));
    }

    /// <summary>Null when there is no target, or when the target is zero — a percentage of nothing is not 0%.</summary>
    private static decimal? Achievement(decimal netSaleValue, decimal? targetValue)
        => targetValue is null or 0m ? null : Math.Round(netSaleValue / targetValue.Value * 100m, 2);

    private static decimal Money(decimal value) => Math.Round(value, 2);

    // ── Helpers ───────────────────────────────────────────────────────────────────────────────

    private static SalesSummarySalesAgg EmptySales(int? key)
        => new(key, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);

    private static (string Code, string Name) ResolveLabel(
        int? key, IReadOnlyDictionary<int, SalesSummaryLabel> labels)
    {
        // A null FK is a real bucket (a bill whose route/territory was never set), not a row to drop
        // — dropping it would stop the report summing to the company total.
        if (key is not int id) return (string.Empty, "(Unassigned)");

        return labels.TryGetValue(id, out var label)
            ? (label.Code, label.Name)
            : (string.Empty, $"#{id}");   // defensive, same shape as BillingService.cs:874-875
    }

    private void GuardGroupCount(int count, SalesSummaryQuery q)
    {
        if (count <= MaxGroups) return;

        _logger.LogWarning(
            "Sales summary exceeded {MaxGroups} groups for {GroupBy} over {From}..{To}",
            MaxGroups, q.GroupBy, q.From, q.To);

        throw new BusinessRuleException(
            "REPORT_TOO_MANY_GROUPS",
            $"This report would return more than {MaxGroups} rows. Narrow the filters or choose a coarser grouping.");
    }

    private static string BuildCacheKey(SalesSummaryQuery q) => string.Join(':',
        "sales-summary", q.GroupBy, q.From.DayNumber, q.To.DayNumber,
        q.RegionId, q.AreaId, q.TerritoryId, q.DivisionId, q.RouteId,
        q.DistributorId, q.SalesRepId, q.SupervisorId, q.ProductId);
}
