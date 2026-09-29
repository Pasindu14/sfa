using sfa_api.Features.Supervisor.DTOs;

namespace sfa_api.Features.Supervisor.Services;

public interface ISupervisorService
{
    Task<SupervisorSummaryDto> GetSummaryAsync(int supervisorId, DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// Throws <see cref="Common.Errors.AuthorizationException"/> if <paramref name="userId"/> is not an
    /// active SalesRep reporting to <paramref name="supervisorId"/>. Guards rep-scoped endpoints against IDOR.
    /// </summary>
    Task EnsureRepUnderSupervisorAsync(int supervisorId, int userId, CancellationToken ct = default);

    /// <summary>
    /// One rep's billing totals for BillingDate in [from, to] (at most
    /// <see cref="SupervisorService.MaxSummaryRangeDays"/> days). Caller must have checked
    /// the rep belongs to the supervisor.
    /// </summary>
    Task<RepBillingSummaryDto> GetRepBillingSummaryAsync(
        int salesRepId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>
    /// One rep's per-product sales for BillingDate in [from, to] over approved + pending bills
    /// (same range cap as <see cref="GetRepBillingSummaryAsync"/>). Caller must have checked
    /// the rep belongs to the supervisor.
    /// </summary>
    Task<RepItemwiseSalesDto> GetRepItemwiseSalesAsync(
        int salesRepId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
