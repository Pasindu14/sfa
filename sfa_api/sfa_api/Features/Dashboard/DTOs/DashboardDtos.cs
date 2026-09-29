namespace sfa_api.Features.Dashboard.DTOs;

/// <summary>
/// The admin dashboard for one business day. Every sales figure is the Sales Summary report's own
/// number (same service, same formulas), so the dashboard and the report can never disagree.
/// </summary>
public record DashboardDto(
    DateOnly Date,
    DateOnly MonthStart,
    DateOnly MonthEnd,
    int      DaysInMonth,
    int      DaysElapsed,
    DashboardSalesBlockDto Today,
    DashboardSalesBlockDto MonthToDate,
    DashboardTargetDto     MonthTarget,
    DashboardRepsDto       Reps,
    DashboardOutletsDto    Outlets,
    IReadOnlyList<DashboardDailyPointDto>   DailyTrend,
    IReadOnlyList<DashboardRegionRowDto>    Regions,
    DateTime GeneratedAtUtc);

/// <summary>
/// Sales facts over a date range. <paramref name="TargetValue"/> is pro-rated to the range (a day's
/// share of the month for Today, the elapsed days' share for MonthToDate); null when no target is
/// imported, so the UI shows a dash instead of a misleading 0%.
/// </summary>
/// <param name="Revenue">Net sale value — gross sales less returns and every discount.</param>
/// <param name="Discount">Item-wise + bill-level discount granted to outlets.</param>
/// <param name="DbDiscount">Distributor-funded free issues.</param>
/// <param name="GoodReturn">Resaleable (MarketResell) returns.</param>
/// <param name="MarketReturn">Damage + Expire write-off returns.</param>
public record DashboardSalesBlockDto(
    decimal? TargetValue,
    decimal  Revenue,
    decimal? AchievementPercent,
    decimal  GrossSaleValue,
    decimal  Discount,
    decimal  DbDiscount,
    decimal  TotalDiscount,
    decimal  GoodReturn,
    decimal  MarketReturn,
    decimal  TotalReturn,
    int      BillCount);

/// <summary>The whole calendar month's target, and how the month-to-date revenue measures against it.</summary>
/// <param name="ExpectedToDate">The target pro-rated to the elapsed days — where revenue "should" be by now.</param>
/// <param name="RequiredDailyRate">Revenue per remaining day needed to hit the full target; null once the month is over or there is no target.</param>
public record DashboardTargetDto(
    decimal? TargetValue,
    decimal? ExpectedToDate,
    decimal? AchievementPercent,
    decimal? Balance,
    decimal? RequiredDailyRate);

/// <param name="ActiveToday">Distinct reps with a bill or a no-sale visit on the day.</param>
/// <param name="TotalReps">Active sales-rep accounts.</param>
public record DashboardRepsDto(int ActiveToday, int TotalReps, decimal? ActivePercent);

/// <param name="ActiveOutlets">Active shops right now (current snapshot).</param>
/// <param name="TotalCustomers">Every non-deleted outlet, active or deactivated.</param>
/// <param name="BilledLast45Days">Distinct outlets with a live (not cancelled / rejected) bill in the 45 days ending on the dashboard date.</param>
/// <param name="NewToday">Outlets registered on the dashboard date that are active.</param>
public record DashboardOutletsDto(
    int      ActiveOutlets,
    int      InactiveOutlets,
    int      TotalCustomers,
    decimal? ActivePercent,
    decimal? InactivePercent,
    int      BilledLast45Days,
    decimal? BilledLast45DaysPercent,
    DateOnly BilledWindowFrom,
    int      NewToday);

/// <summary>One day of the month: revenue against that day's pro-rated share of the monthly target.</summary>
public record DashboardDailyPointDto(DateOnly Date, decimal Revenue, decimal? Target, decimal CumulativeRevenue, decimal? CumulativeTarget);

public record DashboardRegionRowDto(
    int?     RegionId,
    string   RegionName,
    decimal? MonthTarget,
    decimal  Revenue,
    decimal? AchievementPercent);

// ─── Repository → service aggregates ──────────────────────────────────────────────────────────

public record DashboardDailyRevenueAgg(DateOnly Date, decimal Revenue);

public record DashboardOutletCounts(int Active, int Inactive, int NewOnDate);
