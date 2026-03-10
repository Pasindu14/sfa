using sfa_api.Features.Territories.DTOs;
using sfa_api.Features.Territories.Requests;

namespace sfa_api.Features.Territories.Services;

public interface ITerritoryService
{
    Task<TerritoryDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TerritoryListDto> GetAllAsync(int page, int pageSize, int? areaId = null, bool? isActive = null, CancellationToken ct = default);
    Task<IEnumerable<TerritoryDto>> GetAllActiveAsync(int? areaId = null, CancellationToken ct = default);
    Task<TerritoryDto> CreateAsync(CreateTerritoryRequest request, int? callerId, CancellationToken ct = default);
    Task<TerritoryDto> UpdateAsync(int id, UpdateTerritoryRequest request, int? callerId, CancellationToken ct = default);
    Task ActivateAsync(int id, int? callerId, CancellationToken ct = default);
    Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default);
}
