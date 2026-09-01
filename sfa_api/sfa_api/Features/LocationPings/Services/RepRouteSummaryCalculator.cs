using sfa_api.Common.Geo;
using sfa_api.Features.LocationPings.DTOs;

namespace sfa_api.Features.LocationPings.Services;

/// <summary>
/// Builds the totals for a rep's day from their ordered pings.
///
/// Extracted from the service so it can be unit tested: the integration test that would
/// cover this path is skipped, because the SQLite test provider cannot translate
/// DateTimeOffset range comparisons (see LocationPingsApiTests).
/// </summary>
public static class RepRouteSummaryCalculator
{
    /// <summary>
    /// Two consecutive pings further apart than this are treated as a gap rather than a
    /// journey. The phone reports about every 5 minutes, so exceeding this means at least
    /// two reports were missed and whatever happened in between was not recorded.
    /// </summary>
    public const int GapThresholdMinutes = 15;

    private static readonly TimeSpan GapThreshold = TimeSpan.FromMinutes(GapThresholdMinutes);

    public static RepRouteSummaryDto Build(IReadOnlyList<RepRoutePointDto> points)
    {
        double measured = 0;
        var gaps = 0;

        for (var i = 1; i < points.Count; i++)
        {
            var previous = points[i - 1];
            var current = points[i];

            // A gap means we have no idea what path was taken, so the straight line joining
            // the two ends is not distance travelled — it is the width of what we missed.
            // Counting it would let one missing hour invent hundreds of kilometres.
            if (current.RecordedAt - previous.RecordedAt > GapThreshold)
            {
                gaps++;
                continue;
            }

            var hop = GeoMath.HaversineMeters(
                previous.Latitude, previous.Longitude,
                current.Latitude, current.Longitude);

            // MaxValue is GeoMath's "no coordinate" sentinel for a (0,0) endpoint.
            if (hop < double.MaxValue) measured += hop;
        }

        return new RepRouteSummaryDto(
            PointCount: points.Count,
            FirstPingAt: points.Count > 0 ? points[0].RecordedAt : null,
            LastPingAt: points.Count > 0 ? points[^1].RecordedAt : null,
            MeasuredDistanceMeters: measured,
            GapCount: gaps,
            GapThresholdMinutes: GapThresholdMinutes);
    }
}
