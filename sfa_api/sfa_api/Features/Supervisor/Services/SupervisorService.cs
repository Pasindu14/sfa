using sfa_api.Common.Errors;
using sfa_api.Features.Supervisor.DTOs;
using sfa_api.Features.Supervisor.Repositories;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.Supervisor.Services;

public class SupervisorService(ISupervisorRepository repository, ICacheService cache) : ISupervisorService
{
    private readonly ISupervisorRepository _repository = repository;
    private readonly ICacheService _cache = cache;

    public async Task EnsureRepUnderSupervisorAsync(int supervisorId, int userId, CancellationToken ct = default)
    {
        if (!await _repository.IsRepUnderSupervisorAsync(supervisorId, userId, ct))
            throw new AuthorizationException("this sales rep's data");
    }

    public async Task<SupervisorSummaryDto> GetSummaryAsync(int supervisorId, DateOnly date, CancellationToken ct = default)
    {
        // Short-lived cache (SupervisorSummaryCacheKeys.Ttl); the bill / not-billing / route-assignment
        // write paths evict this supervisor's entries so their own actions show up immediately.
        var cacheKey = SupervisorSummaryCacheKeys.Summary(supervisorId, date);
        var cached = await _cache.GetAsync<SupervisorSummaryDto>(cacheKey, ct);
        if (cached is not null) return cached;

        // Sequential — EF Core DbContext does not support concurrent operations on the same instance
        var totalReps                     = await _repository.CountRepsByReportsToAsync(supervisorId, ct);
        var assignedReps                  = await _repository.CountAssignedRepsTodayAsync(supervisorId, date, ct);
        var (billsToday, totalSalesToday) = await _repository.CountAndSumBillsTodayAsync(supervisorId, date, ct);
        var nonBillingsToday              = await _repository.CountNonBillingsTodayBySupervisorAsync(supervisorId, date, ct);

        var result = new SupervisorSummaryDto(
            TotalReps:        totalReps,
            AssignedReps:     assignedReps,
            BillsToday:       billsToday,
            NonBillingsToday: nonBillingsToday,
            TotalSalesToday:  totalSalesToday);

        await _cache.SetAsync(cacheKey, result, SupervisorSummaryCacheKeys.Ttl, ct);
        return result;
    }
}
