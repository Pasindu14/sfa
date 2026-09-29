using FluentAssertions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Dashboard.DTOs;
using sfa_api.Features.Dashboard.Repositories;
using sfa_api.Features.Dashboard.Services;
using sfa_api.Features.Reports.DTOs;
using sfa_api.Features.Reports.Enums;
using sfa_api.Features.Reports.Requests;
using sfa_api.Features.Reports.Services;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.UnitTests.Features.Dashboard.Services;

/// <summary>
/// The dashboard's sales figures are the Sales Summary service's totals, and its repository pushes
/// decimal SUMs into SQL (not translatable on the SQLite test provider) — so the composition and
/// every derived number is asserted here against mocks.
/// </summary>
public class DashboardServiceTests
{
    private readonly Mock<IDashboardRepository> _repoMock = new();
    private readonly Mock<ISalesSummaryService> _salesMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly DashboardService _sut;

    // April has 30 days; the 10th leaves 20 days remaining.
    private static readonly DateOnly Day        = new(2026, 4, 10);
    private static readonly DateOnly MonthStart = new(2026, 4, 1);
    private static readonly DateOnly MonthEnd   = new(2026, 4, 30);

    public DashboardServiceTests()
    {
        _sut = new DashboardService(_repoMock.Object, _salesMock.Object, _cacheMock.Object);

        _cacheMock.Setup(c => c.GetAsync<DashboardSalesDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((DashboardSalesDto?)null);
        _cacheMock.Setup(c => c.GetAsync<DashboardActivityDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((DashboardActivityDto?)null);
        _cacheMock.Setup(c => c.GetAsync<DashboardTrendDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((DashboardTrendDto?)null);

        // Full month target 30,000 (1,000/day). North sold 6,000 MTD, South 3,000.
        SetupSummary(Day, Day, Totals(target: 1000m, net: 800m, discount: 40m, db: 10m, good: 25m, market: 15m),
            Row(1, "North", target: 600m, net: 500m), Row(2, "South", target: 400m, net: 300m));
        SetupSummary(MonthStart, Day, Totals(target: 10000m, net: 9000m, discount: 300m, db: 50m, good: 120m, market: 80m),
            Row(1, "North", target: 6000m, net: 6000m), Row(2, "South", target: 4000m, net: 3000m));
        SetupSummary(MonthStart, MonthEnd, Totals(target: 30000m, net: 9000m),
            Row(1, "North", target: 18000m, net: 6000m), Row(2, "South", target: 12000m, net: 3000m));

        _repoMock.Setup(r => r.CountRevenueBillsAsync(Day, Day, It.IsAny<CancellationToken>())).ReturnsAsync(12);
        _repoMock.Setup(r => r.CountRevenueBillsAsync(MonthStart, Day, It.IsAny<CancellationToken>())).ReturnsAsync(140);
        _repoMock.Setup(r => r.GetDailyRevenueAsync(MonthStart, Day, It.IsAny<CancellationToken>()))
                 .ReturnsAsync([new(new DateOnly(2026, 4, 1), 500m), new(new DateOnly(2026, 4, 3), 700m)]);
        _repoMock.Setup(r => r.CountActiveRepsAsync(Day, It.IsAny<CancellationToken>())).ReturnsAsync(18);
        _repoMock.Setup(r => r.CountSalesRepsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(24);
        _repoMock.Setup(r => r.GetOutletCountsAsync(Day, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new DashboardOutletCounts(Active: 800, Inactive: 200, NewOnDate: 3));
        _repoMock.Setup(r => r.CountBilledOutletsAsync(Day.AddDays(-44), Day, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(600);
    }

    // ── Stubs ─────────────────────────────────────────────────────────────────────────────────

    private void SetupSummary(DateOnly from, DateOnly to, SalesSummaryTotalsDto totals, params SalesSummaryRowDto[] rows) =>
        SetupSummary(SalesSummaryGroupBy.Region, from, to, totals, rows);

    private void SetupSummary(
        SalesSummaryGroupBy groupBy, DateOnly from, DateOnly to, SalesSummaryTotalsDto totals, params SalesSummaryRowDto[] rows) =>
        _salesMock.Setup(s => s.GetSalesSummaryAsync(
                      It.Is<SalesSummaryQuery>(q => q.From == from && q.To == to && q.GroupBy == groupBy),
                      It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new SalesSummaryResponseDto(
                      groupBy, from, to, true, null, rows.Length, rows, totals));

    /// <summary>Month-to-date product, rep and distributor rankings over a 10,000 month.</summary>
    private void SetupBreakdown()
    {
        var total = Totals(target: null, net: 10000m);
        SetupSummary(SalesSummaryGroupBy.Product, MonthStart, Day, total,
            Enumerable.Range(1, 10).Select(i => Row(i, $"P{i}", null, i * 100m)).ToArray());   // 100..1000
        SetupSummary(SalesSummaryGroupBy.SalesRep, MonthStart, Day, total,
            Row(1, "Nimal", 5000m, 6000m), Row(2, "Kamal", 5000m, 4000m));
        SetupSummary(SalesSummaryGroupBy.Distributor, MonthStart, Day, total,
            Enumerable.Range(1, 8).Select(i => Row(i, $"D{i}", null, i * 100m)).ToArray());    // 100..800
        _repoMock.Setup(r => r.CountLiveBillsAsync(MonthStart, Day, It.IsAny<CancellationToken>())).ReturnsAsync(60);
        _repoMock.Setup(r => r.GetNoSaleReasonsAsync(MonthStart, Day, It.IsAny<CancellationToken>()))
                 .ReturnsAsync([
                     new(sfa_api.Features.NotBillings.Enums.NotBillingReason.NoOrder, 10),
                     new(sfa_api.Features.NotBillings.Enums.NotBillingReason.OutletClosed, 30),
                 ]);
    }

    private static SalesSummaryTotalsDto Totals(
        decimal? target, decimal net, decimal discount = 0m, decimal db = 0m, decimal good = 0m, decimal market = 0m) =>
        new(target, null, net + 100m, 0m, good, 0m, market, 0m, db, discount, net, 0m,
            target is > 0m ? Math.Round(net / target.Value * 100m, 2) : null);

    private static SalesSummaryRowDto Row(int? key, string name, decimal? target, decimal net) =>
        new(key, string.Empty, name, target, null, net, 0m, 0m, 0m, 0m, 0m, 0m, 0m, net, 0m, null);

    // ── Sales section ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSalesAsync_TodayBlock_MirrorsTheOneDaySalesSummary()
    {
        var result = await _sut.GetSalesAsync(Day);

        result.Today.Revenue.Should().Be(800m);
        result.Today.TargetValue.Should().Be(1000m);
        result.Today.AchievementPercent.Should().Be(80m);
        result.Today.TotalDiscount.Should().Be(50m);   // 40 outlet discount + 10 distributor free issue
        result.Today.TotalReturn.Should().Be(40m);     // 25 good + 15 market
        result.Today.BillCount.Should().Be(12);
    }

    [Fact]
    public async Task GetSalesAsync_MonthToDateBlock_MirrorsTheMonthToDateSalesSummary()
    {
        var result = await _sut.GetSalesAsync(Day);

        result.MonthToDate.Revenue.Should().Be(9000m);
        result.MonthToDate.TotalDiscount.Should().Be(350m);
        result.MonthToDate.TotalReturn.Should().Be(200m);
        result.MonthToDate.BillCount.Should().Be(140);
        result.DaysInMonth.Should().Be(30);
        result.DaysElapsed.Should().Be(10);
    }

    [Fact]
    public async Task GetSalesAsync_NoTargetImported_ReportsNullNotZeroPercent()
    {
        SetupSummary(MonthStart, MonthEnd, Totals(target: 0m, net: 9000m));

        var result = await _sut.GetSalesAsync(Day);

        result.MonthTarget.TargetValue.Should().BeNull();
        result.MonthTarget.AchievementPercent.Should().BeNull();
    }

    [Fact]
    public async Task GetSalesAsync_MonthTarget_MeasuresMonthToDateAgainstTheFullMonth()
    {
        var result = await _sut.GetSalesAsync(Day);

        result.MonthTarget.TargetValue.Should().Be(30000m);
        result.MonthTarget.ExpectedToDate.Should().Be(10000m);        // 10 of 30 days
        result.MonthTarget.AchievementPercent.Should().Be(30m);       // 9,000 / 30,000
        result.MonthTarget.Balance.Should().Be(21000m);
        result.MonthTarget.RequiredDailyRate.Should().Be(1050m);      // 21,000 over 20 remaining days
    }

    [Fact]
    public async Task GetSalesAsync_Regions_PairMonthToDateRevenueWithFullMonthTarget()
    {
        var result = await _sut.GetSalesAsync(Day);

        result.Regions.Should().HaveCount(2);
        result.Regions[0].RegionName.Should().Be("North");
        result.Regions[0].MonthTarget.Should().Be(18000m);
        result.Regions[0].Revenue.Should().Be(6000m);
        result.Regions[0].AchievementPercent.Should().Be(33.33m);
    }

    [Fact]
    public async Task GetSalesAsync_DoesNotTouchActivityOrTrendQueries()
    {
        await _sut.GetSalesAsync(Day);

        _repoMock.Verify(r => r.GetDailyRevenueAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(r => r.GetOutletCountsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void BuildMonthTarget_LastDayOfMonth_HasNoRequiredRate()
    {
        var target = DashboardService.BuildMonthTarget(30000m, 31000m, 30, 30);

        target.Balance.Should().Be(-1000m);
        target.RequiredDailyRate.Should().BeNull();
    }

    [Fact]
    public void BuildMonthTarget_TargetAlreadyBeaten_RequiresNothingMore()
        => DashboardService.BuildMonthTarget(30000m, 31000m, 30, 10).RequiredDailyRate.Should().Be(0m);

    [Fact]
    public void BuildRegions_KeepsTheUnassignedBucketAndTargetOnlyRegions()
    {
        var rows = DashboardService.BuildRegions(
            [Row(null, "(Unassigned)", null, 50m)],
            [Row(3, "East", 900m, 0m)]);

        rows.Should().HaveCount(2);
        rows.Should().Contain(r => r.RegionId == null && r.Revenue == 50m && r.MonthTarget == null);
        rows.Should().Contain(r => r.RegionId == 3 && r.Revenue == 0m && r.AchievementPercent == 0m);
    }

    // ── Activity section ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetActivityAsync_Reps_ReportsActiveShare()
    {
        var result = await _sut.GetActivityAsync(Day);

        result.Reps.ActiveToday.Should().Be(18);
        result.Reps.TotalReps.Should().Be(24);
        result.Reps.ActivePercent.Should().Be(75m);
    }

    [Fact]
    public async Task GetActivityAsync_Outlets_SplitsCustomersAndMeasuresCoverageAgainstActiveOutlets()
    {
        var result = await _sut.GetActivityAsync(Day);

        result.Outlets.TotalCustomers.Should().Be(1000);
        result.Outlets.ActivePercent.Should().Be(80m);
        result.Outlets.InactivePercent.Should().Be(20m);
        result.Outlets.BilledLast45Days.Should().Be(600);
        result.Outlets.BilledLast45DaysPercent.Should().Be(75m);   // 600 of 800 active
        result.Outlets.BilledWindowFrom.Should().Be(new DateOnly(2026, 2, 25));  // 45 days inclusive of Apr 10
        result.Outlets.NewToday.Should().Be(3);
    }

    [Fact]
    public async Task GetActivityAsync_NeverRunsTheSalesSummary()
    {
        await _sut.GetActivityAsync(Day);

        _salesMock.Invocations.Should().BeEmpty();
    }

    [Fact]
    public void BuildOutlets_NoOutlets_ReturnsNullPercentages()
    {
        var outlets = DashboardService.BuildOutlets(new DashboardOutletCounts(0, 0, 0), 0, Day);

        outlets.ActivePercent.Should().BeNull();
        outlets.InactivePercent.Should().BeNull();
        outlets.BilledLast45DaysPercent.Should().BeNull();
    }

    // ── Trend section ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTrendAsync_ZeroFillsEveryDayAndAccumulates()
    {
        var result = await _sut.GetTrendAsync(Day);

        result.DaysInMonth.Should().Be(30);
        result.Points.Should().HaveCount(10);
        result.Points[1].Revenue.Should().Be(0m);              // Apr 2 had no sales
        result.Points[2].CumulativeRevenue.Should().Be(1200m); // 500 + 0 + 700
        result.Points[9].CumulativeRevenue.Should().Be(1200m);
    }

    [Fact]
    public async Task GetTrendAsync_NeverRunsTheSalesSummary()
    {
        await _sut.GetTrendAsync(Day);

        _salesMock.Invocations.Should().BeEmpty();
    }

    // ── Breakdown section ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBreakdownAsync_Products_TopEightByRevenueWithShareOfTheMonth()
    {
        SetupBreakdown();

        var result = await _sut.GetBreakdownAsync(Day);

        result.Products.Should().HaveCount(DashboardService.TopProducts);
        result.Products[0].Name.Should().Be("P10");
        result.Products[0].Revenue.Should().Be(1000m);
        result.Products[0].SharePercent.Should().Be(10m);
        result.Products.Select(p => p.Revenue).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetBreakdownAsync_Reps_CarryTheirPaceTarget()
    {
        SetupBreakdown();

        var result = await _sut.GetBreakdownAsync(Day);

        result.Reps[0].Name.Should().Be("Nimal");
        result.Reps[0].TargetValue.Should().Be(5000m);
        result.Reps[0].Revenue.Should().Be(6000m);
    }

    [Fact]
    public async Task GetBreakdownAsync_Distributors_RollTheTailIntoOthers()
    {
        SetupBreakdown();

        var result = await _sut.GetBreakdownAsync(Day);

        result.Distributors.Should().HaveCount(DashboardService.TopDistributors);
        result.OtherDistributors.Should().NotBeNull();
        result.OtherDistributors!.Count.Should().Be(2);          // D1 + D2
        result.OtherDistributors.Revenue.Should().Be(300m);      // 100 + 200
        result.OtherDistributors.SharePercent.Should().Be(3m);
    }

    [Fact]
    public async Task GetBreakdownAsync_Visits_SaleShareAndReasonsLargestFirst()
    {
        SetupBreakdown();

        var result = await _sut.GetBreakdownAsync(Day);

        result.Visits.SaleVisits.Should().Be(60);
        result.Visits.NoSaleVisits.Should().Be(40);
        result.Visits.SalePercent.Should().Be(60m);
        result.Visits.Reasons.Select(r => r.Reason).Should().Equal("OutletClosed", "NoOrder");
        result.Visits.Reasons[0].SharePercent.Should().Be(75m);
    }

    [Fact]
    public void Rank_LeavesOutRowsThatEarnedNothing()
    {
        var ranked = DashboardService.Rank(
            [Row(1, "Sold", null, 500m), Row(2, "Returned more than sold", null, -626m), Row(3, "Nothing", null, 0m)],
            total: 500m, take: 8);

        ranked.Select(r => r.Name).Should().Equal("Sold");
    }

    [Fact]
    public void Others_NothingLeftOver_ReturnsNull()
        => DashboardService.Others([Row(1, "D1", null, 100m)], 6, 100m).Should().BeNull();

    [Fact]
    public void BuildVisits_NoVisitsAtAll_HasNoPercentage()
        => DashboardService.BuildVisits(0, []).SalePercent.Should().BeNull();

    // ── Guards and caching ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EverySection_FutureDate_Throws()
    {
        var tomorrow = SriLankaTime.Today.AddDays(1);

        (await FluentActions.Awaiting(() => _sut.GetSalesAsync(tomorrow)).Should().ThrowAsync<BusinessRuleException>())
            .Which.ErrorCode.Should().Be("DASHBOARD_FUTURE_DATE");
        await FluentActions.Awaiting(() => _sut.GetActivityAsync(tomorrow)).Should().ThrowAsync<BusinessRuleException>();
        await FluentActions.Awaiting(() => _sut.GetTrendAsync(tomorrow)).Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task GetSalesAsync_CacheHit_SkipsEveryQuery()
    {
        var cached = await _sut.GetSalesAsync(Day);
        _cacheMock.Setup(c => c.GetAsync<DashboardSalesDto>("dashboard:sales:" + Day.DayNumber, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(cached);
        _salesMock.Invocations.Clear();
        _repoMock.Invocations.Clear();

        var result = await _sut.GetSalesAsync(Day);

        result.Should().BeSameAs(cached);
        _salesMock.Invocations.Should().BeEmpty();
        _repoMock.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task Sections_UseSeparateCacheKeys()
    {
        await _sut.GetSalesAsync(Day);
        await _sut.GetActivityAsync(Day);
        await _sut.GetTrendAsync(Day);

        _cacheMock.Verify(c => c.SetAsync("dashboard:sales:" + Day.DayNumber, It.IsAny<DashboardSalesDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.SetAsync("dashboard:activity:" + Day.DayNumber, It.IsAny<DashboardActivityDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.SetAsync("dashboard:trend:" + Day.DayNumber, It.IsAny<DashboardTrendDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
