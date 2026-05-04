using sfa_api.Features.Supervisor.DTOs;

namespace sfa_api.Features.Supervisor.Services;

public interface ISupervisorService
{
    Task<SupervisorSummaryDto> GetSummaryAsync(int supervisorId, DateOnly date, CancellationToken ct = default);
}
