using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Billings.DTOs;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Billings.Repositories;
using sfa_api.Features.Users.Repositories;

namespace sfa_api.Features.Billings.Services;

public interface IDistributorBillingDashboardService
{
    /// <summary>
    /// Dashboard billing figures for the distributor linked to <paramref name="callerUserId"/>.
    /// Dates are Sri Lanka business dates, inclusive; a missing bound defaults to the other one,
    /// and both default to today (Asia/Colombo).
    /// </summary>
    Task<DistributorBillingDashboardDto> GetSummaryAsync(
        int callerUserId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken ct = default);
}

public class DistributorBillingDashboardService(
    IDistributorBillingDashboardRepository repo,
    IUserRepository userRepo) : IDistributorBillingDashboardService
{
    /// <summary>Upper bound on the range. The dashboard asks for 1 or 7 days.</summary>
    public const int MaxRangeDays = 92;

    public async Task<DistributorBillingDashboardDto> GetSummaryAsync(
        int callerUserId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken ct = default)
    {
        // Distributor is resolved from the JWT user, never taken from the client.
        var user = await userRepo.GetUserAccessInfoAsync(callerUserId, ct);
        if (user?.DistributorId is not int distributorId)
            throw new BusinessRuleException("NO_DISTRIBUTOR_LINKED",
                "Your account is not linked to a distributor.");

        var today = SriLankaTime.Today;
        var from = dateFrom ?? dateTo ?? today;
        var to = dateTo ?? dateFrom ?? today;

        if (to < from)
            throw new ValidationException(new Dictionary<string, string[]>
                { ["dateTo"] = ["dateTo must be on or after dateFrom."] });
        if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays)
            throw new ValidationException(new Dictionary<string, string[]>
                { ["dateTo"] = [$"The date range may not exceed {MaxRangeDays} days."] });

        var rows = await repo.GetGroupedAsync(distributorId, from, to, ct);
        return Aggregate(from, to, rows);
    }

    /// <summary>
    /// Pure aggregation (unit-tested). Mirrors what the web dashboard used to compute from the bill list:
    /// TotalCount / ApprovedCount / PendingCount count bills by distributor status regardless of rep
    /// status; every revenue figure excludes DistributorStatus == Rejected and RepStatus == Cancelled,
    /// because cancelling does not reset DistributorStatus (an approved-then-cancelled bill is not revenue).
    /// </summary>
    public static DistributorBillingDashboardDto Aggregate(
        DateOnly from, DateOnly to, IReadOnlyCollection<DistributorBillingDashboardGroupRow> rows)
    {
        var byDate = rows.ToLookup(r => r.BillingDate);

        var days = new List<DistributorBillingDashboardDayDto>(to.DayNumber - from.DayNumber + 1);
        for (var d = from; d <= to; d = d.AddDays(1))
            days.Add(Summarise(d, byDate[d]));

        return new DistributorBillingDashboardDto(
            from, to,
            TotalRevenue:    days.Sum(x => x.TotalRevenue),
            TotalCount:      days.Sum(x => x.TotalCount),
            ApprovedRevenue: days.Sum(x => x.ApprovedRevenue),
            ApprovedCount:   days.Sum(x => x.ApprovedCount),
            PendingRevenue:  days.Sum(x => x.PendingRevenue),
            PendingCount:    days.Sum(x => x.PendingCount),
            Days: days);
    }

    private static DistributorBillingDashboardDayDto Summarise(
        DateOnly date, IEnumerable<DistributorBillingDashboardGroupRow> groups)
    {
        decimal totalRevenue = 0, approvedRevenue = 0, pendingRevenue = 0;
        int totalCount = 0, approvedCount = 0, pendingCount = 0;

        foreach (var g in groups)
        {
            var isRevenue = g.RepStatus != RepBillingStatus.Cancelled
                         && g.DistributorStatus != DistributorBillingStatus.Rejected;
            totalCount += g.Count;
            if (isRevenue) totalRevenue += g.TotalAmount;

            switch (g.DistributorStatus)
            {
                case DistributorBillingStatus.Approved:
                    approvedCount += g.Count;
                    if (isRevenue) approvedRevenue += g.TotalAmount;
                    break;
                case DistributorBillingStatus.Pending:
                    pendingCount += g.Count;
                    if (isRevenue) pendingRevenue += g.TotalAmount;
                    break;
            }
        }

        return new DistributorBillingDashboardDayDto(
            date, totalRevenue, totalCount, approvedRevenue, approvedCount, pendingRevenue, pendingCount);
    }
}
