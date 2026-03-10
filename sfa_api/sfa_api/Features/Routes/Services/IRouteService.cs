using sfa_api.Features.Routes.DTOs;
using sfa_api.Features.Routes.Requests;

namespace sfa_api.Features.Routes.Services;

public interface IRouteService
{
    Task<RouteDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<RouteListDto> GetAllAsync(int page, int pageSize, string? search = null, CancellationToken ct = default);
    Task<RouteDto> CreateAsync(CreateRouteRequest request, int? callerId, CancellationToken ct = default);
    Task<RouteDto> UpdateAsync(int id, UpdateRouteRequest request, int? callerId, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
