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
        ValidateRange(from, to);

        // Sequential — EF Core DbContext does not support concurrent operations on the same instance
        var groups  = await _repository.GetRepBillingStatusGroupsAsync(salesRepId, from, to, ct);
        var returns = await _repository.GetRepReturnTotalsAsync(salesRepId, from, to, ct);
        return BuildRepBillingSummary(from, to, groups, returns);
    }

    public async Task<RepItemwiseSalesDto> GetRepItemwiseSalesAsync(
        int salesRepId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        ValidateRange(from, to);

        // Sequential — EF Core DbContext does not support concurrent operations on the same instance
        var aggs   = await _repository.GetRepItemSalesAsync(salesRepId, from, to, ct);
        var names  = await _repository.GetProductNamesAsync(aggs.Select(a => a.ProductId), ct);
        var groups = await _repository.GetRepBillingStatusGroupsAsync(salesRepId, from, to, ct);

        // Same live-bill discount the Sales Summary reports (BuildRepBillingSummary).
        var headerDiscount = groups
            .Where(g => g.RepStatus != RepBillingStatus.Cancelled
                     && g.DistributorStatus != DistributorBillingStatus.Rejected)
            .Sum(g => g.TotalDiscount);

        return BuildRepItemwiseSales(from, to, aggs, names, headerDiscount);
    }

    private static void ValidateRange(DateOnly from, DateOnly to)
    {
        if (to < from)
            throw new ValidationException(new Dictionary<string, string[]>
                { ["to"] = ["to must be on or after from."] });
        if (to.DayNumber - from.DayNumber + 1 > MaxSummaryRangeDays)
            throw new ValidationException(new Dictionary<string, string[]>
                { ["to"] = [$"The date range may not exceed {MaxSummaryRangeDays} days."] });
    }

    /// <summary>
    /// Pure aggregation (unit-tested). Rows with nothing sold, given free or returned (a product whose
    /// only line is a DistributorReturn) are dropped. Best sellers first.
    /// <paramref name="headerDiscount"/> is Σ Billing.TotalDiscount over the same live bills: each
    /// header rounds its own bill discount, so summing per-item allocated shares can drift a cent
    /// from it. The totals use the header figure so they agree with the Sales Summary exactly;
    /// per-item discounts are the allocated shares and may sum a cent away from the total.
    /// </summary>
    public static RepItemwiseSalesDto BuildRepItemwiseSales(
        DateOnly from, DateOnly to,
        IEnumerable<RepItemSalesAgg> aggs,
        IReadOnlyDictionary<int, (string Code, string Name)> names,
        decimal headerDiscount)
    {
        var items = aggs
            .Where(a => a.SaleQty != 0 || a.FreeIssueQty != 0 || a.GoodReturnQty != 0 || a.MarketReturnQty != 0)
            .Select(a =>
            {
                var (code, name) = names.TryGetValue(a.ProductId, out var n)
                    ? n
                    : ($"#{a.ProductId}", $"Product {a.ProductId}");
                var discount = Math.Round(a.ItemDiscount + a.BillDiscount, 2);
                return new RepItemwiseSalesItemDto(
                    ProductId:         a.ProductId,
                    ItemCode:          code,
                    ItemName:          name,
                    SaleQty:           a.SaleQty,
                    FreeIssueQty:      a.FreeIssueQty,
                    GoodReturnQty:     a.GoodReturnQty,
                    MarketReturnQty:   a.MarketReturnQty,
                    GrossValue:        a.GrossValue,
                    Discount:          discount,
                    GoodReturnValue:   a.GoodReturnValue,
                    MarketReturnValue: a.MarketReturnValue,
                    NetValue:          a.GrossValue - discount - a.GoodReturnValue - a.MarketReturnValue);
            })
            .OrderByDescending(i => i.GrossValue)
            .ThenByDescending(i => i.SaleQty)
            .ThenBy(i => i.ItemName)
            .ToList();

        var totalDiscount = headerDiscount;
        var totalGross    = items.Sum(i => i.GrossValue);
        var totalGood     = items.Sum(i => i.GoodReturnValue);
        var totalMarket   = items.Sum(i => i.MarketReturnValue);

        return new RepItemwiseSalesDto(
            From:                   from,
            To:                     to,
            TotalSaleQty:           items.Sum(i => i.SaleQty),
            TotalFreeIssueQty:      items.Sum(i => i.FreeIssueQty),
            TotalGoodReturnQty:     items.Sum(i => i.GoodReturnQty),
            TotalMarketReturnQty:   items.Sum(i => i.MarketReturnQty),
            TotalGrossValue:        totalGross,
            TotalDiscount:          totalDiscount,
            TotalGoodReturnValue:   totalGood,
            TotalMarketReturnValue: totalMarket,
            TotalNetValue:          totalGross - totalDiscount - totalGood - totalMarket,
            Items:                  items);
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
