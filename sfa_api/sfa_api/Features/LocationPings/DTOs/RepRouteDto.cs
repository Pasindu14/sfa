namespace sfa_api.Features.LocationPings.DTOs;

/// <summary>
/// One rep's travelled route for a single Sri Lanka business day.
///
/// Deliberately not a <c>List&lt;RepLocationPingDto&gt;</c>: that DTO repeats RepId and RepName on
/// every row, which is dead weight when all points belong to one rep. Here the rep is named
/// once and the points stay minimal.
/// </summary>
public record RepRouteDto(
    int RepId,
    string RepName,
    DateOnly Date,
    RepRouteSummaryDto Summary,
    IReadOnlyList<RepRoutePointDto> Points);

/// <summary>
/// A single GPS fix on the route.
/// <para>
/// <c>RecordedAt</c> is the device clock — the real moment the position was captured, and the
/// order the rep actually travelled in. <c>ReceivedAt</c> is the server clock at upload; a
/// large gap between the two means the ping was queued offline and back-filled later, which
/// is worth being able to see rather than hiding.
/// </para>
/// </summary>
public record RepRoutePointDto(
    double Latitude,
    double Longitude,
    float Accuracy,
    DateTimeOffset RecordedAt,
    DateTimeOffset ReceivedAt);

/// <summary>
/// Totals computed server-side so every client doesn't re-derive them.
/// </summary>
/// <param name="MeasuredDistanceMeters">
/// Sum of straight-line hops between consecutive pings, <b>excluding</b> hops that span a
/// gap — across a gap the path is unknown, so the straight line is the size of the missing
/// data, not distance travelled. This is a lower bound on real road distance: roads bend,
/// straight lines don't, and the phone only reports every few minutes.
/// </param>
/// <param name="GapCount">How many gaps were skipped, so the UI can say what isn't counted.</param>
/// <param name="GapThresholdMinutes">
/// The rule used to classify a gap. Returned so clients draw those segments as gaps using
/// the same threshold the distance was calculated with, instead of keeping their own copy
/// that can drift out of step.
/// </param>
public record RepRouteSummaryDto(
    int PointCount,
    DateTimeOffset? FirstPingAt,
    DateTimeOffset? LastPingAt,
    double MeasuredDistanceMeters,
    int GapCount,
    int GapThresholdMinutes);
