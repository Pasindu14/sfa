using sfa_api.Features.LocationPings.DTOs;
using sfa_api.Features.LocationPings.Requests;

namespace sfa_api.Features.LocationPings.Services;

public interface ILocationPingService
{
    Task<int> RecordAsync(int repId, CreateLocationPingsRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<RepLocationPingDto>> GetLatestPerRepAsync(CancellationToken ct = default);

    /// <summary>
    /// One rep's travelled route for a single Sri Lanka business day.
    /// Throws <c>NotFoundException</c> if the rep does not exist, so an unknown id is
    /// distinguishable from a rep who genuinely sent no pings that day.
    /// </summary>
    Task<RepRouteDto> GetRepRouteAsync(int repId, DateOnly date, CancellationToken ct = default);
}
