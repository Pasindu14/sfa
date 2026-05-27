using sfa_api.Features.Outlets.DTOs;
using sfa_api.Features.Outlets.Requests;

namespace sfa_api.Features.Outlets.Services;

public interface IOutletService
{
    Task<OutletDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<OutletListDto> GetAllAsync(int page, int pageSize, bool? isActive = null, string? search = null, CancellationToken ct = default);
    Task<OutletListDto> GetAllByTerritoryAsync(int territoryId, int page, int pageSize, bool? isActive = null, string? search = null, CancellationToken ct = default);
    Task<IEnumerable<OutletDto>> GetAllActiveAsync(CancellationToken ct = default);
    Task<IEnumerable<OutletMapPointDto>> GetMapPointsAsync(CancellationToken ct = default);
    Task<IEnumerable<OutletDto>> GetByRouteIdAsync(int routeId, CancellationToken ct = default);
    Task<OutletDto> CreateAsync(CreateOutletRequest request, int? callerId, CancellationToken ct = default);
    Task<OutletDto> UpdateAsync(int id, UpdateOutletRequest request, int? callerId, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task ActivateAsync(int id, int? callerId, CancellationToken ct = default);
    Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default);
}
