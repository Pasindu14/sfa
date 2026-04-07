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

    /// <summary>
    /// Role-aware delete:
    /// - Supervisor → flags as PendingApproval, returns updated DTO.
    /// - Admin/NSM/RSM → direct soft-delete, returns null (caller maps to 204).
    /// </summary>
    Task<DailyRouteAssignmentDto?> DeleteAsync(int id, int? callerId, string callerRole, string? reason, CancellationToken ct = default);

    /// <summary>Admin/NSM/RSM approves a pending deletion → assignment is soft-deleted.</summary>
    Task ApproveDeletionAsync(int id, int? callerId, CancellationToken ct = default);

    /// <summary>Admin/NSM/RSM rejects a pending deletion → assignment stays active, status set to Rejected.</summary>
    Task RejectDeletionAsync(int id, int? callerId, string? reason, CancellationToken ct = default);

    /// <summary>Returns all assignments with DeletionStatus == PendingApproval (for manager oversight).</summary>
    Task<DailyRouteAssignmentListDto> GetPendingDeletionsAsync(int page, int pageSize, CancellationToken ct = default);
}
