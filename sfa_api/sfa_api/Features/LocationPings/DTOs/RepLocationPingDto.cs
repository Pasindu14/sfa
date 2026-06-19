namespace sfa_api.Features.LocationPings.DTOs;

public record RepLocationPingDto(
    int RepId,
    string RepName,
    double Latitude,
    double Longitude,
    float Accuracy,
    DateTimeOffset RecordedAt,
    DateTimeOffset ReceivedAt
);
