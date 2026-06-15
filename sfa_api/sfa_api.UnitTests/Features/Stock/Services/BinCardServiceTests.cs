using FluentAssertions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Features.Stock.Requests;
using sfa_api.Features.Stock.Services;

namespace sfa_api.UnitTests.Features.Stock.Services;

public class BinCardServiceTests
{
    private readonly Mock<IBinCardRepository> _repoMock = new();
    private readonly BinCardService _sut;

    private const int DistributorId = 70;
    private const int ProductId = 1;
    private static readonly DateOnly From = new(2026, 6, 1);
    private static readonly DateOnly To = new(2026, 6, 15);

    public BinCardServiceTests()
    {
        _sut = new BinCardService(_repoMock.Object);

        // Sensible empty defaults; individual tests override what they need.
        _repoMock.Setup(r => r.GetDistributorNameAsync(DistributorId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync("A & S Distributors");
        SetupMovements();
        SetupOpening();
        SetupRepReturns();
        SetupCounts();
        _repoMock.Setup(r => r.GetBinCardProductsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<BinCardProductInfo> { new(ProductId, "P1", "Product One", 10m) });
    }

    private BinCardQuery Query() => new(DistributorId, From, To);

    // ── Helpers to stub each repo call ──────────────────────────────────────

    private void SetupMovements(params BinCardMovementAgg[] moves) =>
        _repoMock.Setup(r => r.GetBinCardMovementsAsync(DistributorId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(moves.ToList());

    private void SetupOpening(decimal? opening = null) =>
        _repoMock.Setup(r => r.GetBinCardOpeningAsync(DistributorId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(opening.HasValue
                     ? new List<BinCardOpeningAgg> { new(ProductId, opening.Value) }
                     : new List<BinCardOpeningAgg>());

    private void SetupRepReturns(decimal? qty = null) =>
        _repoMock.Setup(r => r.GetBinCardRepReturnsAsync(DistributorId, From, To, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(qty.HasValue
                     ? new List<BinCardRepReturnAgg> { new(ProductId, qty.Value) }
                     : new List<BinCardRepReturnAgg>());

    private void SetupCounts(decimal? counted = null) =>
        _repoMock.Setup(r => r.GetBinCardLatestCountsAsync(DistributorId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(counted.HasValue
                     ? new List<BinCardCountAgg> { new(ProductId, counted.Value) }
                     : new List<BinCardCountAgg>());

    private static BinCardMovementAgg Move(
        StockTransactionType type, StockTransactionDirection dir, decimal qty,
        StockType pool = StockType.Normal) => new(ProductId, type, pool, dir, qty);

    // ── Tests ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBinCardAsync_MapsEveryColumn_AndComputesEndStock()
    {
        SetupOpening(50m);
        SetupRepReturns(6m);
        SetupCounts(120m);
        SetupMovements(
            Move(StockTransactionType.GRNReceipt,            StockTransactionDirection.In,  100m),
            Move(StockTransactionType.Sale,                  StockTransactionDirection.Out, 30m),
            Move(StockTransactionType.Return,                StockTransactionDirection.In,  5m),
            Move(StockTransactionType.BillingReversal,       StockTransactionDirection.In,  8m),
            Move(StockTransactionType.StockTakingAdjustment, StockTransactionDirection.In,  3m),
            Move(StockTransactionType.FreeIssue,             StockTransactionDirection.Out, 2m, StockType.Normal),
            Move(StockTransactionType.FreeIssue,             StockTransactionDirection.Out, 4m, StockType.FreeIssue));

        var result = await _sut.GetBinCardAsync(Query());

        result.DistributorName.Should().Be("A & S Distributors");
        result.RecordCount.Should().Be(1);
        var row = result.Rows.Single();

        row.ItemCode.Should().Be("P1");
        row.ItemPrice.Should().Be(10m);
        row.OpenStock.Should().Be(50m);
        row.InvoiceQuantity.Should().Be(100m);
        row.MarketResaleable.Should().Be(5m);
        row.DeletedInv.Should().Be(8m);
        row.StockAdjustment.Should().Be(3m);
        row.SoldQty.Should().Be(30m);
        row.FreeIssues.Should().Be(2m);
        row.CompanyFreeIssues.Should().Be(4m);
        row.RepReturnQtyDE.Should().Be(6m);

        // End = 50 + 100 + 5 + 8 + 3 − 30 − 2 − 4 = 130
        row.EndStock.Should().Be(130m);
        row.ClosingStockValue.Should().Be(1300m);     // 130 × 10
        row.CurrentStock.Should().Be(120m);
        row.StockVariance.Should().Be(-10m);          // 120 − 130
    }

    [Fact]
    public async Task GetBinCardAsync_NetsBillingReversalAndAdjustmentByDirection()
    {
        SetupOpening(0m);
        SetupMovements(
            Move(StockTransactionType.BillingReversal,       StockTransactionDirection.In,  10m),
            Move(StockTransactionType.BillingReversal,       StockTransactionDirection.Out, 4m),
            Move(StockTransactionType.StockTakingAdjustment, StockTransactionDirection.In,  2m),
            Move(StockTransactionType.StockTakingAdjustment, StockTransactionDirection.Out, 5m));

        var row = (await _sut.GetBinCardAsync(Query())).Rows.Single();

        row.DeletedInv.Should().Be(6m);        // 10 − 4
        row.StockAdjustment.Should().Be(-3m);  // 2 − 5
        row.EndStock.Should().Be(3m);          // 0 + 6 + (−3)
    }

    [Fact]
    public async Task GetBinCardAsync_EndStock_AlwaysReconcilesToOpenPlusInsMinusOuts()
    {
        SetupOpening(200m);
        SetupCounts(195m);
        SetupMovements(
            Move(StockTransactionType.GRNReceipt, StockTransactionDirection.In, 60m),
            Move(StockTransactionType.Sale,       StockTransactionDirection.Out, 80m),
            Move(StockTransactionType.FreeIssue,  StockTransactionDirection.Out, 5m, StockType.FreeIssue));

        var row = (await _sut.GetBinCardAsync(Query())).Rows.Single();

        var expectedEnd = row.OpenStock
            + row.InvoiceQuantity + row.MarketResaleable + row.DeletedInv + row.StockAdjustment
            - row.SoldQty - row.FreeIssues - row.CompanyFreeIssues;
        row.EndStock.Should().Be(expectedEnd);
        row.EndStock.Should().Be(175m);        // 200 + 60 − 80 − 5
        row.StockVariance.Should().Be(20m);    // 195 − 175
    }

    [Fact]
    public async Task GetBinCardAsync_CurrentStockAndVariance_AreNull_WhenNeverCounted()
    {
        SetupOpening(10m);
        SetupCounts(null); // no physical count
        SetupMovements(Move(StockTransactionType.Sale, StockTransactionDirection.Out, 4m));

        var row = (await _sut.GetBinCardAsync(Query())).Rows.Single();

        row.EndStock.Should().Be(6m);
        row.CurrentStock.Should().BeNull();
        row.StockVariance.Should().BeNull();
    }

    [Fact]
    public async Task GetBinCardAsync_TotalsAggregateAllRows()
    {
        // Two products. Override the product + movement stubs for a 2-row case.
        _repoMock.Setup(r => r.GetBinCardProductsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<BinCardProductInfo>
                 {
                     new(1, "P1", "Product One", 10m),
                     new(2, "P2", "Product Two", 20m),
                 });
        _repoMock.Setup(r => r.GetBinCardOpeningAsync(DistributorId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<BinCardOpeningAgg> { new(1, 100m), new(2, 50m) });
        _repoMock.Setup(r => r.GetBinCardMovementsAsync(DistributorId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<BinCardMovementAgg>
                 {
                     new(1, StockTransactionType.Sale, StockType.Normal, StockTransactionDirection.Out, 40m),
                     new(2, StockTransactionType.Sale, StockType.Normal, StockTransactionDirection.Out, 10m),
                 });

        var result = await _sut.GetBinCardAsync(Query());

        result.RecordCount.Should().Be(2);
        result.Totals.OpenStock.Should().Be(150m);      // 100 + 50
        result.Totals.SoldQty.Should().Be(50m);         // 40 + 10
        result.Totals.EndStock.Should().Be(100m);       // 60 + 40
        result.Totals.ClosingStockValue.Should().Be(60m * 10m + 40m * 20m); // 600 + 800 = 1400
        result.Rows.Select(r => r.ItemCode).Should().ContainInOrder("P1", "P2"); // ordered by code
    }

    [Fact]
    public async Task GetBinCardAsync_ReturnsEmpty_WhenNoActivity()
    {
        // all stubs already empty by default
        var result = await _sut.GetBinCardAsync(Query());

        result.RecordCount.Should().Be(0);
        result.Rows.Should().BeEmpty();
        result.Totals.EndStock.Should().Be(0m);
        result.Totals.ClosingStockValue.Should().Be(0m);
    }

    [Fact]
    public async Task GetBinCardAsync_Throws_WhenDistributorNotFound()
    {
        _repoMock.Setup(r => r.GetDistributorNameAsync(DistributorId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((string?)null);

        var act = () => _sut.GetBinCardAsync(Query());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
