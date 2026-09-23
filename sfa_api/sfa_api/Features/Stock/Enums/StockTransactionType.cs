namespace sfa_api.Features.Stock.Enums;

public enum StockTransactionType
{
    GRNReceipt      = 0,
    Sale            = 1,
    FreeIssue       = 2,
    Return          = 3,
    Damage          = 4,
    Opening         = 5,
    BillingReversal      = 6,
    StockTakingAdjustment = 7,

    /// <summary>One-off ledger correction (e.g. backfilling a unit-conversion bug) — distinct
    /// from StockTakingAdjustment, which specifically means a physical stock-count variance.</summary>
    Correction = 8,

    /// <summary>Stock moved out of a closed distributor by an admin stock transfer (paired with TransferIn).</summary>
    TransferOut = 9,

    /// <summary>Stock moved into the receiving distributor by an admin stock transfer (paired with TransferOut).</summary>
    TransferIn = 10
}
