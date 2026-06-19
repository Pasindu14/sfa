using sfa_api.Features.LocationPings.Entities;

namespace sfa_api.Features.LocationPings.Repositories;

public interface ILocationPingRepository
{
    Task BulkInsertAsync(IEnumerable<RepLocationPing> pings, CancellationToken ct = default);
    Task<IReadOnlyList<RepLocationPing>> GetLatestPerRepAsync(CancellationToken ct = default);
}
