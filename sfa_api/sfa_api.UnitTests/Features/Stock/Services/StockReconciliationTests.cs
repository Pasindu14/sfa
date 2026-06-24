using FluentAssertions;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Services;
using static sfa_api.Features.Stock.Services.StockReconciliation;

namespace sfa_api.UnitTests.Features.Stock.Services;

/// <summary>
/// Pure reconciliation math (review finding #4). This is the heart of the feature, so it is tested
/// exhaustively here — the SQL aggregation it consumes can't run under the SQLite test provider.
///
/// Two ORTHOGONAL checks: #1 LedgerSumVsBalance (Σ vs live balance) and #2 SnapshotVsLedgerSum
/// (latest QuantityAfter vs Σ). They use different reference points, so a balance-only drift trips
/// only #1, and a corrupt snapshot trips only #2.
/// </summary>
public class StockReconciliationTests
{
    private static Key K(int d, int p, StockType st = StockType.Normal) => new(d, p, st);

    private static Dictionary<Key, decimal> OnHand(params (Key key, decimal qty)[] rows)
        => rows.ToDictionary(r => r.key, r => r.qty);

    [Fact]
    public void CleanLedger_MatchesBalance_NoDiscrepancies()
    {
        // Σ = 100 − 30 = 70; snapshot = 70; balance = 70 → both invariants hold.
        var nets = new[] { new LedgerNet(K(1, 5), 100m, 30m) };
        var snaps = new[] { new Snapshot(K(1, 5), 70m) };
        var onHand = OnHand((K(1, 5), 70m));

        Reconcile(nets, snaps, onHand).Should().BeEmpty();
    }

    [Fact]
    public void BalanceHigherThanLedgerSum_FlagsLedgerSumVsBalance_Only()
    {
        // Balance inflated by 5, but the ledger sum and its snapshot still agree → ONLY #1 fires.
        var nets = new[] { new LedgerNet(K(1, 5), 100m, 30m) };   // Σ = 70
        var snaps = new[] { new Snapshot(K(1, 5), 70m) };          // snapshot agrees with Σ
        var onHand = OnHand((K(1, 5), 75m));

        var result = Reconcile(nets, snaps, onHand);

        result.Should().ContainSingle();
        var d = result[0];
        d.Kind.Should().Be(StockDiscrepancyKind.LedgerSumVsBalance);
        d.ExpectedQuantity.Should().Be(70m);
        d.ActualQuantity.Should().Be(75m);
        d.Delta.Should().Be(5m);   // actual − expected, positive = balance too high
    }

    [Fact]
    public void BalanceLowerThanLedgerSum_FlagsNegativeDelta()
    {
        var nets = new[] { new LedgerNet(K(1, 5), 100m, 30m) };   // Σ = 70
        var snaps = new[] { new Snapshot(K(1, 5), 70m) };
        var onHand = OnHand((K(1, 5), 64m));

        Reconcile(nets, snaps, onHand).Should().ContainSingle()
            .Which.Delta.Should().Be(-6m);
    }

    [Fact]
    public void SnapshotDisagreesWithLedgerSum_FlagsSnapshotVsLedgerSum_Only()
    {
        // Σ = 50 and the balance matches Σ, but the ledger's latest snapshot is wrong → ONLY #2 fires.
        var nets = new[] { new LedgerNet(K(2, 9), 50m, 0m) };     // Σ = 50
        var snaps = new[] { new Snapshot(K(2, 9), 48m) };          // snapshot wrong
        var onHand = OnHand((K(2, 9), 50m));                       // balance fine

        var result = Reconcile(nets, snaps, onHand);

        result.Should().ContainSingle();
        result[0].Kind.Should().Be(StockDiscrepancyKind.SnapshotVsLedgerSum);
        result[0].ExpectedQuantity.Should().Be(50m);   // the trusted Σ
        result[0].ActualQuantity.Should().Be(48m);     // the wrong snapshot
        result[0].Delta.Should().Be(-2m);
    }

