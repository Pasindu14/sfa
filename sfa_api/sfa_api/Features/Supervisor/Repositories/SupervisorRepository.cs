using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Billings.Entities;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Supervisor.DTOs;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Supervisor.Repositories;

public class SupervisorRepository(AppDbContext context) : ISupervisorRepository
{
    private readonly AppDbContext _context = context;

    public async Task<bool> IsRepUnderSupervisorAsync(int supervisorId, int userId, CancellationToken ct = default)
        => await _context.UserReportingLines
            .AnyAsync(rl => rl.ReportsToUserId == supervisorId
                         && rl.UserId == userId
                         && rl.IsActive
                         && !rl.IsDeleted
                         && rl.User!.Role == UserRole.SalesRep
                         && !rl.User.IsDeleted, ct);

    public async Task<int> CountRepsByReportsToAsync(int supervisorId, CancellationToken ct = default)
        => await _context.UserReportingLines
            .Where(rl => rl.ReportsToUserId == supervisorId
                      && rl.IsActive
                      && !rl.IsDeleted
                      && rl.User!.Role == UserRole.SalesRep
                      && !rl.User.IsDeleted)
            .CountAsync(ct);

    public async Task<int> CountAssignedRepsTodayAsync(int supervisorId, DateOnly date, CancellationToken ct = default)
    {
        // Get all rep IDs under this supervisor first, then count which have assignments for the date.
        // Two-step to keep the query simple and avoid a complex join across unrelated tables.
        var repIds = await _context.UserReportingLines
            .Where(rl => rl.ReportsToUserId == supervisorId
                      && rl.IsActive
                      && !rl.IsDeleted
                      && rl.User!.Role == UserRole.SalesRep
                      && !rl.User.IsDeleted)
            .Select(rl => rl.UserId)
            .ToListAsync(ct);

        if (repIds.Count == 0) return 0;

        return await _context.DailyRouteAssignments
            .Where(a => repIds.Contains(a.UserId)
                     && a.AssignedDate == date
                     && a.IsActive
                     && !a.IsDeleted)
            .Select(a => a.UserId)
            .Distinct()
            .CountAsync(ct);
    }

