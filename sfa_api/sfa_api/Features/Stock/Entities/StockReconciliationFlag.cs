using sfa_api.Features.Stock.Enums;

namespace sfa_api.Features.Stock.Entities;

/// <summary>
/// A single (distributor, product, stock-type) group whose ledger-derived expected on-hand
/// disagreed with the recorded <c>DistributorStock.QuantityOnHand</c>. One row per broken
/// invariant (a group can break both <see cref="StockDiscrepancyKind"/> checks). Append-only.
/// </summary>
public class StockReconciliationFlag
{
    public int Id { get; set; }

    public int RunId { get; set; }              // FK → StockReconciliationRun.Id

    public int DistributorId { get; set; }
    public int ProductId { get; set; }
    public StockType StockType { get; set; } = StockType.Normal;

    public StockDiscrepancyKind Kind { get; set; }

    /// <summary>The trusted reference: Σ(In) − Σ(Out) re-summed from the movement quantities.</summary>
    public decimal ExpectedQuantity { get; set; }

    /// <summary>What was observed — the live QuantityOnHand (LedgerSumVsBalance) or the latest
    /// snapshot QuantityAfter (SnapshotVsLedgerSum).</summary>
    public decimal ActualQuantity { get; set; }

    /// <summary>ActualQuantity − ExpectedQuantity (signed; positive = observed is higher than the re-sum).</summary>
    public decimal Delta { get; set; }

    // DistributorId / ProductId are stored as plain ids (no navigations) — these are append-only audit
    // rows; names are resolved on read via a lookup, avoiding FK coupling to filtered master data.
    public StockReconciliationRun Run { get; set; } = null!;
}
