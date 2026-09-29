using sfa_api.Features.Dashboard.DTOs;

namespace sfa_api.Features.Dashboard.Services;

/// <summary>
/// The admin dashboard, one method per independently-loaded section. Each takes a Sri Lanka
/// business day (today when <c>date</c> is null) and rejects a future date.
/// </summary>
public interface IDashboardService
{
    /// <summary>Revenue, target, discount and return figures for the day and month to date, plus the regional breakdown.</summary>
    Task<DashboardSalesDto> GetSalesAsync(DateOnly? date, CancellationToken ct = default);

    /// <summary>Active reps and outlet/customer counts.</summary>
    Task<DashboardActivityDto> GetActivityAsync(DateOnly? date, CancellationToken ct = default);

    /// <summary>Revenue per day for the month up to the date.</summary>
    Task<DashboardTrendDto> GetTrendAsync(DateOnly? date, CancellationToken ct = default);
}
