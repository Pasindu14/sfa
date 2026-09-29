using sfa_api.Features.Dashboard.DTOs;

namespace sfa_api.Features.Dashboard.Services;

public interface IDashboardService
{
    /// <summary>The admin dashboard for one Sri Lanka business day (today when <paramref name="date"/> is null).</summary>
    Task<DashboardDto> GetAsync(DateOnly? date, CancellationToken ct = default);
}
