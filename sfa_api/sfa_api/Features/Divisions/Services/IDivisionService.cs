using sfa_api.Features.Divisions.DTOs;
using sfa_api.Features.Divisions.Requests;

namespace sfa_api.Features.Divisions.Services;

public interface IDivisionService
{
    Task<DivisionDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<DivisionListDto> GetAllAsync(int page, int pageSize, int? territoryId = null, int? areaId = null, int? regionId = null, bool? isActive = null, string? search = null, CancellationToken ct = default);
    Task<IEnumerable<DivisionDto>> GetAllActiveAsync(int? territoryId = null, CancellationToken ct = default);
    Task<DivisionDto> CreateAsync(CreateDivisionRequest request, int? callerId, CancellationToken ct = default);
    Task<DivisionDto> UpdateAsync(int id, UpdateDivisionRequest request, int? callerId, CancellationToken ct = default);
    Task ActivateAsync(int id, int? callerId, CancellationToken ct = default);
    Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default);
    Task DeleteAsync(int id, int? callerId, CancellationToken ct = default);
}
