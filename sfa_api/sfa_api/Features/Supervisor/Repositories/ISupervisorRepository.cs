using sfa_api.Features.Supervisor.DTOs;

namespace sfa_api.Features.Supervisor.Repositories;

public interface ISupervisorRepository
{
    Task<bool> IsRepUnderSupervisorAsync(int supervisorId, int userId, CancellationToken ct = default);
    Task<int> CountRepsByReportsToAsync(int supervisorId, CancellationToken ct = default);
    Task<int> CountAssignedRepsTodayAsync(int supervisorId, DateOnly date, CancellationToken ct = default);
    Task<(int Count, decimal TotalAmount)> CountAndSumBillsTodayAsync(int supervisorId, DateOnly date, CancellationToken ct = default);
    Task<int> CountNonBillingsTodayBySupervisorAsync(int supervisorId, DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// One rep's live, non-deleted bills with BillingDate in [from, to], grouped by
    /// (RepStatus, DistributorStatus) with COUNT, SUM(TotalAmount) and SUM(TotalDiscount).
    /// </summary>
    Task<List<RepBillingStatusGroupRow>> GetRepBillingStatusGroupsAsync(
        int salesRepId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>
    /// Good (MarketResell) and market (Damage + Expire) return-line totals for the rep's
    /// approved + pending bills in [from, to]. DistributorReturn lines are in neither bucket.
    /// </summary>
    Task<RepBillingReturnTotals> GetRepReturnTotalsAsync(
        int salesRepId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>
    /// Per-product sale / free-issue / return sums over the rep's approved + pending bills in
    /// [from, to]. Products with only DistributorReturn lines still come back (all zeros).
    /// </summary>
    Task<List<RepItemSalesAgg>> GetRepItemSalesAsync(
        int salesRepId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Code and name for each id, including deactivated/deleted products.</summary>
    Task<Dictionary<int, (string Code, string Name)>> GetProductNamesAsync(
        IEnumerable<int> productIds, CancellationToken ct = default);
}
