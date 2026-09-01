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
/// <c>TotalDistanceMeters</c> is the sum of great-circle hops between consecutive points —
/// it is a lower bound on real distance travelled, since the phone only reports roughly
/// every 5 minutes and drops fixes it considers too inaccurate.
/// </summary>
public record RepRouteSummaryDto(
    int PointCount,
    DateTimeOffset? FirstPingAt,
    DateTimeOffset? LastPingAt,
    double TotalDistanceMeters);
