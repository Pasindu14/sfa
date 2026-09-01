namespace sfa_api.Features.LocationPings.DTOs;

/// <summary>
/// Why a rep's phone last failed to record a position.
///
/// Location capture fails silently by design — a denied permission, a weak fix or an
/// indoor GPS timeout all produce simply "no row". That makes "the service is dead" and
/// "the service is alive but every fix is being rejected" look identical from the server,
/// which is the single hardest thing to diagnose about field tracking.
///
/// The phone reports this only on ticks where it captured nothing, so a healthy rep adds no
/// extra traffic: their pings are already the signal. Held in the cache rather than the
/// database because it is current state, not history — it is rewritten every 5 minutes and
/// is worthless once stale.
/// </summary>
/// <param name="Reason">A <c>TrackingSkipReasons</c> value.</param>
/// <param name="AccuracyMeters">Set when the reason is a rejected low-accuracy fix.</param>
/// <param name="ReportedAt">Device clock at the failed tick.</param>
public record RepTrackingStatusDto(
    string Reason,
    double? AccuracyMeters,
    DateTimeOffset ReportedAt);

/// <summary>
/// The reasons the mobile app can report. Kept as constants rather than an enum so an
/// older or newer app version reporting an unknown value is stored and displayed verbatim
/// instead of failing to deserialise.
/// </summary>
public static class TrackingSkipReasons
{
    public const string PermissionDenied = "PermissionDenied";
    public const string LocationServicesOff = "LocationServicesOff";
    public const string NoFixTimeout = "NoFixTimeout";
    public const string AccuracyTooPoor = "AccuracyTooPoor";
    public const string ZeroCoordinate = "ZeroCoordinate";
    public const string CaptureError = "CaptureError";
}
