using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.Reports.DTOs;
using sfa_api.Features.Reports.Enums;
using sfa_api.Features.Reports.Repositories;
using sfa_api.Features.Reports.Requests;
using sfa_api.Features.Reports.Services;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.UnitTests.Features.Reports.Services;

/// <summary>
/// The repository pushes decimal SUMs into SQL, which the SQLite test provider cannot translate, so
/// the data path is untestable in CI (see SalesSummaryApiTests). Every column formula therefore
/// lives in the service and is asserted here against a mocked repository.
/// </summary>
public class SalesSummaryServiceTests
{
    private readonly Mock<ISalesSummaryRepository> _repoMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly SalesSummaryService _sut;

    private const int RepId = 42;
    private static readonly DateOnly From = new(2026, 4, 1);
    private static readonly DateOnly To = new(2026, 4, 30);   // a whole month ⇒ proration factor 1

    public SalesSummaryServiceTests()
    {
        _sut = new SalesSummaryService(
            _repoMock.Object, _cacheMock.Object, Mock.Of<ILogger<SalesSummaryService>>());

        // Always a cache miss unless a test says otherwise.
        _cacheMock.Setup(c => c.GetAsync<SalesSummaryResponseDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((SalesSummaryResponseDto?)null);

        SetupSales();
        SetupTargets();
        SetupLabels((RepId, new SalesSummaryLabel(string.Empty, "Nimal Perera")));
    }

    // ── Stubs ─────────────────────────────────────────────────────────────────────────────────

    private void SetupSales(params SalesSummarySalesAgg[] rows) =>
        _repoMock.Setup(r => r.GetSalesAggregatesAsync(
                     It.IsAny<SalesSummaryQuery>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(rows.ToList());

    private void SetupTargets(params SalesSummaryTargetAgg[] rows) =>
        _repoMock.Setup(r => r.GetTargetAggregatesAsync(
                     It.IsAny<SalesSummaryQuery>(), It.IsAny<IReadOnlyList<(int, int)>>(),
                     It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(rows.ToList());

    private void SetupLabels(params (int Id, SalesSummaryLabel Label)[] labels) =>
        _repoMock.Setup(r => r.GetLabelsAsync(
                     It.IsAny<SalesSummaryGroupBy>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(labels.ToDictionary(x => x.Id, x => x.Label));

    private static SalesSummaryQuery Query(
        SalesSummaryGroupBy groupBy = SalesSummaryGroupBy.SalesRep,
        int? routeId = null,
        DateOnly? from = null,
        DateOnly? to = null)
        => new(groupBy, from ?? From, to ?? To, RouteId: routeId);

    /// <summary>
    /// A fully-populated sales row. Gross 1000, item discount 50, allocated bill discount 30,
    /// good return 100 (60 packs), market return 40 (20 packs), DB free issue 25, 500 packs sold.
    /// </summary>
    private static SalesSummarySalesAgg Sales(int? key = RepId) =>
        new(key,
            SaleGross:         1000m,
            SaleQty:            500m,
            ItemWiseDiscount:    50m,
            BillDiscount:        30m,
            GoodReturnValue:    100m,
            GoodReturnQty:       60m,
            MarketReturnValue:   40m,
            MarketReturnQty:     20m,
            DbDiscount:          25m);

    // ── Column math ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GrossSaleValue_NetsOffGoodReturn_ButNotMarketReturn()
    {
        SetupSales(Sales());

        var result = await _sut.GetSalesSummaryAsync(Query());

        // 1000 gross − 100 good return. Market return (40) is deducted later, at the net line.
        result.Rows.Single().GrossSaleValue.Should().Be(900m);
    }

    [Fact]
    public async Task Discount_CombinesItemWiseAndBillLevel()
    {
        SetupSales(Sales());

        var result = await _sut.GetSalesSummaryAsync(Query());

        result.Rows.Single().Discount.Should().Be(80m);   // 50 item-wise + 30 bill-level
    }

    [Fact]
    public async Task NetSaleValue_SubtractsMarketReturnAndBothDiscounts()
    {
        SetupSales(Sales());

        var result = await _sut.GetSalesSummaryAsync(Query());

        // 900 gross − 40 market return − (25 DB discount + 80 discount)
        result.Rows.Single().NetSaleValue.Should().Be(755m);
    }

    [Fact]
    public async Task NetSaleQty_SubtractsGoodReturnOnly_NotMarketReturn()
    {
        // Deliberate asymmetry from the report spec: damage/expiry stock never re-enters saleable
        // inventory, so its quantity is never deducted. This test exists so nobody "fixes" it.
        SetupSales(Sales());

        var result = await _sut.GetSalesSummaryAsync(Query());

        result.Rows.Single().NetSaleQty.Should().Be(440m);   // 500 − 60, NOT 500 − 60 − 20
    }

    [Fact]
    public async Task SaleQty_IsGross_AndDoesNotDeductReturns()
    {
        SetupSales(Sales());

        var result = await _sut.GetSalesSummaryAsync(Query());

        result.Rows.Single().SaleQty.Should().Be(500m);
    }

    [Fact]
    public async Task ReturnsAndDbDiscount_ArePassedThroughUnchanged()
    {
        SetupSales(Sales());

        var row = (await _sut.GetSalesSummaryAsync(Query())).Rows.Single();

        row.GoodReturn.Should().Be(100m);
        row.GoodReturnQty.Should().Be(60m);
        row.MarketReturn.Should().Be(40m);
        row.MarketReturnQty.Should().Be(20m);
        row.DbDiscount.Should().Be(25m);
    }

    /// <summary>
    /// Substituting Billing.TotalAmount = SubTotal − BillDiscount − GoodReturn (BillingService.cs:250)
    /// into the column formulas gives an identity that holds for any input, which is the strongest
    /// single check that the arithmetic reconciles with what the write path actually stored.
    /// </summary>
    [Fact]
    public async Task NetSaleValue_MatchesTheBillHeaderIdentity()
    {
        var s = Sales();
        SetupSales(s);

        var row = (await _sut.GetSalesSummaryAsync(Query())).Rows.Single();

        var subTotal    = s.SaleGross - s.ItemWiseDiscount;                       // what SubTotalAmount stores
        var totalAmount = subTotal - s.BillDiscount - s.GoodReturnValue;          // BillingService.cs:250
        var expected    = totalAmount - s.MarketReturnValue - s.DbDiscount;

        row.NetSaleValue.Should().Be(expected);
    }

    // ── Targets & proration ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Targets_ForAWholeMonth_AreNotProrated()
    {
        SetupSales(Sales());
        SetupTargets(new SalesSummaryTargetAgg(RepId, 2026, 4, TargetQtyPacks: 600m, TargetValue: 1200m));

        var row = (await _sut.GetSalesSummaryAsync(Query())).Rows.Single();

        row.TargetQty.Should().Be(600m);
        row.TargetValue.Should().Be(1200m);
    }

    [Fact]
    public async Task Targets_ForAPartialMonth_AreProratedByDays()
    {
        SetupSales(Sales());
        SetupTargets(new SalesSummaryTargetAgg(RepId, 2026, 4, 600m, 1200m));

        // 1..15 April = 15 of 30 days ⇒ half the month's target.
        var q = Query(from: new DateOnly(2026, 4, 1), to: new DateOnly(2026, 4, 15));
        var row = (await _sut.GetSalesSummaryAsync(q)).Rows.Single();

        row.TargetQty.Should().Be(300m);
        row.TargetValue.Should().Be(600m);
    }

    [Fact]
    public async Task Targets_SpanningTwoMonths_SumTheWeightedSlices()
    {
        SetupSales(Sales());
        SetupTargets(
            new SalesSummaryTargetAgg(RepId, 2026, 4, 300m, 3000m),
            new SalesSummaryTargetAgg(RepId, 2026, 5, 310m, 3100m));

        // 15 Apr .. 14 May ⇒ April × 16/30 + May × 14/31.
        var q = Query(from: new DateOnly(2026, 4, 15), to: new DateOnly(2026, 5, 14));
        var row = (await _sut.GetSalesSummaryAsync(q)).Rows.Single();

        row.TargetQty.Should().Be(300m * 16m / 30m + 310m * 14m / 31m);
    }

    [Theory]
    [InlineData(SalesSummaryGroupBy.Route)]
    [InlineData(SalesSummaryGroupBy.Outlet)]
    public async Task Targets_AreNull_NotZero_WhenGroupingHasNoTargets(SalesSummaryGroupBy groupBy)
    {
        // A zero would render as "0% achievement" — a wrong answer, not a missing one.
        SetupSales(Sales());
        SetupLabels((RepId, new SalesSummaryLabel(string.Empty, "Route 7")));

        var result = await _sut.GetSalesSummaryAsync(Query(groupBy));

        result.TargetsAvailable.Should().BeFalse();
        result.TargetsUnavailableReason.Should().NotBeNullOrWhiteSpace();
        result.Rows.Single().TargetValue.Should().BeNull();
        result.Rows.Single().TargetQty.Should().BeNull();
        result.Totals.TargetValue.Should().BeNull();

        // And we should not have paid for a query that cannot return anything.
        _repoMock.Verify(r => r.GetTargetAggregatesAsync(
            It.IsAny<SalesSummaryQuery>(), It.IsAny<IReadOnlyList<(int, int)>>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Targets_AreSuppressed_WhenFilteringByRoute()
    {
        // SalesTarget has no RouteId, so a route-filtered sales figure would sit beside the rep's
        // company-wide target — a wrong number that looks plausible. Subtle; regresses easily.
        SetupSales(Sales());

        var result = await _sut.GetSalesSummaryAsync(Query(SalesSummaryGroupBy.SalesRep, routeId: 7));

        result.TargetsAvailable.Should().BeFalse();
        result.Rows.Single().TargetValue.Should().BeNull();
        _repoMock.Verify(r => r.GetTargetAggregatesAsync(
            It.IsAny<SalesSummaryQuery>(), It.IsAny<IReadOnlyList<(int, int)>>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AchievementPercent_IsNull_WhenTargetIsZero()
    {
        SetupSales(Sales());
        SetupTargets();   // no target rows ⇒ target 0

        var row = (await _sut.GetSalesSummaryAsync(Query())).Rows.Single();

        row.TargetValue.Should().Be(0m);
        row.AchievementPercent.Should().BeNull();   // a percentage of nothing is not 0%
    }

    [Fact]
    public async Task AchievementPercent_IsNetOverTarget()
    {
        SetupSales(Sales());
        SetupTargets(new SalesSummaryTargetAgg(RepId, 2026, 4, 600m, 1510m));

        var row = (await _sut.GetSalesSummaryAsync(Query())).Rows.Single();

        row.AchievementPercent.Should().Be(50m);   // 755 / 1510
    }

    // ── Merge behaviour ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GroupWithTargetButNoSales_StillProducesARow()
    {
        // The 0%-achievement line is the single most important row in the report.
        SetupSales();
        SetupTargets(new SalesSummaryTargetAgg(RepId, 2026, 4, 600m, 1200m));

        var result = await _sut.GetSalesSummaryAsync(Query());

        var row = result.Rows.Single();
        row.GroupName.Should().Be("Nimal Perera");
        row.TargetValue.Should().Be(1200m);
        row.NetSaleValue.Should().Be(0m);
        row.AchievementPercent.Should().Be(0m);
    }

    [Fact]
    public async Task GroupWithSalesButNoTarget_StillProducesARow()
    {
        SetupSales(Sales());
        SetupTargets();

        var result = await _sut.GetSalesSummaryAsync(Query());

        result.Rows.Should().ContainSingle().Which.NetSaleValue.Should().Be(755m);
    }

    [Fact]
    public async Task NullGroupKey_BecomesAnUnassignedBucket_AndIsKept()
    {
        // Billing.TerritoryId and friends are nullable. Dropping the null bucket would stop the
        // report summing to the company total.
        SetupSales(Sales(key: null), Sales());

        var result = await _sut.GetSalesSummaryAsync(Query(SalesSummaryGroupBy.Territory));

        result.Rows.Should().HaveCount(2);
        result.Rows.Should().ContainSingle(r => r.GroupName == "(Unassigned)");
        result.Totals.NetSaleValue.Should().Be(755m * 2);
    }

    [Fact]
    public async Task MissingLabel_FallsBackToTheId()
    {
        SetupSales(Sales());
        SetupLabels();   // no labels at all

        var result = await _sut.GetSalesSummaryAsync(Query());

        result.Rows.Single().GroupName.Should().Be($"#{RepId}");
    }

    // ── Totals & ordering ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Totals_SumEveryRow()
    {
        SetupSales(Sales(key: 1), Sales(key: 2));
        SetupTargets(
            new SalesSummaryTargetAgg(1, 2026, 4, 600m, 1200m),
            new SalesSummaryTargetAgg(2, 2026, 4, 400m, 800m));
        SetupLabels((1, new SalesSummaryLabel("", "A")), (2, new SalesSummaryLabel("", "B")));

        var result = await _sut.GetSalesSummaryAsync(Query());

        result.GroupCount.Should().Be(2);
        result.Totals.GrossSaleValue.Should().Be(1800m);
        result.Totals.NetSaleValue.Should().Be(1510m);
        result.Totals.TargetValue.Should().Be(2000m);
        result.Totals.SaleQty.Should().Be(1000m);
        result.Totals.NetSaleQty.Should().Be(880m);
    }

    [Fact]
    public async Task Rows_AreOrderedByNetSaleValueDescending()
    {
        SetupSales(
            Sales(key: 1) with { SaleGross = 500m },
            Sales(key: 2) with { SaleGross = 2000m });
        SetupLabels((1, new SalesSummaryLabel("", "Small")), (2, new SalesSummaryLabel("", "Big")));

        var result = await _sut.GetSalesSummaryAsync(Query());

        result.Rows.Select(r => r.GroupName).Should().ContainInOrder("Big", "Small");
    }

    // ── Guards & caching ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TooManyGroups_ThrowsBusinessRule()
    {
        SetupSales([.. Enumerable.Range(1, 5001).Select(i => Sales(key: i))]);

        var act = () => _sut.GetSalesSummaryAsync(Query(SalesSummaryGroupBy.Outlet));

        (await act.Should().ThrowAsync<BusinessRuleException>())
            .Which.ErrorCode.Should().Be("REPORT_TOO_MANY_GROUPS");
    }

    [Fact]
    public async Task CacheHit_ShortCircuitsTheRepository()
    {
        var cached = new SalesSummaryResponseDto(
            SalesSummaryGroupBy.SalesRep, From, To, true, null, 0, [],
            new SalesSummaryTotalsDto(0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, null));

        _cacheMock.Setup(c => c.GetAsync<SalesSummaryResponseDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(cached);

        var result = await _sut.GetSalesSummaryAsync(Query());

        result.Should().BeSameAs(cached);
        _repoMock.Verify(r => r.GetSalesAggregatesAsync(
            It.IsAny<SalesSummaryQuery>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OrgLevelFilters_ReachTheRepository()
    {
        SetupSales(Sales());
        var q = new SalesSummaryQuery(
            SalesSummaryGroupBy.SalesRep, From, To, AsmId: 5, RsmId: 6, NsmId: 7);

        await _sut.GetSalesSummaryAsync(q);

        _repoMock.Verify(r => r.GetSalesAggregatesAsync(
            It.Is<SalesSummaryQuery>(x => x.AsmId == 5 && x.RsmId == 6 && x.NsmId == 7),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OrgLevelFilters_DoNotSuppressTargets()
    {
        // Unlike RouteId, SalesTarget carries AsmUserId/RsmUserId/NsmUserId, so these filters must
        // still return targets. Guards against someone copying the RouteId suppression rule.
        SetupSales(Sales());
        SetupTargets(new SalesSummaryTargetAgg(RepId, 2026, 4, 600m, 1200m));

        var result = await _sut.GetSalesSummaryAsync(
            new SalesSummaryQuery(SalesSummaryGroupBy.SalesRep, From, To, AsmId: 5));

        result.TargetsAvailable.Should().BeTrue();
        result.Rows.Single().TargetValue.Should().Be(1200m);
    }

    [Fact]
    public async Task CacheKey_DistinguishesEveryFilter()
    {
        // A parameter missing from the cache key makes two different requests share a result —
        // one filter silently showing another's numbers. Assert each filter yields a distinct key.
        SetupSales(Sales());
        var keys = new List<string>();
        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<SalesSummaryResponseDto>(),
                                         It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                  .Callback<string, SalesSummaryResponseDto, TimeSpan, CancellationToken>(
                      (k, _, _, _) => keys.Add(k));

        var baseQ = new SalesSummaryQuery(SalesSummaryGroupBy.SalesRep, From, To);
        foreach (var q in new[]
        {
            baseQ,
            baseQ with { RegionId = 1 },   baseQ with { AreaId = 1 },
            baseQ with { TerritoryId = 1 }, baseQ with { DivisionId = 1 },
            baseQ with { RouteId = 1 },     baseQ with { DistributorId = 1 },
            baseQ with { SalesRepId = 1 },  baseQ with { SupervisorId = 1 },
            baseQ with { AsmId = 1 },       baseQ with { RsmId = 1 },
            baseQ with { NsmId = 1 },       baseQ with { ProductId = 1 },
        })
            await _sut.GetSalesSummaryAsync(q);

        keys.Should().OnlyHaveUniqueItems();
        keys.Should().HaveCount(13);
    }

    [Fact]
    public async Task Response_EchoesTheRequestedRangeAndGrouping()
    {
        SetupSales(Sales());

        var result = await _sut.GetSalesSummaryAsync(Query(SalesSummaryGroupBy.Territory));

        result.From.Should().Be(From);
        result.To.Should().Be(To);
        result.GroupBy.Should().Be(SalesSummaryGroupBy.Territory);
    }
}
