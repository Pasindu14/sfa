namespace sfa_api.Features.Dashboard.DTOs;

/// <summary>
/// The admin dashboard is served as three independent sections so the page can request them in
/// parallel and render each as soon as it lands — the cheap counts never wait behind the heavier
/// sales aggregates. Every sales figure is the Sales Summary report's own number (same service,
/// same formulas), so the dashboard and the report can never disagree.
/// </summary>
public record DashboardSalesDto(
    DateOnly Date,
    DateOnly MonthStart,
    DateOnly MonthEnd,
    int      DaysInMonth,
    int      DaysElapsed,
    DashboardSalesBlockDto Today,
    DashboardSalesBlockDto MonthToDate,
    DashboardTargetDto     MonthTarget,
    IReadOnlyList<DashboardRegionRowDto> Regions,
    DateTime GeneratedAtUtc);

/// <summary>Field-force and outlet counts for the day — no sales aggregates, so it answers fast.</summary>
public record DashboardActivityDto(
    DateOnly Date,
    DashboardRepsDto    Reps,
    DashboardOutletsDto Outlets,
    DateTime GeneratedAtUtc);

/// <summary>
/// Revenue per day from the 1st of the month to the dashboard date. Carries no target: the client
/// draws the target line from the sales section's month target, so this endpoint never repeats the
/// sales-summary target query.
/// </summary>
public record DashboardTrendDto(
    DateOnly Date,
    DateOnly MonthStart,
    int      DaysInMonth,
    IReadOnlyList<DashboardDailyPointDto> Points,
    DateTime GeneratedAtUtc);

/// <summary>
/// Month-to-date leaderboards and the no-sale picture: who and what is driving revenue, and why
/// visits end without a sale. The rankings come from the Sales Summary service (grouped by product,
/// rep and distributor), so every figure matches the report.
/// </summary>
public record DashboardBreakdownDto(
    DateOnly Date,
    DateOnly MonthStart,
    IReadOnlyList<DashboardRankedDto> Products,
    IReadOnlyList<DashboardRankedDto> Reps,
    IReadOnlyList<DashboardRankedDto> Distributors,
    DashboardOthersDto? OtherDistributors,
    DashboardVisitsDto Visits,
    DateTime GeneratedAtUtc);

/// <summary>One entry in a month-to-date ranking.</summary>
/// <param name="SharePercent">This entry's share of the month's total revenue.</param>
/// <param name="Quantity">Net packs sold (products only; meaningful for any grouping).</param>
/// <param name="TargetValue">The target pro-rated to the days elapsed; null when none was imported.</param>
/// <param name="AchievementPercent">Revenue against <paramref name="TargetValue"/> — "on pace" rather than against the full month.</param>
public record DashboardRankedDto(
    int?     Id,
    string   Code,
    string   Name,
    decimal  Revenue,
    decimal? SharePercent,
    decimal  Quantity,
    decimal? TargetValue,
    decimal? AchievementPercent);

/// <summary>Everything outside a ranking's top entries, rolled into one line.</summary>
public record DashboardOthersDto(int Count, decimal Revenue, decimal? SharePercent);

/// <param name="SaleVisits">Visits that produced a live (not cancelled or rejected) bill.</param>
/// <param name="NoSaleVisits">Visits logged as no-sale.</param>
/// <param name="SalePercent">Share of all visits that ended in a sale.</param>
public record DashboardVisitsDto(
    int SaleVisits,
    int NoSaleVisits,
    decimal? SalePercent,
    IReadOnlyList<DashboardReasonDto> Reasons);

/// <param name="Reason">The <c>NotBillingReason</c> member name; the client owns the wording.</param>
public record DashboardReasonDto(string Reason, int Count, decimal? SharePercent);

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

/// <summary>One day of the month, zero-filled, with the running month-to-date total.</summary>
public record DashboardDailyPointDto(DateOnly Date, decimal Revenue, decimal CumulativeRevenue);

public record DashboardRegionRowDto(
    int?     RegionId,
    string   RegionName,
    decimal? MonthTarget,
    decimal  Revenue,
    decimal? AchievementPercent);

// ─── Repository → service aggregates ──────────────────────────────────────────────────────────

public record DashboardDailyRevenueAgg(DateOnly Date, decimal Revenue);

public record DashboardOutletCounts(int Active, int Inactive, int NewOnDate);

public record DashboardReasonCount(sfa_api.Features.NotBillings.Enums.NotBillingReason Reason, int Count);
