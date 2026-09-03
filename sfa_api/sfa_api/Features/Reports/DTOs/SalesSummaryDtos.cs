using sfa_api.Features.Reports.Enums;

namespace sfa_api.Features.Reports.DTOs;

// ─── Repository → service aggregates (flat SQL projection targets) ────────────────────────────

/// <summary>
/// One grouped row of sales facts, straight out of SQL. Every member is a raw SUM — the report's
/// column formulas are applied in <c>SalesSummaryService</c>, not here, so the arithmetic stays
/// unit-testable without a database.
/// </summary>
/// <param name="GroupKey">
/// The grouping dimension's id. Nullable because the denormalized columns on <c>Billing</c>
/// (RouteId, DivisionId, TerritoryId…) are nullable — a null key is a real "(Unassigned)" bucket
/// and must not be dropped, or the report stops summing to the company total.
/// </param>
/// <param name="SaleGross">Sale lines valued BEFORE any discount.</param>
/// <param name="SaleQty">Sale-line quantity, in packs.</param>
/// <param name="ItemWiseDiscount">Per-line discount on sale lines.</param>
/// <param name="BillDiscount">Bill-header discount, allocated pro-rata across the bill's sale lines.</param>
/// <param name="GoodReturnValue">Return lines with ReturnType = MarketResell (resaleable).</param>
/// <param name="MarketReturnValue">Return lines with ReturnType = Damage or Expire (write-offs).</param>
/// <param name="DbDiscount">Free-issue lines funded by the distributor.</param>
public record SalesSummarySalesAgg(
    int?    GroupKey,
    decimal SaleGross,
    decimal SaleQty,
    decimal ItemWiseDiscount,
    decimal BillDiscount,
    decimal GoodReturnValue,
    decimal GoodReturnQty,
    decimal MarketReturnValue,
    decimal MarketReturnQty,
    decimal DbDiscount);

/// <summary>
/// One grouped row of targets for a single calendar month. Kept at month grain because targets are
/// stored per (Year, Month) and the service pro-rates each month by the share of its days that the
/// requested range covers.
/// </summary>
/// <param name="TargetQtyPacks">Target quantity converted from cases to PACKS, to match BillingItem.Quantity.</param>
public record SalesSummaryTargetAgg(
    int?    GroupKey,
    int     Year,
    int     Month,
    decimal TargetQtyPacks,
    decimal TargetValue);

/// <summary>Display label for a group key. <c>Code</c> is empty for dimensions that have no code.</summary>
public record SalesSummaryLabel(string Code, string Name);

// ─── Response ────────────────────────────────────────────────────────────────────────────────

/// <summary>
/// One report row. Target columns are <c>null</c> — never <c>0</c> — when targets are unavailable,
/// because a zero renders as "0% achievement", which is a wrong answer rather than a missing one.
/// </summary>
public record SalesSummaryRowDto(
    int?     GroupKey,
    string   GroupCode,
    string   GroupName,
    decimal? TargetValue,
    decimal? TargetQty,
    decimal  GrossSaleValue,
    decimal  SaleQty,
    decimal  GoodReturn,
    decimal  GoodReturnQty,
    decimal  MarketReturn,
    decimal  MarketReturnQty,
    decimal  DbDiscount,
    decimal  Discount,
    decimal  NetSaleValue,
    decimal  NetSaleQty,
    decimal? AchievementPercent);

/// <summary>Grand totals across every group in the report — not just a page of them.</summary>
public record SalesSummaryTotalsDto(
    decimal? TargetValue,
    decimal? TargetQty,
    decimal  GrossSaleValue,
    decimal  SaleQty,
    decimal  GoodReturn,
    decimal  GoodReturnQty,
    decimal  MarketReturn,
    decimal  MarketReturnQty,
    decimal  DbDiscount,
    decimal  Discount,
    decimal  NetSaleValue,
    decimal  NetSaleQty,
    decimal? AchievementPercent);

/// <param name="From">Echoed back for the export title line only — there is no Date Range column.</param>
/// <param name="TargetsUnavailableReason">Human-readable cause, so the UI can explain the dashes.</param>
public record SalesSummaryResponseDto(
    SalesSummaryGroupBy GroupBy,
    DateOnly From,
    DateOnly To,
    bool     TargetsAvailable,
    string?  TargetsUnavailableReason,
    int      GroupCount,
    IReadOnlyList<SalesSummaryRowDto> Rows,
    SalesSummaryTotalsDto Totals);
