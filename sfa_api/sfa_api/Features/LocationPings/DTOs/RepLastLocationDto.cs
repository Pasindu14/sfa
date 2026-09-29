namespace sfa_api.Features.LocationPings.DTOs;

/// <summary>
/// One rep's single most recent ping — position only, never the day's trail. Exposed to the
/// rep's own supervisor; the full movement history stays Admin-only.
/// </summary>
public record RepLastLocationDto(
    double Latitude,
    double Longitude,
    float Accuracy,
    DateTimeOffset RecordedAt,
    DateTimeOffset ReceivedAt);
