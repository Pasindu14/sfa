using FluentAssertions;
using sfa_api.Features.Reports.Enums;
using sfa_api.Features.Reports.Requests;
using sfa_api.Features.Reports.Validators;

namespace sfa_api.UnitTests.Features.Reports.Validators;

public class SalesSummaryQueryValidatorTests
{
    private readonly SalesSummaryQueryValidator _sut = new();

    private static SalesSummaryQuery Query(
        DateOnly? from = null,
        DateOnly? to = null,
        SalesSummaryGroupBy groupBy = SalesSummaryGroupBy.SalesRep,
        int? salesRepId = null)
        => new(groupBy,
               from ?? new DateOnly(2026, 4, 1),
               to ?? new DateOnly(2026, 4, 30),
               SalesRepId: salesRepId);

    [Fact]
    public void Valid_SingleMonth_Passes()
        => _sut.Validate(Query()).IsValid.Should().BeTrue();

    [Fact]
    public void SameDayRange_Passes()
    {
        var day = new DateOnly(2026, 4, 15);
        _sut.Validate(Query(day, day)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ToBeforeFrom_Fails()
    {
        var result = _sut.Validate(Query(new DateOnly(2026, 4, 30), new DateOnly(2026, 4, 1)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SalesSummaryQuery.To));
    }

    [Fact]
    public void RangeAtTheCap_Passes()
    {
        var from = new DateOnly(2026, 1, 1);
        _sut.Validate(Query(from, from.AddDays(366))).IsValid.Should().BeTrue();
    }

    [Fact]
    public void RangeBeyondTheCap_Fails()
    {
        var from = new DateOnly(2026, 1, 1);

        var result = _sut.Validate(Query(from, from.AddDays(367)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("366 days"));
    }

    [Fact]
    public void UnrecognisedGroupBy_Fails()
    {
        var result = _sut.Validate(Query(groupBy: (SalesSummaryGroupBy)999));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SalesSummaryQuery.GroupBy));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveFilterId_Fails(int id)
        => _sut.Validate(Query(salesRepId: id)).IsValid.Should().BeFalse();

    [Fact]
    public void NullFilterId_Passes()
        => _sut.Validate(Query(salesRepId: null)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(SalesSummaryGroupBy.SalesRep)]
    [InlineData(SalesSummaryGroupBy.Supervisor)]
    [InlineData(SalesSummaryGroupBy.Asm)]
    [InlineData(SalesSummaryGroupBy.Rsm)]
    [InlineData(SalesSummaryGroupBy.Nsm)]
    [InlineData(SalesSummaryGroupBy.Distributor)]
    [InlineData(SalesSummaryGroupBy.Outlet)]
    [InlineData(SalesSummaryGroupBy.Route)]
    [InlineData(SalesSummaryGroupBy.Division)]
    [InlineData(SalesSummaryGroupBy.Territory)]
    [InlineData(SalesSummaryGroupBy.Area)]
    [InlineData(SalesSummaryGroupBy.Region)]
    [InlineData(SalesSummaryGroupBy.Product)]
    public void EveryDeclaredGroupingIsAccepted(SalesSummaryGroupBy groupBy)
        => _sut.Validate(Query(groupBy: groupBy)).IsValid.Should().BeTrue();
}
