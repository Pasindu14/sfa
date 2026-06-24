namespace sfa_api.Features.Stock.Enums;

/// <summary>
/// Which self-consistency invariant a reconciliation flag violated (review finding #4).
/// </summary>
public enum StockDiscrepancyKind
{
    /// <summary>Σ(In.Quantity) − Σ(Out.Quantity) over the whole ledger ≠ DistributorStock.QuantityOnHand.
    /// The check finding #4 asks for: an independent re-sum vs the live balance. Catches a balance that
    /// drifted from the true movement total.</summary>
    LedgerSumVsBalance   = 0,

    /// <summary>The latest StockTransaction.QuantityAfter (the ledger's own recorded running balance)
    /// ≠ Σ(In) − Σ(Out). A ledger-internal integrity check, independent of the live balance — catches a
    /// mis-written snapshot / broken running-balance chain even when the live balance happens to match it.</summary>
    SnapshotVsLedgerSum  = 1
}
