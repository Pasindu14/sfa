namespace sfa_api.Features.Billings.DTOs;

/// <summary>
/// Internal aggregation row used by the item-wise achievement endpoint. All quantities are packs.
/// SaleQty / ReturnQty / FreeIssueQty / NetAmount come from distributor-APPROVED bills the rep has not
/// cancelled; PendingNetQty (sale minus return) from still-pending submitted bills.
/// </summary>
public record RepProductSalesRow(
    int ProductId,
    decimal SaleQty,
    decimal ReturnQty,
    decimal FreeIssueQty,
    decimal PendingNetQty,
    decimal NetAmount);
