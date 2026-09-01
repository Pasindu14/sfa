using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Common.Geo;
using sfa_api.Features.LocationPings.DTOs;
using sfa_api.Features.LocationPings.Entities;
using sfa_api.Features.LocationPings.Repositories;
using sfa_api.Features.LocationPings.Requests;
using sfa_api.Features.Users.Repositories;

namespace sfa_api.Features.LocationPings.Services;

public class LocationPingService(
    ILocationPingRepository repository,
    IUserRepository userRepository) : ILocationPingService
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

    public async Task<RepRouteDto> GetRepRouteAsync(int repId, DateOnly date, CancellationToken ct = default)
    {
        // Resolve the rep first so an unknown id 404s rather than returning an empty
        // route, which would read as "he didn't move today".
        var rep = await userRepository.GetUserByIdAsync(repId, ct)
            ?? throw new NotFoundException("User", repId);

        // A business day is 00:00–24:00 in Colombo, not in UTC — see SriLankaTime.DayRange.
        var (fromInclusive, toExclusive) = SriLankaTime.DayRange(date);

        var pings = await repository.GetForRepBetweenAsync(repId, fromInclusive, toExclusive, ct);

        var points = pings.Select(p => new RepRoutePointDto(
            Latitude:   p.Latitude,
            Longitude:  p.Longitude,
            Accuracy:   p.Accuracy,
            RecordedAt: p.RecordedAt,
            ReceivedAt: p.ReceivedAt
        )).ToList();

        return new RepRouteDto(
            RepId:   repId,
            RepName: rep.Name ?? string.Empty,
            Date:    date,
            Summary: new RepRouteSummaryDto(
                PointCount:          points.Count,
                FirstPingAt:         points.Count > 0 ? points[0].RecordedAt : null,
                LastPingAt:          points.Count > 0 ? points[^1].RecordedAt : null,
                TotalDistanceMeters: TotalDistanceMeters(points)),
            Points: points);
    }

    /// Sum of great-circle hops between consecutive fixes. GeoMath returns double.MaxValue
    /// for a (0,0) endpoint — its "no coordinate" sentinel — so those hops are skipped
    /// rather than poisoning the total with a ~10,000 km segment.
    private static double TotalDistanceMeters(IReadOnlyList<RepRoutePointDto> points)
    {
        double total = 0;
        for (var i = 1; i < points.Count; i++)
        {
            var hop = GeoMath.HaversineMeters(
                points[i - 1].Latitude, points[i - 1].Longitude,
                points[i].Latitude,     points[i].Longitude);

            if (hop < double.MaxValue) total += hop;
        }
        return total;
    }
}
