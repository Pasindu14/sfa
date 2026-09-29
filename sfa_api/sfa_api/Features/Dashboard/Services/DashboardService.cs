using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Dashboard.DTOs;
using sfa_api.Features.Dashboard.Repositories;
using sfa_api.Features.Reports.DTOs;
using sfa_api.Features.Reports.Enums;
using sfa_api.Features.Reports.Services;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.Dashboard.Services;

/// <summary>
/// Composes the admin dashboard. Revenue, targets, discounts and returns are taken from the Sales
/// Summary service rather than re-derived, so a dashboard tile and the report for the same range are
/// the same number. Only the facts the report has no column for (bill/rep/outlet counts and the
/// per-day series) come from <see cref="IDashboardRepository"/>.
/// <para>
/// Split into three sections, each its own request and cache entry, so the client can fetch them in
/// parallel. Within a section the queries stay sequential — AppDbContext does not support
/// concurrent operations.
/// </para>
/// </summary>
public class DashboardService(
    IDashboardRepository repository,
    ISalesSummaryService salesSummary,
    ICacheService cache) : IDashboardService
{
    private readonly IDashboardRepository _repository = repository;
    private readonly ISalesSummaryService _salesSummary = salesSummary;
    private readonly ICacheService _cache = cache;

    /// <summary>Short — this is a monitoring screen, and the underlying report caches for 5 minutes anyway.</summary>
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    /// <summary>"Billing outlets within the last 45 days" — the window includes the dashboard date itself.</summary>
    public const int BilledOutletWindowDays = 45;

    // ── Sections ──────────────────────────────────────────────────────────────────────────────

    public Task<DashboardSalesDto> GetSalesAsync(DateOnly? date, CancellationToken ct = default)
        => Cached("sales", date, ct, async (day, ct) =>
        {
            var (monthStart, monthEnd) = MonthOf(day);

            // Grouped by Region so the same calls also feed the regional breakdown; totals are the
            // same for any grouping.
            var daySummary   = await _salesSummary.GetSalesSummaryAsync(new(SalesSummaryGroupBy.Region, day, day), ct);
            var mtdSummary   = await _salesSummary.GetSalesSummaryAsync(new(SalesSummaryGroupBy.Region, monthStart, day), ct);
            // Only its targets are read: for a past date its sales would include days after `day`.
            var monthSummary = await _salesSummary.GetSalesSummaryAsync(new(SalesSummaryGroupBy.Region, monthStart, monthEnd), ct);

            var dayBills = await _repository.CountRevenueBillsAsync(day, day, ct);
            var mtdBills = await _repository.CountRevenueBillsAsync(monthStart, day, ct);

            var monthTarget = monthSummary.Totals.TargetValue is > 0m ? monthSummary.Totals.TargetValue : null;
            var mtd = SalesBlock(mtdSummary.Totals, mtdBills);

            return new DashboardSalesDto(
                day, monthStart, monthEnd, monthEnd.Day, day.Day,
                Today:       SalesBlock(daySummary.Totals, dayBills),
                MonthToDate: mtd,
                MonthTarget: BuildMonthTarget(monthTarget, mtd.Revenue, monthEnd.Day, day.Day),
                Regions:     BuildRegions(mtdSummary.Rows, monthSummary.Rows),
                GeneratedAtUtc: DateTime.UtcNow);
        });

    public Task<DashboardActivityDto> GetActivityAsync(DateOnly? date, CancellationToken ct = default)
        => Cached("activity", date, ct, async (day, ct) =>
        {
            var windowFrom = day.AddDays(-(BilledOutletWindowDays - 1));

            var activeReps = await _repository.CountActiveRepsAsync(day, ct);
            var totalReps  = await _repository.CountSalesRepsAsync(ct);
            var outlets    = await _repository.GetOutletCountsAsync(day, ct);
            var billed45   = await _repository.CountBilledOutletsAsync(windowFrom, day, ct);

            return new DashboardActivityDto(
                day,
                new DashboardRepsDto(activeReps, totalReps, Percent(activeReps, totalReps)),
                BuildOutlets(outlets, billed45, windowFrom),
                DateTime.UtcNow);
        });

    public Task<DashboardTrendDto> GetTrendAsync(DateOnly? date, CancellationToken ct = default)
        => Cached("trend", date, ct, async (day, ct) =>
        {
            var (monthStart, monthEnd) = MonthOf(day);
            var revenue = await _repository.GetDailyRevenueAsync(monthStart, day, ct);

            return new DashboardTrendDto(
                day, monthStart, monthEnd.Day,
                BuildDailyTrend(revenue, monthStart, day),
                DateTime.UtcNow);
        });

    /// <summary>How many entries each ranking shows before the rest are rolled up.</summary>
    public const int TopProducts = 8;
    public const int TopReps = 8;
    public const int TopDistributors = 6;

    public Task<DashboardBreakdownDto> GetBreakdownAsync(DateOnly? date, CancellationToken ct = default)
        => Cached("breakdown", date, ct, async (day, ct) =>
        {
            var (monthStart, _) = MonthOf(day);

            // Month to date, so each rep's target is pro-rated to the days elapsed — the ranking
            // measures pace, not a full month the reps have not had yet.
            var products     = await _salesSummary.GetSalesSummaryAsync(new(SalesSummaryGroupBy.Product, monthStart, day), ct);
            var reps         = await _salesSummary.GetSalesSummaryAsync(new(SalesSummaryGroupBy.SalesRep, monthStart, day), ct);
            var distributors = await _salesSummary.GetSalesSummaryAsync(new(SalesSummaryGroupBy.Distributor, monthStart, day), ct);

            var saleVisits = await _repository.CountLiveBillsAsync(monthStart, day, ct);
            var reasons    = await _repository.GetNoSaleReasonsAsync(monthStart, day, ct);

            var total = products.Totals.NetSaleValue;
            var topDistributors = Rank(distributors.Rows, total, TopDistributors);

            return new DashboardBreakdownDto(
                day, monthStart,
                Products:          Rank(products.Rows, total, TopProducts),
                Reps:              Rank(reps.Rows, total, TopReps),
                Distributors:      topDistributors,
                OtherDistributors: Others(distributors.Rows, topDistributors.Count, total),
                Visits:            BuildVisits(saleVisits, reasons),
                GeneratedAtUtc:    DateTime.UtcNow);
        });

    // ── Plumbing ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Resolves the business day, rejects a future one, and caches the section per day.</summary>
    private async Task<T> Cached<T>(
        string section, DateOnly? date, CancellationToken ct, Func<DateOnly, CancellationToken, Task<T>> build)
        where T : class
    {
        var today = SriLankaTime.Today;
        var day   = date ?? today;
        if (day > today)
            throw new BusinessRuleException("DASHBOARD_FUTURE_DATE", "The dashboard cannot be shown for a future date.");

        var cacheKey = $"dashboard:{section}:{day.DayNumber}";
        var cached = await _cache.GetAsync<T>(cacheKey, ct);
        if (cached is not null) return cached;

        var result = await build(day, ct);
        await _cache.SetAsync(cacheKey, result, CacheTtl, ct);
        return result;
    }

    private static (DateOnly Start, DateOnly End) MonthOf(DateOnly day)
    {
        var start = new DateOnly(day.Year, day.Month, 1);
        return (start, start.AddMonths(1).AddDays(-1));
    }

    // ── Builders (pure) ───────────────────────────────────────────────────────────────────────

    public static DashboardSalesBlockDto SalesBlock(SalesSummaryTotalsDto t, int billCount) => new(
        TargetValue:        t.TargetValue is > 0m ? t.TargetValue : null,
        Revenue:            t.NetSaleValue,
        AchievementPercent: t.AchievementPercent,
        GrossSaleValue:     t.GrossSaleValue,
        Discount:           t.Discount,
        DbDiscount:         t.DbDiscount,
        TotalDiscount:      t.Discount + t.DbDiscount,
        GoodReturn:         t.GoodReturn,
        MarketReturn:       t.MarketReturn,
        TotalReturn:        t.GoodReturn + t.MarketReturn,
        BillCount:          billCount);

    public static DashboardTargetDto BuildMonthTarget(
        decimal? target, decimal revenue, int daysInMonth, int daysElapsed)
    {
        if (target is not decimal t) return new(null, null, null, null, null);

        var remainingDays = daysInMonth - daysElapsed;
        var balance       = Math.Round(t - revenue, 2);

        return new DashboardTargetDto(
            TargetValue:        t,
            ExpectedToDate:     Math.Round(t * daysElapsed / daysInMonth, 2),
            AchievementPercent: Math.Round(revenue / t * 100m, 2),
            Balance:            balance,
            RequiredDailyRate:  remainingDays > 0 ? Math.Round(Math.Max(balance, 0m) / remainingDays, 2) : null);
    }

    public static DashboardOutletsDto BuildOutlets(DashboardOutletCounts c, int billed45, DateOnly windowFrom)
    {
        var total = c.Active + c.Inactive;
        return new DashboardOutletsDto(
            ActiveOutlets:           c.Active,
            InactiveOutlets:         c.Inactive,
            TotalCustomers:          total,
            ActivePercent:           Percent(c.Active, total),
            InactivePercent:         Percent(c.Inactive, total),
            BilledLast45Days:        billed45,
            BilledLast45DaysPercent: Percent(billed45, c.Active),
            BilledWindowFrom:        windowFrom,
            NewToday:                c.NewOnDate);
    }

    /// <summary>
    /// One point per day from <paramref name="from"/> to <paramref name="to"/>, zero-filled — a day
    /// with no sales is a real zero on the chart, not a gap.
    /// </summary>
    public static List<DashboardDailyPointDto> BuildDailyTrend(
        IReadOnlyList<DashboardDailyRevenueAgg> revenue, DateOnly from, DateOnly to)
    {
        var byDate = revenue.ToDictionary(r => r.Date, r => r.Revenue);

        var points = new List<DashboardDailyPointDto>();
        var cumulative = 0m;
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            var rev = byDate.GetValueOrDefault(d);
            cumulative += rev;
            points.Add(new DashboardDailyPointDto(d, rev, cumulative));
        }
        return points;
    }

    /// <summary>Month-to-date revenue per region against that region's full-month target, largest first.</summary>
    public static List<DashboardRegionRowDto> BuildRegions(
        IReadOnlyList<SalesSummaryRowDto> mtdRows, IReadOnlyList<SalesSummaryRowDto> monthRows)
    {
        // ToLookup, not ToDictionary: the null "(Unassigned)" region is a real bucket, and
        // Dictionary rejects a null key even for int?.
        var mtd    = mtdRows.ToLookup(r => r.GroupKey);
        var target = monthRows.ToLookup(r => r.GroupKey);

        return mtd.Select(g => g.Key).Union(target.Select(g => g.Key))
            .Select(key =>
            {
                var m = mtd[key].FirstOrDefault();
                var t = target[key].FirstOrDefault();
                var revenue = m?.NetSaleValue ?? 0m;
                decimal? monthTarget = t?.TargetValue is > 0m ? t.TargetValue : null;
                return new DashboardRegionRowDto(
                    key,
                    m?.GroupName ?? t?.GroupName ?? "(Unassigned)",
                    monthTarget,
                    revenue,
                    monthTarget is decimal mt ? Math.Round(revenue / mt * 100m, 2) : null);
            })
            .OrderByDescending(r => r.Revenue)
            .ThenBy(r => r.RegionName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// The top <paramref name="take"/> rows by revenue, each with its share of <paramref name="total"/>.
    /// Only rows that actually earned something rank: a product whose returns outweighed its sales
    /// has negative revenue, and listing it among the "top" would be nonsense.
    /// </summary>
    public static List<DashboardRankedDto> Rank(IReadOnlyList<SalesSummaryRowDto> rows, decimal total, int take)
        => rows
            .Where(r => r.NetSaleValue > 0m)
            .OrderByDescending(r => r.NetSaleValue)
            .ThenBy(r => r.GroupName, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .Select(r => new DashboardRankedDto(
                r.GroupKey,
                r.GroupCode,
                r.GroupName,
                r.NetSaleValue,
                Share(r.NetSaleValue, total),
                r.NetSaleQty,
                r.TargetValue is > 0m ? r.TargetValue : null,
                r.AchievementPercent))
            .ToList();

    /// <summary>Everything after the first <paramref name="shown"/> rows by revenue; null when nothing is left over.</summary>
    public static DashboardOthersDto? Others(IReadOnlyList<SalesSummaryRowDto> rows, int shown, decimal total)
    {
        var rest = rows.OrderByDescending(r => r.NetSaleValue).Skip(shown).ToList();
        if (rest.Count == 0) return null;
        var revenue = rest.Sum(r => r.NetSaleValue);
        return new DashboardOthersDto(rest.Count, revenue, Share(revenue, total));
    }

    /// <summary>Sale visits against no-sale visits, with the no-sale reasons largest first.</summary>
    public static DashboardVisitsDto BuildVisits(int saleVisits, IReadOnlyList<DashboardReasonCount> reasons)
    {
        var noSale = reasons.Sum(r => r.Count);
        return new DashboardVisitsDto(
            saleVisits,
            noSale,
            Percent(saleVisits, saleVisits + noSale),
            reasons
                .OrderByDescending(r => r.Count)
                .ThenBy(r => r.Reason.ToString(), StringComparer.Ordinal)
                .Select(r => new DashboardReasonDto(r.Reason.ToString(), r.Count, Percent(r.Count, noSale)))
                .ToList());
    }

    /// <summary>A money share, rounded to one decimal; null when there is nothing to share.</summary>
    private static decimal? Share(decimal part, decimal total)
        => total <= 0m ? null : Math.Round(part / total * 100m, 1);

    /// <summary>Null when the whole is zero — a percentage of nothing is not 0%.</summary>
    public static decimal? Percent(int part, int whole)
        => whole == 0 ? null : Math.Round(part * 100m / whole, 1);
}
