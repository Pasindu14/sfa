using sfa_api.Features.UserReportingLines.Entities;

namespace sfa_api.Features.UserReportingLines.Repositories;

public interface IUserReportingLineRepository
{
    Task<UserReportingLine?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<UserReportingLine?> GetActiveByUserIdAsync(int userId, CancellationToken ct = default);

    Task<(IEnumerable<UserReportingLine> Items, int TotalCount)> GetAllAsync(
        int skip,
        int take,
        string? search = null,
        string? role = null,
        int? reportsToUserId = null,
        bool? isActive = null,
        CancellationToken ct = default);

    /// <summary>Returns all active reporting lines where ReportsToUserId = managerId.</summary>
    Task<IEnumerable<UserReportingLine>> GetDirectReportsAsync(int managerId, CancellationToken ct = default);

    /// <summary>Batch fetch: returns userId → managerId mapping for all supplied userIds. Used for in-memory org-chain walks.</summary>
    Task<Dictionary<int, int>> GetActiveLinesForUsersAsync(IEnumerable<int> userIds, CancellationToken ct = default);

    Task<bool> UserExistsAsync(int userId, CancellationToken ct = default);
    Task<bool> IsAdminOrDistributorAsync(int userId, CancellationToken ct = default);

    Task CreateAsync(UserReportingLine line, CancellationToken ct = default);
    Task UpdateAsync(UserReportingLine line, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
