using sfa_api.Common.Errors;
using sfa_api.Features.Billings.Enums;
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

    /// <summary>Same cap as the distributor billing dashboard.</summary>
    public const int MaxSummaryRangeDays = 92;

    public async Task<RepBillingSummaryDto> GetRepBillingSummaryAsync(
        int salesRepId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from)
            throw new ValidationException(new Dictionary<string, string[]>
                { ["to"] = ["to must be on or after from."] });
        if (to.DayNumber - from.DayNumber + 1 > MaxSummaryRangeDays)
            throw new ValidationException(new Dictionary<string, string[]>
                { ["to"] = [$"The date range may not exceed {MaxSummaryRangeDays} days."] });

        // Sequential — EF Core DbContext does not support concurrent operations on the same instance
        var groups  = await _repository.GetRepBillingStatusGroupsAsync(salesRepId, from, to, ct);
        var returns = await _repository.GetRepReturnTotalsAsync(salesRepId, from, to, ct);
        return BuildRepBillingSummary(from, to, groups, returns);
    }

    /// <summary>
    /// Pure aggregation (unit-tested). Rep cancellation wins over distributor approval — CancelAsync
    /// ignores DistributorStatus, so an approved bill can later be cancelled and must not count as a
    /// sale. Money figures cover live bills only (approved + pending).
    /// </summary>
    public static RepBillingSummaryDto BuildRepBillingSummary(
        DateOnly from, DateOnly to,
        IEnumerable<RepBillingStatusGroupRow> groups,
        RepBillingReturnTotals returns)
    {
        int approvedN = 0, pendingN = 0, rejectedN = 0, cancelledN = 0;
        decimal approved = 0m, pending = 0m, discount = 0m;

        foreach (var g in groups)
        {
            if (g.RepStatus == RepBillingStatus.Cancelled)
            {
                cancelledN += g.Count;
                continue;
            }
            switch (g.DistributorStatus)
            {
                case DistributorBillingStatus.Rejected:
                    rejectedN += g.Count;
                    break;
                case DistributorBillingStatus.Approved:
                    approvedN += g.Count;
                    approved  += g.TotalAmount;
                    discount  += g.TotalDiscount;
                    break;
                default:
                    pendingN += g.Count;
                    pending  += g.TotalAmount;
                    discount += g.TotalDiscount;
                    break;
            }
        }

        return new RepBillingSummaryDto(
            From:           from,
            To:             to,
            TotalBills:     approvedN + pendingN + rejectedN + cancelledN,
            ApprovedCount:  approvedN,
            PendingCount:   pendingN,
            RejectedCount:  rejectedN,
            CancelledCount: cancelledN,
            TotalBilled:    approved + pending,
            ApprovedSales:  approved,
            PendingValue:   pending,
            TotalDiscount:  discount,
            GoodReturn:     returns.GoodReturn,
            MarketReturn:   returns.MarketReturn);
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
