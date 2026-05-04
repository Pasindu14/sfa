using sfa_api.Features.Supervisor.DTOs;
using sfa_api.Features.Supervisor.Repositories;

namespace sfa_api.Features.Supervisor.Services;

public class SupervisorService(ISupervisorRepository repository) : ISupervisorService
{
    private readonly ISupervisorRepository _repository = repository;

    public async Task<SupervisorSummaryDto> GetSummaryAsync(int supervisorId, DateOnly date, CancellationToken ct = default)
    {
        // Sequential — EF Core DbContext does not support concurrent operations on the same instance
        var totalReps        = await _repository.CountRepsByReportsToAsync(supervisorId, ct);
        var assignedReps     = await _repository.CountAssignedRepsTodayAsync(supervisorId, date, ct);
        var billsToday       = await _repository.CountBillsTodayBySupervisorAsync(supervisorId, date, ct);
        var nonBillingsToday = await _repository.CountNonBillingsTodayBySupervisorAsync(supervisorId, date, ct);

        return new SupervisorSummaryDto(
            TotalReps:        totalReps,
            AssignedReps:     assignedReps,
            BillsToday:       billsToday,
            NonBillingsToday: nonBillingsToday);
    }
}
