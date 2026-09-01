using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.LocationPings.DTOs;
using sfa_api.Features.LocationPings.Entities;
using sfa_api.Features.LocationPings.Repositories;
using sfa_api.Features.LocationPings.Requests;
using sfa_api.Features.Users.Repositories;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.LocationPings.Services;

public class LocationPingService(
    ILocationPingRepository repository,
    IUserRepository userRepository,
    ICacheService cache) : ILocationPingService
{
    /// Current state, not history — rewritten every few minutes and meaningless once stale,
    /// so it lives in the cache rather than earning a table and a migration. Two days is
    /// long enough to explain a quiet weekend and short enough to self-expire.
    private static readonly TimeSpan TrackingStatusTtl = TimeSpan.FromDays(2);

    private static string TrackingStatusKey(int repId) => $"tracking:status:{repId}";

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
            Summary: RepRouteSummaryCalculator.Build(points),
            Points:  points,
            TrackingStatus: await GetTrackingStatusAsync(repId, ct));
    }

    public async Task ReportTrackingStatusAsync(
        int repId, ReportTrackingStatusRequest request, CancellationToken ct = default)
    {
        var status = new RepTrackingStatusDto(
            Reason:         request.Reason,
            AccuracyMeters: request.AccuracyMeters,
            ReportedAt:     request.OccurredAt);

        await cache.SetAsync(TrackingStatusKey(repId), status, TrackingStatusTtl, ct);
    }

    public Task<RepTrackingStatusDto?> GetTrackingStatusAsync(int repId, CancellationToken ct = default)
        => cache.GetAsync<RepTrackingStatusDto>(TrackingStatusKey(repId), ct);
}
