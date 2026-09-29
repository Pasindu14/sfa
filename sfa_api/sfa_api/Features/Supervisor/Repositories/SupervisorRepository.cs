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
}