    [Fact]
    public void BalanceAndSnapshotBothWrong_EmitsBothFlags()
    {
        var nets = new[] { new LedgerNet(K(1, 1), 10m, 0m) };     // Σ = 10
        var snaps = new[] { new Snapshot(K(1, 1), 12m) };          // snapshot ≠ Σ → #2
        var onHand = OnHand((K(1, 1), 20m));                       // balance ≠ Σ → #1

        var result = Reconcile(nets, snaps, onHand);

        result.Should().HaveCount(2);
        result.Select(r => r.Kind).Should().BeEquivalentTo(new[]
        {
            StockDiscrepancyKind.LedgerSumVsBalance,
            StockDiscrepancyKind.SnapshotVsLedgerSum
        });
    }

    [Fact]
    public void StockTypePools_AreReconciledIndependently()
    {
        // Normal pool consistent; FreeIssue pool balance drifted → only the FreeIssue pool flags (#1).
        var nets = new[]
        {
            new LedgerNet(K(1, 5, StockType.Normal), 100m, 40m),     // 60
            new LedgerNet(K(1, 5, StockType.FreeIssue), 20m, 5m),    // 15
        };
        var snaps = new[]
        {
            new Snapshot(K(1, 5, StockType.Normal), 60m),
            new Snapshot(K(1, 5, StockType.FreeIssue), 15m),
        };
        var onHand = OnHand(
            (K(1, 5, StockType.Normal), 60m),
            (K(1, 5, StockType.FreeIssue), 99m));   // wrong

        var result = Reconcile(nets, snaps, onHand);

        result.Should().ContainSingle();
        result[0].StockType.Should().Be(StockType.FreeIssue);
        result[0].Kind.Should().Be(StockDiscrepancyKind.LedgerSumVsBalance);
    }

    [Fact]
    public void BalanceWithNoLedger_IsFlaggedAsExpectedZero()
    {
        // A stock row exists with quantity but the ledger has no movements for it → #1 (expected 0).
        var onHand = OnHand((K(7, 3), 12m));

        var result = Reconcile(Array.Empty<LedgerNet>(), Array.Empty<Snapshot>(), onHand);

        result.Should().ContainSingle();
        result[0].Kind.Should().Be(StockDiscrepancyKind.LedgerSumVsBalance);
        result[0].ExpectedQuantity.Should().Be(0m);
        result[0].ActualQuantity.Should().Be(12m);
    }

    [Fact]
    public void LedgerWithNoBalanceRow_FlagsLedgerSumVsBalance_ActualZero()
    {
        // Movements net to 25 with a matching snapshot, but there's no DistributorStock row.
        // #1 fires (balance 0 vs Σ 25). #2 does NOT (snapshot 25 == Σ 25).
        var nets = new[] { new LedgerNet(K(4, 8), 30m, 5m) };     // Σ = 25
        var snaps = new[] { new Snapshot(K(4, 8), 25m) };

        var result = Reconcile(nets, snaps, OnHand());

        result.Should().ContainSingle();
        result[0].Kind.Should().Be(StockDiscrepancyKind.LedgerSumVsBalance);
        result[0].ExpectedQuantity.Should().Be(25m);
        result[0].ActualQuantity.Should().Be(0m);
    }

    [Fact]
    public void ToleranceSuppressesSubThresholdNoise()
    {
        var nets = new[] { new LedgerNet(K(1, 1), 100.0000m, 0m) };
        var snaps = new[] { new Snapshot(K(1, 1), 100.0000m) };
        var onHand = OnHand((K(1, 1), 100.0001m));

        Reconcile(nets, snaps, onHand, tolerance: 0m).Should().NotBeEmpty();
        Reconcile(nets, snaps, onHand, tolerance: 0.001m).Should().BeEmpty();
    }

    [Fact]
    public void Results_AreDeterministicallyOrdered()
    {
        var nets = new[]
        {
            new LedgerNet(K(2, 1), 5m, 0m),
            new LedgerNet(K(1, 9), 5m, 0m),
            new LedgerNet(K(1, 2), 5m, 0m),
        };
        var onHand = OnHand((K(2, 1), 0m), (K(1, 9), 0m), (K(1, 2), 0m));   // all drift to expected 5

        var result = Reconcile(nets, Array.Empty<Snapshot>(), onHand);

        result.Select(r => (r.DistributorId, r.ProductId))
            .Should().ContainInOrder((1, 2), (1, 9), (2, 1));
    }
}
