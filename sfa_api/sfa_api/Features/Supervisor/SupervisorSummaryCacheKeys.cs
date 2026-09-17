namespace sfa_api.Features.Supervisor;

/// <summary>
/// Cache keys for <c>GET /api/v1/supervisor/summary</c>, cached per supervisor + summary date.
/// <para>
/// <see cref="ForSupervisor"/> is the supervisor's two-segment namespace
/// (<c>supervisor-summary:{id}</c>), so <c>RemoveByPrefixAsync(ForSupervisor(id))</c> evicts every
/// date for that supervisor only (see DistributedCacheService). Writes that move a supervisor's
/// numbers evict it: bill create / adjust / cancel / approve / reject (BillingService), not-billing
/// create (NotBillingService), and daily route assignment create / direct delete / approved deletion
/// (DailyRouteAssignmentService). Reporting-line and user changes rely on the short <see cref="Ttl"/>.
/// </para>
/// </summary>
public static class SupervisorSummaryCacheKeys
{
    public static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    public static string ForSupervisor(int supervisorId) => $"supervisor-summary:{supervisorId}";

    public static string Summary(int supervisorId, DateOnly date)
        => $"{ForSupervisor(supervisorId)}:{date:yyyy-MM-dd}";
}
