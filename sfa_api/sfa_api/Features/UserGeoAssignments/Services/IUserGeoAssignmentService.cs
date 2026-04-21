using sfa_api.Features.UserGeoAssignments.DTOs;
using sfa_api.Features.UserGeoAssignments.Requests;

namespace sfa_api.Features.UserGeoAssignments.Services;

public interface IUserGeoAssignmentService
{
    Task<UserAssignmentDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<RepAssignmentDto> GetMyAssignmentAsync(int userId, CancellationToken ct = default);

    Task<UserAssignmentListDto> GetAllAsync(
        int page,
        int pageSize,
        string? search = null,
        string? role = null,
        int? regionId = null,
        int? areaId = null,
        int? territoryId = null,
        int? divisionId = null,
        bool? isActive = null,
        CancellationToken ct = default);

    Task<UserAssignmentStatsDto> GetStatsAsync(CancellationToken ct = default);

    Task<UserAssignmentDto> CreateAsync(
        CreateUserAssignmentRequest request,
        int? callerId,
        CancellationToken ct = default);

    Task<UserAssignmentDto> UpdateAsync(
        int id,
        UpdateUserAssignmentRequest request,
        int? callerId,
        CancellationToken ct = default);

    Task DeleteAsync(int id, int? callerId, CancellationToken ct = default);

    Task<UserAssignmentDto> ActivateAsync(int id, int? callerId, CancellationToken ct = default);
}
