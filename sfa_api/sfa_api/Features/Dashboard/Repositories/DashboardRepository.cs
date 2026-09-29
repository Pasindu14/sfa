using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Extensions;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Dashboard.DTOs;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Dashboard.Repositories;

public class DashboardRepository(AppDbContext context) : IDashboardRepository
{
    private readonly AppDbContext _context = context;

    public async Task<List<DashboardDailyRevenueAgg>> GetDailyRevenueAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        // Same universe as SalesSummaryRepository.GetSalesAggregatesAsync (approved, not cancelled —
        // CancelAsync never inspects DistributorStatus, so an approved bill can still be cancelled).
        //
        // Net Sale Value per line, collapsed from the report's formula
        //   (SaleGross − GoodReturn) − MarketReturn − (DbDiscount + ItemWise + BillDisc):
        //   Sale                              → TotalPrice × (1 − BillDiscountRate / 100)
        //   Return (MarketResell/Damage/Expire) → −TotalPrice
        //   FreeIssue funded by the distributor → −TotalPrice
        // DistributorReturn lines are excluded, exactly as in the report.
        var raw = await _context.BillingItems
            .AsNoTracking()
            .Where(bi => bi.Billing.DistributorStatus == DistributorBillingStatus.Approved
                      && bi.Billing.RepStatus != RepBillingStatus.Cancelled
                      && bi.Billing.IsActive
                      && bi.Billing.BillingDate >= from
                      && bi.Billing.BillingDate <= to)
            .GroupBy(bi => bi.Billing.BillingDate)
            .Select(g => new
            {
                Date = g.Key,
                Revenue = g.Sum(x =>
                    x.BillingItemType == BillingItemType.Sale
                        ? x.TotalPrice - x.TotalPrice * x.Billing.BillDiscountRate / 100m
                    : x.BillingItemType == BillingItemType.Return
                      && x.ReturnType != ReturnType.DistributorReturn
                        ? -x.TotalPrice
                    : x.BillingItemType == BillingItemType.FreeIssue
                      && x.FreeIssueSource == FreeIssueSource.Distributor
                        ? -x.TotalPrice
                    : 0m),
            })
            .ToListAsync(ct);

        return [.. raw.Select(r => new DashboardDailyRevenueAgg(r.Date, Math.Round(r.Revenue, 2)))];
    }

    public Task<int> CountRevenueBillsAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
        => _context.Billings
            .AsNoTracking()
            .CountAsync(b => b.DistributorStatus == DistributorBillingStatus.Approved
                          && b.RepStatus != RepBillingStatus.Cancelled
                          && b.IsActive
                          && b.BillingDate >= from
                          && b.BillingDate <= to, ct);

    public async Task<int> CountActiveRepsAsync(DateOnly date, CancellationToken ct = default)
    {
        // Activity, not revenue: a bill still awaiting distributor approval proves the rep was out
        // working, so only cancelled bills are excluded. Sequential — shared DbContext.
        var billing = await _context.Billings
            .AsNoTracking()
            .Where(b => b.BillingDate == date && b.RepStatus != RepBillingStatus.Cancelled && b.IsActive)
            .Select(b => b.SalesRepId)
            .Distinct()
            .ToListAsync(ct);

        var notBilling = await _context.NotBillings
            .AsNoTracking()
            .Where(n => n.NotBillingDate == date && n.IsActive)
            .Select(n => n.SalesRepId)
            .Distinct()
            .ToListAsync(ct);

        return billing.Union(notBilling).Count();
    }

    public Task<int> CountSalesRepsAsync(CancellationToken ct = default)
        => _context.Users
            .AsNoTracking()
            .CountAsync(u => u.Role == UserRole.SalesRep && u.IsActive, ct);

    public async Task<DashboardOutletCounts> GetOutletCountsAsync(DateOnly date, CancellationToken ct = default)
    {
        // IgnoreQueryFilters: Outlet's global filter hides deactivated rows, and the deactivated
        // share is half of what this metric reports. !IsDeleted is re-applied by hand.
        var byStatus = await _context.Outlets
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => !o.IsDeleted)
            .GroupBy(o => o.IsActive)
            .Select(g => new { IsActive = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // CreatedAt is an absolute instant — filter by the Sri Lanka business day, not the UTC date.
        var start = SriLankaTime.StartOfDayUtc(date);
        var end   = SriLankaTime.StartOfDayUtc(date.AddDays(1));
        var newOnDate = await _context.Outlets
            .AsNoTracking()   // global filter = active and not deleted, which is what "activated" means
            .CountAsync(o => o.CreatedAt >= start && o.CreatedAt < end, ct);

        return new DashboardOutletCounts(
            byStatus.Where(x => x.IsActive).Sum(x => x.Count),
            byStatus.Where(x => !x.IsActive).Sum(x => x.Count),
            newOnDate);
    }

    public Task<int> CountBilledOutletsAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
        => _context.Billings
            .AsNoTracking()
            .Where(b => b.BillingDate >= from
                     && b.BillingDate <= to
                     && b.RepStatus != RepBillingStatus.Cancelled
                     && b.DistributorStatus != DistributorBillingStatus.Rejected
                     && b.IsActive)
            .Select(b => b.OutletId)
            .Distinct()
            .CountAsync(ct);
}
