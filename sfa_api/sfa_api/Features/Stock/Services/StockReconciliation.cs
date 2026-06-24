using sfa_api.Features.Stock.Enums;

namespace sfa_api.Features.Stock.Services;

/// <summary>
/// Pure, I/O-free reconciliation math (review finding #4). Given the ledger's net movements and
/// latest snapshots per (distributor, product, stock-type), plus the live recorded balances, it
/// returns every group where the two disagree. No EF, no clock, no logging — fully unit-testable.
///
/// Two ORTHOGONAL invariants are checked (different reference points, so they don't double-report the
/// same drift):
///   #1 LedgerSumVsBalance   — Σ(In) − Σ(Out) re-summed from movement quantities == live QuantityOnHand.
///                             The finding's core check: catches a balance that drifted from the ledger.
///   #2 SnapshotVsLedgerSum  — the ledger's own latest QuantityAfter == Σ(In) − Σ(Out). A ledger-internal
///                             integrity check: catches a mis-written running-balance snapshot even when
///                             the live balance coincidentally matches it. Independent of #1.
/// </summary>
public static class StockReconciliation
{
    public readonly record struct Key(int DistributorId, int ProductId, StockType StockType);

    /// <summary>Σ(In) / Σ(Out) of all ledger movements for a group.</summary>
    public record LedgerNet(Key Key, decimal TotalIn, decimal TotalOut)
    {
        public decimal Expected => TotalIn - TotalOut;
    }

    /// <summary>The QuantityAfter of the most-recent ledger row for a group.</summary>
    public record Snapshot(Key Key, decimal LatestQuantityAfter);

    public record Discrepancy(
        int DistributorId,
        int ProductId,
        StockType StockType,
        StockDiscrepancyKind Kind,
        decimal ExpectedQuantity,   // the trusted Σ(In) − Σ(Out)
        decimal ActualQuantity,     // observed: live balance (#1) or latest snapshot (#2)
        decimal Delta);             // ActualQuantity − ExpectedQuantity

    /// <summary>
    /// Compares ledger expectations against recorded on-hand. A key present in the ledger but missing
    /// a balance (or vice-versa) is treated as expected/recorded = 0, so an orphan on either side is
    /// flagged. <paramref name="tolerance"/> is normally 0 — quantities are exact <c>decimal</c>.
    /// </summary>
    public static List<Discrepancy> Reconcile(
        IEnumerable<LedgerNet> ledgerNets,
        IEnumerable<Snapshot> snapshots,
        IReadOnlyDictionary<Key, decimal> onHand,
        decimal tolerance = 0m)
    {
        var netByKey  = ledgerNets.ToDictionary(n => n.Key);
        var snapByKey = snapshots.ToDictionary(s => s.Key);

        // Every key that appears anywhere is a candidate — a balance with no ledger, or a ledger with
        // no balance, is itself a discrepancy worth surfacing.
        var keys = netByKey.Keys
            .Concat(snapByKey.Keys)
            .Concat(onHand.Keys)
            .ToHashSet();

        var results = new List<Discrepancy>();

        foreach (var key in keys)
        {
            // The trusted reference for BOTH checks: the independent re-sum of movement quantities.
            var ledgerSum = netByKey.TryGetValue(key, out var net) ? net.Expected : 0m;

            // Invariant #1 — re-summed ledger vs the live balance (the finding's core check).
            var recorded = onHand.TryGetValue(key, out var oh) ? oh : 0m;
            if (Math.Abs(recorded - ledgerSum) > tolerance)
                results.Add(new Discrepancy(
                    key.DistributorId, key.ProductId, key.StockType,
                    StockDiscrepancyKind.LedgerSumVsBalance,
                    ledgerSum, recorded, recorded - ledgerSum));

            // Invariant #2 — the ledger's own latest snapshot vs the re-sum (ledger-internal integrity).
            // Compared against ledgerSum (NOT the balance) so it stays orthogonal to #1: a balance-only
            // drift trips #1 alone, while a corrupt snapshot trips #2 regardless of the balance.
            if (snapByKey.TryGetValue(key, out var snap)
                && Math.Abs(snap.LatestQuantityAfter - ledgerSum) > tolerance)
                results.Add(new Discrepancy(
                    key.DistributorId, key.ProductId, key.StockType,
                    StockDiscrepancyKind.SnapshotVsLedgerSum,
                    ledgerSum, snap.LatestQuantityAfter, snap.LatestQuantityAfter - ledgerSum));
        }

        // Stable, deterministic ordering for reproducible runs and tests.
        return results
            .OrderBy(d => d.DistributorId)
            .ThenBy(d => d.ProductId)
            .ThenBy(d => d.StockType)
            .ThenBy(d => d.Kind)
            .ToList();
    }
}
