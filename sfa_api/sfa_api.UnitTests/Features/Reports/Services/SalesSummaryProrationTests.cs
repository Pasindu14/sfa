using FluentAssertions;
using sfa_api.Features.Reports.Services;

namespace sfa_api.UnitTests.Features.Reports.Services;

/// <summary>
/// Sales targets are stored per (Year, Month), so an arbitrary date range has to be split into
/// day-weighted month slices. This is pure arithmetic with no I/O — and, because the SQLite test
/// provider cannot translate SUM over decimal, it is also one of the few parts of the report that
/// can be exercised end-to-end in CI at all.
/// </summary>
public class SalesSummaryProrationTests
{
    [Fact]
    public void MonthWeights_FullMonth_IsExactlyOne()
    {
        var result = SalesSummaryProration.MonthWeights(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        result.Should().ContainSingle();
        result[0].Year.Should().Be(2026);
        result[0].Month.Should().Be(4);
        result[0].Factor.Should().Be(1m);
    }

    [Fact]
    public void MonthWeights_PartialMonth_IsDaysOverDaysInMonth()
    {
        // 15..30 April inclusive = 16 days of 30.
        var result = SalesSummaryProration.MonthWeights(new DateOnly(2026, 4, 15), new DateOnly(2026, 4, 30));

        result.Should().ContainSingle();
        result[0].Factor.Should().Be(16m / 30m);
    }

    [Fact]
    public void MonthWeights_SpanningTwoMonths_WeightsEachSeparately()
    {
        // 15 Apr .. 14 May: 16 of April's 30 days, 14 of May's 31.
        var result = SalesSummaryProration.MonthWeights(new DateOnly(2026, 4, 15), new DateOnly(2026, 5, 14));

        result.Should().HaveCount(2);
        result[0].Should().BeEquivalentTo(new { Year = 2026, Month = 4, Factor = 16m / 30m });
        result[1].Should().BeEquivalentTo(new { Year = 2026, Month = 5, Factor = 14m / 31m });
    }

    [Fact]
    public void MonthWeights_SpanningThreeMonths_MiddleMonthIsWhole()
    {
        var result = SalesSummaryProration.MonthWeights(new DateOnly(2026, 4, 20), new DateOnly(2026, 6, 10));

        result.Should().HaveCount(3);
        result[0].Factor.Should().Be(11m / 30m);   // 20..30 April
        result[1].Factor.Should().Be(1m);          // all of May
        result[2].Factor.Should().Be(10m / 30m);   // 1..10 June
    }

    [Fact]
    public void MonthWeights_WholeMonthsRange_EveryFactorIsOne()
    {
        var result = SalesSummaryProration.MonthWeights(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

        result.Should().HaveCount(3);
        result.Should().OnlyContain(m => m.Factor == 1m);
    }

    [Fact]
    public void MonthWeights_SingleDay_IsOneOverDaysInMonth()
    {
        var result = SalesSummaryProration.MonthWeights(new DateOnly(2026, 7, 9), new DateOnly(2026, 7, 9));

        result.Should().ContainSingle();
        result[0].Factor.Should().Be(1m / 31m);
    }

    [Theory]
    [InlineData(2024, 29)]   // leap
    [InlineData(2026, 28)]   // non-leap
    public void MonthWeights_February_UsesActualDaysInMonth(int year, int daysInFebruary)
    {
        var result = SalesSummaryProration.MonthWeights(new DateOnly(year, 2, 1), new DateOnly(year, 2, 10));

        result.Should().ContainSingle();
        result[0].Factor.Should().Be(10m / daysInFebruary);
    }

    [Fact]
    public void MonthWeights_FullLeapFebruary_IsExactlyOne()
    {
        var result = SalesSummaryProration.MonthWeights(new DateOnly(2024, 2, 1), new DateOnly(2024, 2, 29));

        result.Should().ContainSingle();
        result[0].Factor.Should().Be(1m);
    }

    [Fact]
    public void MonthWeights_ToBeforeFrom_ReturnsEmpty()
    {
        var result = SalesSummaryProration.MonthWeights(new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 1));

        result.Should().BeEmpty();
    }

    [Fact]
    public void MonthWeights_YearBoundary_IsHandled()
    {
        var result = SalesSummaryProration.MonthWeights(new DateOnly(2025, 12, 20), new DateOnly(2026, 1, 10));

        result.Should().HaveCount(2);
        result[0].Should().BeEquivalentTo(new { Year = 2025, Month = 12, Factor = 12m / 31m });
        result[1].Should().BeEquivalentTo(new { Year = 2026, Month = 1, Factor = 10m / 31m });
    }
}
