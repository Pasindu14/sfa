namespace sfa_api.Features.Supervisor.Repositories;

public interface ISupervisorRepository
{
    Task<int> CountRepsByReportsToAsync(int supervisorId, CancellationToken ct = default);
    Task<int> CountAssignedRepsTodayAsync(int supervisorId, DateOnly date, CancellationToken ct = default);
    Task<int> CountBillsTodayBySupervisorAsync(int supervisorId, DateOnly date, CancellationToken ct = default);
    Task<int> CountNonBillingsTodayBySupervisorAsync(int supervisorId, DateOnly date, CancellationToken ct = default);
}
