using sfa_api.Features.LocationPings.DTOs;
using sfa_api.Features.LocationPings.Requests;

namespace sfa_api.Features.LocationPings.Services;

public interface ILocationPingService
{
    Task<int> RecordAsync(int repId, CreateLocationPingsRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<RepLocationPingDto>> GetLatestPerRepAsync(CancellationToken ct = default);
}
