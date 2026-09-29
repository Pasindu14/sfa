using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using sfa_api.Features.Reports.DTOs;
using sfa_api.Features.Reports.Enums;
using sfa_api.Features.Reports.Repositories;
using sfa_api.Features.Reports.Requests;
using sfa_api.Features.Reports.Services;
using sfa_api.Features.Reports.Validators;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.UnitTests.Features.Reports.Services;

/// <summary>
/// Grouping by several dimensions at once (e.g. Sales Rep → Territory → Distributor): one row per
/// combination, one labelled cell per dimension, targets matched on the full combination.
/// </summary>
public class SalesSummaryMultiGroupTests
{
    private readonly Mock<ISalesSummaryRepository> _repo = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly SalesSummaryService _sut;

    private static readonly DateOnly From = new(2026, 4, 1);
    private static readonly DateOnly To = new(2026, 4, 30);   // whole month ⇒ proration factor 1

    private static readonly SalesSummaryGroupBy[] RepTerritoryDistributor =
        [SalesSummaryGroupBy.SalesRep, SalesSummaryGroupBy.Territory, SalesSummaryGroupBy.Distributor];

    public SalesSummaryMultiGroupTests()
    {
        _sut = new SalesSummaryService(_repo.Object, _cache.Object, Mock.Of<ILogger<SalesSummaryService>>());

        _cache.Setup(c => c.GetAsync<SalesSummaryResponseDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((SalesSummaryResponseDto?)null);

        SetupTargets();
        Labels(SalesSummaryGroupBy.SalesRep,    (1, "Nimal"), (2, "Kamal"));
        Labels(SalesSummaryGroupBy.Territory,   (10, "Kandy"), (11, "Matale"));
        Labels(SalesSummaryGroupBy.Distributor, (100, "Easten"), (101, "Western"));
    }

    private void SetupSales(params SalesSummarySalesAgg[] rows) =>
        _repo.Setup(r => r.GetSalesAggregatesAsync(It.IsAny<SalesSummaryQuery>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(rows.ToList());

    private void SetupTargets(params SalesSummaryTargetAgg[] rows) =>
        _repo.Setup(r => r.GetTargetAggregatesAsync(
                 It.IsAny<SalesSummaryQuery>(), It.IsAny<IReadOnlyList<(int, int)>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(rows.ToList());

    private void Labels(SalesSummaryGroupBy dim, params (int Id, string Name)[] labels) =>
        _repo.Setup(r => r.GetLabelsAsync(dim, It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(labels.ToDictionary(x => x.Id, x => new SalesSummaryLabel("", x.Name)));

    /// <summary>Gross <paramref name="gross"/>, nothing deducted ⇒ net = gross.</summary>
    private static SalesSummarySalesAgg Sale(int? rep, int? territory, int? distributor, decimal gross)
        => new(rep, gross, 10m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, ThenByKeys: [territory, distributor]);

    private static SalesSummaryQuery Query(params SalesSummaryGroupBy[] dims)
        => new(dims[0], From, To, ThenBy: dims[1..]);

    [Fact]
    public async Task EachCombination_IsOneRow_WithOneLabelledCellPerDimension()
    {
        SetupSales(Sale(1, 10, 100, 500m), Sale(1, 11, 100, 300m));

        var result = await _sut.GetSalesSummaryAsync(Query(RepTerritoryDistributor));

        result.Dimensions.Should().Equal(RepTerritoryDistributor);
        result.Rows.Should().HaveCount(2);
        var kandy = result.Rows.Single(r => r.Groups![1].Name == "Kandy");
        kandy.Groups!.Select(g => g.Dimension).Should().Equal(RepTerritoryDistributor);
        kandy.Groups!.Select(g => g.Name).Should().Equal("Nimal", "Kandy", "Easten");
        kandy.Groups!.Select(g => g.Key).Should().Equal(1, 10, 100);
        // The single-dimension fields mirror the first dimension for older consumers.
        kandy.GroupKey.Should().Be(1);
        kandy.GroupName.Should().Be("Nimal");
    }

    [Fact]
    public async Task LabelsAreLookedUpOncePerDimension_WithThatDimensionsIds()
    {
        SetupSales(Sale(1, 10, 100, 500m), Sale(2, 10, 101, 300m));

        await _sut.GetSalesSummaryAsync(Query(RepTerritoryDistributor));

        _repo.Verify(r => r.GetLabelsAsync(SalesSummaryGroupBy.Territory,
            It.Is<IReadOnlyList<int>>(ids => ids.SequenceEqual(new[] { 10 })), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.GetLabelsAsync(SalesSummaryGroupBy.Distributor,
            It.Is<IReadOnlyList<int>>(ids => ids.OrderBy(i => i).SequenceEqual(new[] { 100, 101 })), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.GetLabelsAsync(It.IsAny<SalesSummaryGroupBy>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task NullKeyInALaterDimension_IsAnUnassignedCell_AndTheRowIsKept()
    {
        SetupSales(Sale(1, null, 100, 500m), Sale(1, 10, 100, 300m));

        var result = await _sut.GetSalesSummaryAsync(Query(RepTerritoryDistributor));

        result.Rows.Should().HaveCount(2);
        result.Rows.Should().ContainSingle(r => r.Groups![1].Name == "(Unassigned)" && r.Groups[1].Key == null);
        result.Totals.NetSaleValue.Should().Be(800m);
    }

    [Fact]
    public async Task Targets_MatchOnTheFullCombination()
    {
        SetupSales(Sale(1, 10, 100, 500m), Sale(1, 11, 100, 300m));
        SetupTargets(
            new SalesSummaryTargetAgg(1, 2026, 4, 50m, 1000m, ThenByKeys: [10, 100]),
            new SalesSummaryTargetAgg(1, 2026, 4, 20m, 600m, ThenByKeys: [11, 100]),
            // Target with no sales still gets a row (0% achievement is the point of the report).
            new SalesSummaryTargetAgg(2, 2026, 4, 10m, 400m, ThenByKeys: [10, 101]));

        var result = await _sut.GetSalesSummaryAsync(Query(RepTerritoryDistributor));

        result.Rows.Should().HaveCount(3);
        result.Rows.Single(r => r.Groups![1].Name == "Kandy" && r.GroupKey == 1).TargetValue.Should().Be(1000m);
        result.Rows.Single(r => r.Groups![1].Name == "Matale").TargetValue.Should().Be(600m);
        var targetOnly = result.Rows.Single(r => r.GroupKey == 2);
        targetOnly.NetSaleValue.Should().Be(0m);
        targetOnly.AchievementPercent.Should().Be(0m);
        result.Totals.TargetValue.Should().Be(2000m);
    }

    [Theory]
    [InlineData(SalesSummaryGroupBy.Route)]
    [InlineData(SalesSummaryGroupBy.Outlet)]
    public async Task RouteOrOutletInAnyPosition_TurnsTargetsOff(SalesSummaryGroupBy dim)
    {
        Labels(dim);
        SetupSales(Sale(1, 10, null, 500m));

        var result = await _sut.GetSalesSummaryAsync(Query(SalesSummaryGroupBy.SalesRep, dim));

        result.TargetsAvailable.Should().BeFalse();
        result.TargetsUnavailableReason.Should().NotBeNullOrEmpty();
        result.Rows.Single().TargetValue.Should().BeNull();
        _repo.Verify(r => r.GetTargetAggregatesAsync(It.IsAny<SalesSummaryQuery>(),
            It.IsAny<IReadOnlyList<(int, int)>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Rows_StayTogetherByFirstDimension_BlocksOrderedByTheirTotal()
    {
        // Kamal's two rows (400 + 350 = 750) outsell Nimal's one row (600), so Kamal's block comes
        // first even though Nimal has the single biggest row. Inside a block: largest first.
        SetupSales(Sale(1, 10, 100, 600m), Sale(2, 10, 100, 350m), Sale(2, 11, 100, 400m));

        var result = await _sut.GetSalesSummaryAsync(Query(RepTerritoryDistributor));

        result.Rows.Select(r => (r.GroupName, r.NetSaleValue)).Should().Equal(
            ("Kamal", 400m), ("Kamal", 350m), ("Nimal", 600m));
    }

    [Fact]
    public async Task GrandTotals_AreTheSameForAnyGrouping()
    {
        var rows = new[] { Sale(1, 10, 100, 600m), Sale(2, 10, 100, 350m), Sale(2, 11, 101, 400m) };
        SetupSales(rows);
        var multi = await _sut.GetSalesSummaryAsync(Query(RepTerritoryDistributor));

        // The same facts rolled up by rep only.
        SetupSales(
            new(1, 600m, 10m, 0m, 0m, 0m, 0m, 0m, 0m, 0m),
            new(2, 750m, 20m, 0m, 0m, 0m, 0m, 0m, 0m, 0m));
        var single = await _sut.GetSalesSummaryAsync(Query(SalesSummaryGroupBy.SalesRep));

        multi.Totals.NetSaleValue.Should().Be(single.Totals.NetSaleValue).And.Be(1350m);
        multi.Totals.GrossSaleValue.Should().Be(single.Totals.GrossSaleValue);
    }

    [Fact]
    public async Task CacheKey_DependsOnTheDimensionsAndTheirOrder()
    {
        SetupSales(Sale(1, 10, 100, 500m));
        var keys = new List<string>();
        _cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<SalesSummaryResponseDto>(),
                                     It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
              .Callback<string, SalesSummaryResponseDto, TimeSpan, CancellationToken>((k, _, _, _) => keys.Add(k));

        await _sut.GetSalesSummaryAsync(Query(SalesSummaryGroupBy.SalesRep));
        await _sut.GetSalesSummaryAsync(Query(SalesSummaryGroupBy.SalesRep, SalesSummaryGroupBy.Territory));
        await _sut.GetSalesSummaryAsync(Query(SalesSummaryGroupBy.Territory, SalesSummaryGroupBy.SalesRep));

        keys.Should().HaveCount(3).And.OnlyHaveUniqueItems();
    }

    // ── Validation ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Validator_RejectsADuplicateDimension()
        => new SalesSummaryQueryValidator()
            .Validate(Query(SalesSummaryGroupBy.SalesRep, SalesSummaryGroupBy.Area, SalesSummaryGroupBy.SalesRep))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validator_AcceptsSixDimensions_RejectsSeven()
    {
        var v = new SalesSummaryQueryValidator();
        SalesSummaryGroupBy[] seven =
        [
            SalesSummaryGroupBy.Region, SalesSummaryGroupBy.Area, SalesSummaryGroupBy.Territory,
            SalesSummaryGroupBy.Division, SalesSummaryGroupBy.SalesRep, SalesSummaryGroupBy.Distributor,
            SalesSummaryGroupBy.Product,
        ];

        v.Validate(Query(seven[..6])).IsValid.Should().BeTrue();
        v.Validate(Query(seven)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_RejectsAnUnknownThenByDimension()
        => new SalesSummaryQueryValidator()
            .Validate(Query(SalesSummaryGroupBy.SalesRep, (SalesSummaryGroupBy)99))
            .IsValid.Should().BeFalse();
}
