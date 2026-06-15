namespace sfa_api.Features.Supervisor.Repositories;

public interface ISupervisorRepository
{
    Task<bool> IsRepUnderSupervisorAsync(int supervisorId, int userId, CancellationToken ct = default);
    Task<int> CountRepsByReportsToAsync(int supervisorId, CancellationToken ct = default);
    Task<int> CountAssignedRepsTodayAsync(int supervisorId, DateOnly date, CancellationToken ct = default);
    Task<(int Count, decimal TotalAmount)> CountAndSumBillsTodayAsync(int supervisorId, DateOnly date, CancellationToken ct = default);
    Task<int> CountNonBillingsTodayBySupervisorAsync(int supervisorId, DateOnly date, CancellationToken ct = default);
}
