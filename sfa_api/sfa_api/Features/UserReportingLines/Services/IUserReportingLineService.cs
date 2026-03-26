using sfa_api.Features.UserReportingLines.DTOs;
using sfa_api.Features.UserReportingLines.Requests;

namespace sfa_api.Features.UserReportingLines.Services;

public interface IUserReportingLineService
{
    Task<UserReportingLineDto> GetByIdAsync(int id, CancellationToken ct = default);

    Task<UserReportingLineListDto> GetAllAsync(
        int page,
        int pageSize,
        string? search = null,
        string? role = null,
        int? reportsToUserId = null,
        bool? isActive = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the reporting subtree under a given user.
    /// When directOnly is true, returns only immediate direct reports.
    /// When false, traverses the full subtree via BFS.
    /// </summary>
    Task<IEnumerable<UserReportingLineDto>> GetSubordinatesAsync(int userId, bool directOnly, CancellationToken ct = default);

    /// <summary>
    /// Creates a new reporting line. If the user already has an active line,
    /// it is automatically deactivated before the new one is created.
    /// </summary>
    Task<UserReportingLineDto> CreateAsync(CreateUserReportingLineRequest request, int? callerId, CancellationToken ct = default);

    Task<UserReportingLineDto> UpdateAsync(int id, UpdateUserReportingLineRequest request, int? callerId, CancellationToken ct = default);

    /// <summary>Deactivates the reporting line (IsActive = false).</summary>
    Task DeleteAsync(int id, int? callerId, CancellationToken ct = default);

    /// <summary>Reactivates a previously deactivated reporting line (IsActive = true).</summary>
    Task ActivateAsync(int id, int? callerId, CancellationToken ct = default);
}
