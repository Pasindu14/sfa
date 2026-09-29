using sfa_api.Features.Dashboard.DTOs;

namespace sfa_api.Features.Dashboard.Repositories;

/// <summary>
/// The dashboard facts the Sales Summary report does not already produce. Revenue, targets,
/// discounts and returns come from <c>ISalesSummaryService</c> instead, so they stay identical to
/// the report by construction.
/// </summary>
public interface IDashboardRepository
{
    /// <summary>Net revenue per business day over [from, to] — same formula as Sales Summary's Net Sale Value.</summary>
    Task<List<DashboardDailyRevenueAgg>> GetDailyRevenueAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Revenue bills (approved, not cancelled) over [from, to].</summary>
    Task<int> CountRevenueBillsAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Distinct reps with a live bill or a no-sale visit on the date.</summary>
    Task<int> CountActiveRepsAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>Active sales-rep accounts.</summary>
    Task<int> CountSalesRepsAsync(CancellationToken ct = default);

    /// <summary>Active / deactivated outlet counts (current snapshot) and active outlets registered on the date.</summary>
    Task<DashboardOutletCounts> GetOutletCountsAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>Distinct outlets with a live (not cancelled, not rejected) bill over [from, to].</summary>
    Task<int> CountBilledOutletsAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}
