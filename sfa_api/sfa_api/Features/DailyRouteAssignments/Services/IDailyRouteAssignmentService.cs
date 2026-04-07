using sfa_api.Features.DailyRouteAssignments.DTOs;
using sfa_api.Features.DailyRouteAssignments.Requests;

namespace sfa_api.Features.DailyRouteAssignments.Services;

public interface IDailyRouteAssignmentService
{
    Task<DailyRouteAssignmentDto> GetByIdAsync(int id, CancellationToken ct = default);

    Task<DailyRouteAssignmentListDto> GetAllAsync(
        int page,
        int pageSize,
        int? userId = null,
        int? routeId = null,
        DateOnly? date = null,
        CancellationToken ct = default);

    /// <summary>Returns sales reps who have an active reporting line to the current supervisor.</summary>
    Task<IEnumerable<RepSummaryDto>> GetMyRepsAsync(int supervisorId, CancellationToken ct = default);

    /// <summary>Returns active routes available in the rep's assigned division.</summary>
    Task<IEnumerable<RepRouteDto>> GetRepRoutesAsync(int userId, CancellationToken ct = default);

    Task<DailyRouteAssignmentDto> CreateAsync(
        CreateDailyRouteAssignmentRequest request,
        int? callerId,
        CancellationToken ct = default);

    Task DeleteAsync(int id, int? callerId, CancellationToken ct = default);
}
