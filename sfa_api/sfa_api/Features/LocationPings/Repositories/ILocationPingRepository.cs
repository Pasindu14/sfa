using sfa_api.Features.LocationPings.Entities;

namespace sfa_api.Features.LocationPings.Repositories;

public interface ILocationPingRepository
{
    Task BulkInsertAsync(IEnumerable<RepLocationPing> pings, CancellationToken ct = default);
    Task<IReadOnlyList<RepLocationPing>> GetLatestPerRepAsync(CancellationToken ct = default);

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
