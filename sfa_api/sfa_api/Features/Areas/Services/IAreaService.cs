using sfa_api.Features.Areas.DTOs;
using sfa_api.Features.Areas.Requests;

namespace sfa_api.Features.Areas.Services;

public interface IAreaService
{
    Task<AreaDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<AreaListDto> GetAllAsync(int page, int pageSize, int? regionId = null, bool? isActive = null, string? search = null, CancellationToken ct = default);
    Task<IEnumerable<AreaDto>> GetAllActiveAsync(int? regionId = null, CancellationToken ct = default);
    Task<AreaDto> CreateAsync(CreateAreaRequest request, int? callerId, CancellationToken ct = default);
    Task<AreaDto> UpdateAsync(int id, UpdateAreaRequest request, int? callerId, CancellationToken ct = default);
    Task ActivateAsync(int id, int? callerId, CancellationToken ct = default);
    Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default);
    Task DeleteAsync(int id, int? callerId, CancellationToken ct = default);
}
