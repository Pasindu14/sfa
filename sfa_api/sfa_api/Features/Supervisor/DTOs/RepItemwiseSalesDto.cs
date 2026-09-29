namespace sfa_api.Features.Supervisor.DTOs;

/// <summary>
/// One rep's sales per product over a date range, for the supervisor Item-wise Sales page.
/// Same bill universe as <see cref="RepBillingSummaryDto"/>'s money figures: approved + pending
/// bills; rejected and cancelled bills are excluded. Quantities are packs.
/// </summary>
public record RepItemwiseSalesDto(
    DateOnly From,
    DateOnly To,
    decimal TotalSaleQty,
    decimal TotalFreeIssueQty,
    decimal TotalGoodReturnQty,
    decimal TotalMarketReturnQty,
    decimal TotalGrossValue,
    decimal TotalDiscount,
    decimal TotalGoodReturnValue,
    decimal TotalMarketReturnValue,
    decimal TotalNetValue,
    IReadOnlyList<RepItemwiseSalesItemDto> Items);

/// <param name="GrossValue">Sale value before any discount.</param>
/// <param name="Discount">Item-wise discount + this item's pro-rata share of the bill discount.</param>
/// <param name="NetValue">GrossValue − Discount − GoodReturnValue − MarketReturnValue.</param>
public record RepItemwiseSalesItemDto(
    int ProductId,
    string ItemCode,
    string ItemName,
    decimal SaleQty,
    decimal FreeIssueQty,
    decimal GoodReturnQty,
    decimal MarketReturnQty,
    decimal GrossValue,
    decimal Discount,
    decimal GoodReturnValue,
    decimal MarketReturnValue,
    decimal NetValue);

/// <summary>
/// Per-product line sums over the rep's live bills. Good return = MarketResell, market return =
/// Damage + Expire; DistributorReturn lines are in neither bucket (the sale line was already reduced).
/// </summary>
public record RepItemSalesAgg(
    int ProductId,
    decimal SaleQty,
    decimal FreeIssueQty,
    decimal GrossValue,
    decimal ItemDiscount,
    decimal BillDiscount,
    decimal GoodReturnQty,
    decimal GoodReturnValue,
    decimal MarketReturnQty,
    decimal MarketReturnValue);
