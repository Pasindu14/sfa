using sfa_api.Features.LocationPings.DTOs;
using sfa_api.Features.LocationPings.Entities;
using sfa_api.Features.LocationPings.Repositories;
using sfa_api.Features.LocationPings.Requests;

namespace sfa_api.Features.LocationPings.Services;

public class LocationPingService(ILocationPingRepository repository) : ILocationPingService
{
    public async Task<int> RecordAsync(int repId, CreateLocationPingsRequest request, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        var pings = request.Pings.Select(p => new RepLocationPing
        {
            RepId      = repId,
            Latitude   = p.Latitude,
            Longitude  = p.Longitude,
            Accuracy   = p.Accuracy,
            RecordedAt = p.RecordedAt,
            ReceivedAt = now,
        }).ToList();

        await repository.BulkInsertAsync(pings, ct);
        return pings.Count;
    }

    public async Task<IReadOnlyList<RepLocationPingDto>> GetLatestPerRepAsync(CancellationToken ct = default)
    {
        var pings = await repository.GetLatestPerRepAsync(ct);

        return pings.Select(p => new RepLocationPingDto(
            RepId:      p.RepId,
            RepName:    p.Rep?.Name ?? string.Empty,
            Latitude:   p.Latitude,
            Longitude:  p.Longitude,
            Accuracy:   p.Accuracy,
            RecordedAt: p.RecordedAt,
            ReceivedAt: p.ReceivedAt
        )).ToList();
    }
}
