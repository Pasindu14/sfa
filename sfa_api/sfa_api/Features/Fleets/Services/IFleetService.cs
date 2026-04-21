using sfa_api.Features.Fleets.DTOs;
using sfa_api.Features.Fleets.Requests;

namespace sfa_api.Features.Fleets.Services;

public interface IFleetService
{
    Task<FleetDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<FleetListDto> GetAllAsync(int page, int pageSize, string? search = null, CancellationToken ct = default);
    Task<IEnumerable<FleetDto>> GetAllActiveAsync(CancellationToken ct = default);
    Task<FleetDto> CreateAsync(CreateFleetRequest request, int? callerId, CancellationToken ct = default);
    Task<FleetDto> UpdateAsync(int id, UpdateFleetRequest request, int? callerId, CancellationToken ct = default);
    Task ActivateAsync(int id, int? callerId, CancellationToken ct = default);
    Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default);
}
