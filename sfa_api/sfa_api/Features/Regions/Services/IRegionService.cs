using sfa_api.Features.Regions.DTOs;
using sfa_api.Features.Regions.Requests;

namespace sfa_api.Features.Regions.Services;

public interface IRegionService
{
    Task<RegionDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<RegionListDto> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<RegionDto> CreateAsync(CreateRegionRequest request, int? callerId, CancellationToken ct = default);
    Task<RegionDto> UpdateAsync(int id, UpdateRegionRequest request, int? callerId, CancellationToken ct = default);
    Task ActivateAsync(int id, int? callerId, CancellationToken ct = default);
    Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default);
}
