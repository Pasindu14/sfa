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

    public async Task<DashboardDto> GetAsync(DateOnly? date, CancellationToken ct = default)
    {
        var today = SriLankaTime.Today;
        var day   = date ?? today;
        if (day > today)
            throw new BusinessRuleException("DASHBOARD_FUTURE_DATE", "The dashboard cannot be shown for a future date.");

        var cacheKey = $"dashboard:{day.DayNumber}";
        var cached = await _cache.GetAsync<DashboardDto>(cacheKey, ct);
        if (cached is not null) return cached;

        var monthStart  = new DateOnly(day.Year, day.Month, 1);
        var monthEnd    = monthStart.AddMonths(1).AddDays(-1);
        var daysInMonth = monthEnd.Day;
        var daysElapsed = day.Day;
        var windowFrom  = day.AddDays(-(BilledOutletWindowDays - 1));

        // Sequential throughout — AppDbContext does not support concurrent operations.
        // Grouped by Region so the same calls also feed the regional breakdown; totals are the same
        // for any grouping.
        var daySummary   = await _salesSummary.GetSalesSummaryAsync(new(SalesSummaryGroupBy.Region, day, day), ct);
        var mtdSummary   = await _salesSummary.GetSalesSummaryAsync(new(SalesSummaryGroupBy.Region, monthStart, day), ct);
        // Only its targets are read: for a past date its sales would include days after `day`.
        var monthSummary = await _salesSummary.GetSalesSummaryAsync(new(SalesSummaryGroupBy.Region, monthStart, monthEnd), ct);

        var dayBills     = await _repository.CountRevenueBillsAsync(day, day, ct);
        var mtdBills     = await _repository.CountRevenueBillsAsync(monthStart, day, ct);
        var dailyRevenue = await _repository.GetDailyRevenueAsync(monthStart, day, ct);
        var activeReps   = await _repository.CountActiveRepsAsync(day, ct);
        var totalReps    = await _repository.CountSalesRepsAsync(ct);
        var outlets      = await _repository.GetOutletCountsAsync(day, ct);
        var billed45     = await _repository.CountBilledOutletsAsync(windowFrom, day, ct);

        var monthTargetValue = monthSummary.Totals.TargetValue is > 0m ? monthSummary.Totals.TargetValue : null;
        var mtd = SalesBlock(mtdSummary.Totals, mtdBills);

        var dto = new DashboardDto(
            day, monthStart, monthEnd, daysInMonth, daysElapsed,
            Today:       SalesBlock(daySummary.Totals, dayBills),
            MonthToDate: mtd,
            MonthTarget: BuildMonthTarget(monthTargetValue, mtd.Revenue, daysInMonth, daysElapsed),
            Reps:        new DashboardRepsDto(activeReps, totalReps, Percent(activeReps, totalReps)),
            Outlets:     BuildOutlets(outlets, billed45, windowFrom),
            DailyTrend:  BuildDailyTrend(dailyRevenue, monthStart, day, monthTargetValue, daysInMonth),
            Regions:     BuildRegions(mtdSummary.Rows, monthSummary.Rows),
            GeneratedAtUtc: DateTime.UtcNow);

        await _cache.SetAsync(cacheKey, dto, CacheTtl, ct);
        return dto;
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
    /// with no sales is a real zero on the chart, not a gap. The daily target is the month's target
    /// spread evenly, the same pro-rating the Sales Summary report applies to a one-day range.
    /// </summary>
    public static List<DashboardDailyPointDto> BuildDailyTrend(
        IReadOnlyList<DashboardDailyRevenueAgg> revenue, DateOnly from, DateOnly to,
        decimal? monthTarget, int daysInMonth)
    {
        var byDate = revenue.ToDictionary(r => r.Date, r => r.Revenue);
        decimal? dailyTarget = monthTarget / daysInMonth;

        var points = new List<DashboardDailyPointDto>();
        var cumulative = 0m;
        var dayIndex = 0;
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            dayIndex++;
            var rev = byDate.GetValueOrDefault(d);
            cumulative += rev;
            points.Add(new DashboardDailyPointDto(
                d, rev,
                dailyTarget is decimal dt ? Math.Round(dt, 2) : null,
                cumulative,
                dailyTarget is decimal perDay ? Math.Round(perDay * dayIndex, 2) : null));
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

    /// <summary>Null when the whole is zero — a percentage of nothing is not 0%.</summary>
    public static decimal? Percent(int part, int whole)
        => whole == 0 ? null : Math.Round(part * 100m / whole, 1);
}
