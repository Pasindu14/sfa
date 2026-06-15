using sfa_api.Features.Stock.Enums;

namespace sfa_api.Features.Stock.DTOs;

// ── Public response DTOs ────────────────────────────────────────────────────

/// <summary>One bin-card line per SKU for a distributor over the selected date range.</summary>
public record BinCardRowDto(
    string   ItemCode,
    string   ItemDescription,
    decimal  ItemPrice,
    decimal  OpenStock,
    decimal  InvoiceQuantity,     // GRN receipts  (IN)
    decimal  MarketResaleable,    // MarketResell returns (IN)
    decimal  DeletedInv,          // net billing reversals (IN − OUT)
    decimal  StockAdjustment,     // net stock-take adjustments (IN − OUT)
    decimal  SoldQty,             // sales (OUT)
    decimal  FreeIssues,          // distributor-funded FOC (OUT, Normal pool)
    decimal  CompanyFreeIssues,   // company-funded FOC (OUT, FreeIssue pool)
    decimal  RepReturnQtyDE,      // Damage/Expire returns — informational, no stock effect
    decimal  EndStock,            // Open + ins − outs (== ledger closing balance)
    decimal? CurrentStock,        // latest physical count; null if never counted
    decimal  ClosingStockValue,   // EndStock × ItemPrice
    decimal? StockVariance        // CurrentStock − EndStock; null if never counted
);

/// <summary>Grand totals across all rows (quantity columns + closing value).</summary>
public record BinCardTotalsDto(
    decimal OpenStock,
    decimal InvoiceQuantity,
    decimal MarketResaleable,
    decimal DeletedInv,
    decimal StockAdjustment,
    decimal SoldQty,
    decimal FreeIssues,
    decimal CompanyFreeIssues,
    decimal RepReturnQtyDE,
    decimal EndStock,
    decimal ClosingStockValue
);

public record BinCardResponseDto(
    int                       DistributorId,
    string                    DistributorName,
    DateOnly                  From,
    DateOnly                  To,
    int                       RecordCount,
    IReadOnlyList<BinCardRowDto> Rows,
    BinCardTotalsDto          Totals
);

// ── Internal aggregate records (repository → service) ───────────────────────

/// <summary>SUM(Quantity) of in-range movements grouped by product, type, pool and direction.</summary>
public record BinCardMovementAgg(
    int                        ProductId,
    StockTransactionType       TransactionType,
    StockType                  StockType,
    StockTransactionDirection  Direction,
    decimal                    Quantity
);

/// <summary>Combined opening balance per product (summed across stock-type pools).</summary>
public record BinCardOpeningAgg(int ProductId, decimal OpeningQuantity);

/// <summary>Damage/Expire return quantity per product (from billing, informational).</summary>
public record BinCardRepReturnAgg(int ProductId, decimal Quantity);

/// <summary>Latest physical count per product (summed across stock-type pools).</summary>
public record BinCardCountAgg(int ProductId, decimal CountedQuantity);

/// <summary>Product metadata needed for the report.</summary>
public record BinCardProductInfo(int ProductId, string Code, string Description, decimal DealerPackPrice);
