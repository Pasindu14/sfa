using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Features.Stock.Services;
using static sfa_api.Features.Stock.Services.StockReconciliation;

namespace sfa_api.UnitTests.Features.Stock.Services;

public class StockReconciliationServiceTests
{
    private readonly Mock<IStockReconciliationRepository> _repoMock = new();
    private readonly StockReconciliationService _sut;

    private StockReconciliationRun? _savedRun;

    public StockReconciliationServiceTests()
    {
        // Capture the persisted run and assign it an Id, as the DB would.
        _repoMock.Setup(r => r.SaveRunAsync(It.IsAny<StockReconciliationRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockReconciliationRun run, CancellationToken _) =>
            {
                run.Id = 1;
                _savedRun = run;
                return run;
            });
        _repoMock.Setup(r => r.GetNamesAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<int> dIds, IEnumerable<int> pIds, CancellationToken _) =>
                (dIds.Distinct().ToDictionary(id => id, id => $"Dist {id}"),
                 pIds.Distinct().ToDictionary(id => id, id => $"P{id}")));

        _sut = new StockReconciliationService(_repoMock.Object, NullLogger<StockReconciliationService>.Instance);
    }

    private void SetupLedger(
        IEnumerable<LedgerNet> nets, IEnumerable<Snapshot> snaps, Dictionary<Key, decimal> onHand)
    {
        _repoMock.Setup(r => r.GetLedgerNetsAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(nets.ToList());
        _repoMock.Setup(r => r.GetLatestSnapshotsAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(snaps.ToList());
        _repoMock.Setup(r => r.GetOnHandAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(onHand);
    }

    [Fact]
    public async Task RunAsync_CleanLedger_PersistsRunWithZeroDiscrepancies()
    {
        SetupLedger(
            new[] { new LedgerNet(new Key(1, 5, StockType.Normal), 100m, 30m) },
            new[] { new Snapshot(new Key(1, 5, StockType.Normal), 70m) },
            new Dictionary<Key, decimal> { [new Key(1, 5, StockType.Normal)] = 70m });

        var result = await _sut.RunAsync(null, null, "nightly");

        result.DiscrepancyCount.Should().Be(0);
        result.GroupsChecked.Should().Be(1);
        result.Discrepancies.Should().BeEmpty();
        result.TriggeredBy.Should().Be("nightly");
        _savedRun.Should().NotBeNull();
        _savedRun!.DiscrepancyCount.Should().Be(0);
        _savedRun.Flags.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_Drift_PersistsFlagsAndEnrichesNames()
    {
        SetupLedger(
            new[] { new LedgerNet(new Key(3, 7, StockType.Normal), 100m, 30m) },   // expected 70
            new[] { new Snapshot(new Key(3, 7, StockType.Normal), 70m) },
            new Dictionary<Key, decimal> { [new Key(3, 7, StockType.Normal)] = 88m });   // drift

        var result = await _sut.RunAsync(null, null, "manual:9");

        result.DiscrepancyCount.Should().Be(1);
        var d = result.Discrepancies.Single();
        d.DistributorName.Should().Be("Dist 3");
        d.ProductCode.Should().Be("P7");
        d.Kind.Should().Be(nameof(StockDiscrepancyKind.LedgerSumVsBalance));
        d.Delta.Should().Be(18m);

        _savedRun!.Flags.Should().ContainSingle();
        _savedRun.Flags.First().ActualQuantity.Should().Be(88m);
        _savedRun.TriggeredBy.Should().Be("manual:9");
    }

    [Fact]
    public async Task GetLatestRunAsync_NoRuns_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetLatestRunAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockReconciliationRun?)null);

        (await _sut.GetLatestRunAsync()).Should().BeNull();
    }

    [Fact]
    public async Task GetLatestRunAsync_MapsPersistedRunToDto()
    {
        var run = new StockReconciliationRun
        {
            Id = 5,
            RunAt = new DateTime(2026, 6, 24, 3, 0, 0, DateTimeKind.Utc),
            TriggeredBy = "nightly",
            GroupsChecked = 12,
            DiscrepancyCount = 1,
            Flags = new List<StockReconciliationFlag>
            {
                new() { DistributorId = 2, ProductId = 4, StockType = StockType.Normal,
                        Kind = StockDiscrepancyKind.SnapshotVsLedgerSum,
                        ExpectedQuantity = 10m, ActualQuantity = 9m, Delta = -1m }
            }
        };
        _repoMock.Setup(r => r.GetLatestRunAsync(It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var result = await _sut.GetLatestRunAsync();

        result.Should().NotBeNull();
        result!.RunId.Should().Be(5);
        result.GroupsChecked.Should().Be(12);
        result.Discrepancies.Single().DistributorName.Should().Be("Dist 2");
        result.Discrepancies.Single().Delta.Should().Be(-1m);
    }
}
