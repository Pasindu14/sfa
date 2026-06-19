namespace sfa_api.Features.LocationPings.Requests;

public record PingItem(
    double Latitude,
    double Longitude,
    float Accuracy,
    DateTimeOffset RecordedAt
);

public record CreateLocationPingsRequest(List<PingItem> Pings);