    public async Task<(int Count, decimal TotalAmount)> CountAndSumBillsTodayAsync(int supervisorId, DateOnly date, CancellationToken ct = default)
    {
        var result = await _context.Billings
            .Where(b => b.SupervisorUserId == supervisorId
                     && b.BillingDate == date
                     && b.IsActive
                     && !b.IsDeleted)
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Total = g.Sum(b => b.TotalAmount) })
            .FirstOrDefaultAsync(ct);

        return result is null ? (0, 0m) : (result.Count, result.Total);
    }

    public async Task<int> CountNonBillingsTodayBySupervisorAsync(int supervisorId, DateOnly date, CancellationToken ct = default)
        => await _context.NotBillings
            .Where(nb => nb.SupervisorUserId == supervisorId
                      && nb.NotBillingDate == date
                      && nb.IsActive
                      && !nb.IsDeleted)
            .CountAsync(ct);

    // Bill's own state only — no join to Outlet/User, so a later-deactivated outlet or rep never
    // removes historical revenue (reporting convention: financial aggregates are facts).
    private IQueryable<Billing> RepBills(int salesRepId, DateOnly from, DateOnly to)
        => _context.Billings
            .AsNoTracking()
            .Where(b => b.SalesRepId == salesRepId
                     && b.IsActive
                     && !b.IsDeleted
                     && b.BillingDate >= from
                     && b.BillingDate <= to);

    // The SQLite test provider cannot translate SUM over decimal (same split as
    // DistributorBillingDashboardRepository): PostgreSQL aggregates in SQL, others in memory.
    private bool IsNpgsql => _context.Database.ProviderName?.Contains("Npgsql") == true;

    public async Task<List<RepBillingStatusGroupRow>> GetRepBillingStatusGroupsAsync(
        int salesRepId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var query = RepBills(salesRepId, from, to);

        if (IsNpgsql)
        {
            // At most 2 x 3 rows back. EF can't bind a GroupBy to a record ctor — anonymous first.
            var grouped = await query
                .GroupBy(b => new { b.RepStatus, b.DistributorStatus })
                .Select(g => new
                {
                    g.Key.RepStatus,
                    g.Key.DistributorStatus,
                    Count = g.Count(),
                    TotalAmount = g.Sum(b => b.TotalAmount),
                    TotalDiscount = g.Sum(b => b.TotalDiscount),
                })
                .ToListAsync(ct);

            return grouped
                .Select(r => new RepBillingStatusGroupRow(
                    r.RepStatus, r.DistributorStatus, r.Count, r.TotalAmount, r.TotalDiscount))
                .ToList();
        }

        var rows = await query
            .Select(b => new { b.RepStatus, b.DistributorStatus, b.TotalAmount, b.TotalDiscount })
            .ToListAsync(ct);

        return rows
            .GroupBy(b => new { b.RepStatus, b.DistributorStatus })
            .Select(g => new RepBillingStatusGroupRow(
                g.Key.RepStatus, g.Key.DistributorStatus, g.Count(),
                g.Sum(b => b.TotalAmount), g.Sum(b => b.TotalDiscount)))
            .ToList();
    }

    public async Task<RepBillingReturnTotals> GetRepReturnTotalsAsync(
        int salesRepId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        // Live bills only — an approved-then-cancelled bill lands at (Cancelled, Approved), so the
        // RepStatus check is needed as well as the distributor one. !IsDeleted on the item arrives
        // via the global query filter. Return predicate matches SalesSummaryRepository.
        var billIds = RepBills(salesRepId, from, to)
            .Where(b => b.RepStatus != RepBillingStatus.Cancelled
                     && b.DistributorStatus != DistributorBillingStatus.Rejected)
            .Select(b => b.Id);

        var returns = _context.BillingItems
            .AsNoTracking()
            .Where(bi => billIds.Contains(bi.BillingId)
                      && bi.BillingItemType == BillingItemType.Return
                      && (bi.ReturnType == ReturnType.MarketResell
                       || bi.ReturnType == ReturnType.Damage
                       || bi.ReturnType == ReturnType.Expire));

        if (IsNpgsql)
        {
            var sums = await returns
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Good = g.Sum(x => x.ReturnType == ReturnType.MarketResell ? x.TotalPrice : 0m),
                    Market = g.Sum(x => x.ReturnType != ReturnType.MarketResell ? x.TotalPrice : 0m),
                })
                .FirstOrDefaultAsync(ct);

            return sums is null
                ? new RepBillingReturnTotals(0m, 0m)
                : new RepBillingReturnTotals(sums.Good, sums.Market);
        }

        var lines = await returns
            .Select(x => new { x.ReturnType, x.TotalPrice })
            .ToListAsync(ct);

        return new RepBillingReturnTotals(
            lines.Where(x => x.ReturnType == ReturnType.MarketResell).Sum(x => x.TotalPrice),
            lines.Where(x => x.ReturnType != ReturnType.MarketResell).Sum(x => x.TotalPrice));
    }

    public async Task<List<RepItemSalesAgg>> GetRepItemSalesAsync(
        int salesRepId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        // Same live-bill universe as GetRepReturnTotalsAsync; line measures match
        // SalesSummaryRepository.GetSalesAggregatesAsync so the two reports agree per product.
        var items = _context.BillingItems
            .AsNoTracking()
            .Where(bi => bi.Billing.SalesRepId == salesRepId
                      && bi.Billing.IsActive
                      && !bi.Billing.IsDeleted
                      && bi.Billing.BillingDate >= from
                      && bi.Billing.BillingDate <= to
                      && bi.Billing.RepStatus != RepBillingStatus.Cancelled
                      && bi.Billing.DistributorStatus != DistributorBillingStatus.Rejected);

        if (IsNpgsql)
        {
            var grouped = await items
                .GroupBy(bi => bi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    SaleQty = g.Sum(x => x.BillingItemType == BillingItemType.Sale ? x.Quantity : 0m),
                    FreeQty = g.Sum(x => x.BillingItemType == BillingItemType.FreeIssue ? x.Quantity : 0m),
                    // Before any discount — TotalPrice + DiscountAmount is bit-identical to the
                    // write path's per-line rounding.
                    Gross = g.Sum(x => x.BillingItemType == BillingItemType.Sale
                                       ? x.TotalPrice + x.DiscountAmount : 0m),
                    ItemDisc = g.Sum(x => x.BillingItemType == BillingItemType.Sale ? x.DiscountAmount : 0m),
                    // Bill-header discount is a flat % of the sale sub-total, so allocating it per
                    // line sums back to Billing.BillDiscountAmount up to the per-bill rounding.
                    BillDisc = g.Sum(x => x.BillingItemType == BillingItemType.Sale
                                          ? x.TotalPrice * x.Billing.BillDiscountRate / 100m : 0m),
                    GoodQty = g.Sum(x => x.BillingItemType == BillingItemType.Return
                                      && x.ReturnType == ReturnType.MarketResell ? x.Quantity : 0m),
                    GoodVal = g.Sum(x => x.BillingItemType == BillingItemType.Return
                                      && x.ReturnType == ReturnType.MarketResell ? x.TotalPrice : 0m),
                    MktQty = g.Sum(x => x.BillingItemType == BillingItemType.Return
                                     && (x.ReturnType == ReturnType.Damage
                                      || x.ReturnType == ReturnType.Expire) ? x.Quantity : 0m),
                    MktVal = g.Sum(x => x.BillingItemType == BillingItemType.Return
                                     && (x.ReturnType == ReturnType.Damage
                                      || x.ReturnType == ReturnType.Expire) ? x.TotalPrice : 0m),
                })
                .ToListAsync(ct);

            return [.. grouped.Select(r => new RepItemSalesAgg(
                r.ProductId, r.SaleQty, r.FreeQty, r.Gross, r.ItemDisc, r.BillDisc,
                r.GoodQty, r.GoodVal, r.MktQty, r.MktVal))];
        }

        var lines = await items
            .Select(x => new
            {
                x.ProductId, x.BillingItemType, x.ReturnType, x.Quantity,
                x.TotalPrice, x.DiscountAmount, x.Billing.BillDiscountRate,
            })
            .ToListAsync(ct);

        return [.. lines
            .GroupBy(x => x.ProductId)
            .Select(g =>
            {
                var sale = g.Where(x => x.BillingItemType == BillingItemType.Sale).ToList();
                var good = g.Where(x => x.BillingItemType == BillingItemType.Return
                                     && x.ReturnType == ReturnType.MarketResell).ToList();
                var mkt  = g.Where(x => x.BillingItemType == BillingItemType.Return
                                     && (x.ReturnType == ReturnType.Damage
                                      || x.ReturnType == ReturnType.Expire)).ToList();
                return new RepItemSalesAgg(
                    g.Key,
                    sale.Sum(x => x.Quantity),
                    g.Where(x => x.BillingItemType == BillingItemType.FreeIssue).Sum(x => x.Quantity),
                    sale.Sum(x => x.TotalPrice + x.DiscountAmount),
                    sale.Sum(x => x.DiscountAmount),
                    sale.Sum(x => x.TotalPrice * x.BillDiscountRate / 100m),
                    good.Sum(x => x.Quantity), good.Sum(x => x.TotalPrice),
                    mkt.Sum(x => x.Quantity),  mkt.Sum(x => x.TotalPrice));
            })];
    }

    public async Task<Dictionary<int, (string Code, string Name)>> GetProductNamesAsync(
        IEnumerable<int> productIds, CancellationToken ct = default)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        // IgnoreQueryFilters — a since-discontinued SKU still has historical sales to label.
        var rows = await _context.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.Code, p.ItemDescription })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.Id, r => (r.Code, r.ItemDescription));
    }
}
