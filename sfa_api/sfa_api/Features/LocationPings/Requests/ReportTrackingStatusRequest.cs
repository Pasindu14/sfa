namespace sfa_api.Features.LocationPings.Requests;

/// <summary>
/// Sent by the mobile app on a tick where no position could be captured.
/// RepId is resolved from the JWT — never trusted from the body.
/// </summary>
public record ReportTrackingStatusRequest(
    string Reason,
    DateTimeOffset OccurredAt,
    double? AccuracyMeters = null
);
