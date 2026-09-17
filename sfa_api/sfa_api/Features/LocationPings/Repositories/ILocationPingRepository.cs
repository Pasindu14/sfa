using sfa_api.Features.LocationPings.DTOs;
using sfa_api.Features.LocationPings.Entities;

namespace sfa_api.Features.LocationPings.Repositories;

public interface ILocationPingRepository
{
    Task BulkInsertAsync(IEnumerable<RepLocationPing> pings, CancellationToken ct = default);

    /// <summary>
    /// Latest ping per (non-deleted) rep, projected to the live-map DTO. When
    /// <paramref name="sinceUtc"/> is set only pings recorded at or after it are considered,
    /// so reps silent since then are omitted; null = every rep's last-ever ping.
    /// </summary>
    Task<IReadOnlyList<RepLocationPingDto>> GetLatestPerRepAsync(
        DateTimeOffset? sinceUtc, CancellationToken ct = default);

    /// <summary>
    /// Every ping for one rep within a half-open instant range, oldest first — the rep's
    /// travelled route. Ordered by <c>RecordedAt</c> (device capture time), which is the
    /// true travel order; <c>ReceivedAt</c> can lag arbitrarily for offline back-fills.
    /// </summary>
    Task<IReadOnlyList<RepLocationPing>> GetForRepBetweenAsync(
        int repId,
        DateTimeOffset fromInclusive,
        DateTimeOffset toExclusive,
        CancellationToken ct = default);
}
