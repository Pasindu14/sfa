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
}
