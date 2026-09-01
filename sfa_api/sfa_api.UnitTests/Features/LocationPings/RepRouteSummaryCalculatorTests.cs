using FluentAssertions;
using sfa_api.Features.LocationPings.DTOs;
using sfa_api.Features.LocationPings.Services;

namespace sfa_api.UnitTests.Features.LocationPings;

/// <summary>
/// The distance shown on the Rep Route History page. The behaviour that matters most here is
/// that a gap is NOT counted as travel: one missing hour must not invent hundreds of
/// kilometres of "distance covered".
/// </summary>
public class RepRouteSummaryCalculatorTests
{
    private static readonly DateTimeOffset Start =
        new(2026, 6, 15, 3, 0, 0, TimeSpan.Zero);

    private static RepRoutePointDto Ping(double lat, double lng, int minutesFromStart) =>
        new(lat, lng, 10f, Start.AddMinutes(minutesFromStart), Start.AddMinutes(minutesFromStart));

    [Fact]
    public void Build_NoPoints_ReturnsZeroedSummary()
    {
        var summary = RepRouteSummaryCalculator.Build([]);

        summary.PointCount.Should().Be(0);
        summary.MeasuredDistanceMeters.Should().Be(0);
        summary.GapCount.Should().Be(0);
        summary.FirstPingAt.Should().BeNull();
        summary.LastPingAt.Should().BeNull();
    }

    [Fact]
    public void Build_SinglePoint_HasNoDistanceButIsStillStampedWithTimes()
    {
        var summary = RepRouteSummaryCalculator.Build([Ping(6.90, 79.85, 0)]);

        summary.PointCount.Should().Be(1);
        summary.MeasuredDistanceMeters.Should().Be(0, "one position implies no movement");
        summary.FirstPingAt.Should().Be(summary.LastPingAt);
    }

    [Fact]
    public void Build_ConsecutivePings_SumsTheHops()
    {
        // Three points 1° of latitude apart, 5 minutes between each ≈ 111 km per hop.
        var summary = RepRouteSummaryCalculator.Build([
            Ping(6.0, 80.0, 0),
            Ping(7.0, 80.0, 5),
            Ping(8.0, 80.0, 10),
        ]);

        summary.MeasuredDistanceMeters.Should().BeApproximately(222_390, 4_000);
        summary.GapCount.Should().Be(0);
    }

    [Fact]
    public void Build_HopAcrossAGap_IsExcludedFromDistanceAndCounted()
    {
        // Colombo → Jaffna with 3 hours of silence between: the straight line is the size of
        // the missing data, not a journey we recorded.
        var summary = RepRouteSummaryCalculator.Build([
            Ping(6.9271, 79.8612, 0),
            Ping(9.6615, 80.0255, 180),
        ]);

        summary.GapCount.Should().Be(1);
        summary.MeasuredDistanceMeters.Should().Be(0,
            "nothing was actually observed between the two ends of a gap");
    }

    [Fact]
    public void Build_MixedRoute_CountsOnlyTheObservedStretches()
    {
        var summary = RepRouteSummaryCalculator.Build([
            Ping(6.0, 80.0, 0),
            Ping(7.0, 80.0, 5),     // measured  ≈ 111 km
            Ping(9.0, 80.0, 200),   // gap       — excluded
            Ping(9.1, 80.0, 205),   // measured  ≈ 11 km
        ]);

        summary.GapCount.Should().Be(1);
        summary.PointCount.Should().Be(4);
        summary.MeasuredDistanceMeters.Should().BeApproximately(122_300, 4_000,
            "only the two in-sequence hops count; the 200-minute jump does not");
    }

    [Fact]
    public void Build_ExactlyAtTheThreshold_IsStillTreatedAsTravel()
    {
        var summary = RepRouteSummaryCalculator.Build([
            Ping(6.0, 80.0, 0),
            Ping(6.1, 80.0, RepRouteSummaryCalculator.GapThresholdMinutes),
        ]);

        summary.GapCount.Should().Be(0, "the threshold is exclusive — only beyond it is a gap");
        summary.MeasuredDistanceMeters.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Build_ZeroZeroCoordinate_DoesNotAddTenThousandKilometres()
    {
        // (0,0) is GeoMath's "no coordinate" sentinel; treating it as a real position would
        // put the rep in the Atlantic.
        var summary = RepRouteSummaryCalculator.Build([
            Ping(6.9271, 79.8612, 0),
            Ping(0, 0, 5),
        ]);

        summary.MeasuredDistanceMeters.Should().Be(0);
        summary.GapCount.Should().Be(0, "it is a bad coordinate, not missing time");
    }

    [Fact]
    public void Build_ReportsTheThresholdItUsed()
    {
        var summary = RepRouteSummaryCalculator.Build([Ping(6.0, 80.0, 0)]);

        summary.GapThresholdMinutes.Should().Be(RepRouteSummaryCalculator.GapThresholdMinutes,
            "clients draw gap segments with this value, so it must come from the same source "
            + "as the distance calculation");
    }
}
