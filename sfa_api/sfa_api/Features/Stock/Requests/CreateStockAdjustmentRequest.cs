using sfa_api.Features.Stock.Enums;

namespace sfa_api.Features.Stock.Requests;

/// <summary>
/// POST /api/v1/stock-adjustments — Reason binds from "Damage" / "Expiry" / "CountCorrection" /
/// "DataEntryError" / "Other"; StockType from "Normal" / "FreeIssue".
/// </summary>
public record CreateStockAdjustmentRequest(
    int DistributorId,
    StockAdjustmentReason Reason,
    string? Notes,
    List<CreateStockAdjustmentLineRequest> Lines);

/// <summary>
/// Quantities are in pieces. ExpectedQuantity is the balance the admin saw (0 for a product with no
/// stock row yet); the adjustment is rejected with STOCK_CHANGED if the live balance differs.
/// </summary>
public record CreateStockAdjustmentLineRequest(
    int ProductId,
    StockType StockType,
    decimal ExpectedQuantity,
    decimal NewQuantity);
