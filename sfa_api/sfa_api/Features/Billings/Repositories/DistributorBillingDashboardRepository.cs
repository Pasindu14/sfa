using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Billings.DTOs;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Billings.Repositories;

public interface IDistributorBillingDashboardRepository
{
    /// <summary>
    /// Non-deleted bills of one distributor with BillingDate in [from, to], grouped by
    /// (BillingDate, DistributorStatus, RepStatus) with COUNT and SUM(TotalAmount).
    /// </summary>
    Task<List<DistributorBillingDashboardGroupRow>> GetGroupedAsync(
        int distributorId, DateOnly from, DateOnly to, CancellationToken ct = default);
}

public class DistributorBillingDashboardRepository(AppDbContext db) : IDistributorBillingDashboardRepository
{
    public async Task<List<DistributorBillingDashboardGroupRow>> GetGroupedAsync(
        int distributorId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        // Bill's own state only: no join to Outlet/SalesRep, so a later-deactivated outlet or rep
        // never removes historical revenue (reporting convention: financial aggregates are facts).
        var query = db.Billings
            .AsNoTracking()
            .Where(b => !b.IsDeleted
                     && b.DistributorId == distributorId
                     && b.BillingDate >= from
                     && b.BillingDate <= to);

        if (db.Database.ProviderName?.Contains("Npgsql") == true)
        {
            // PostgreSQL: one GROUP BY with SUM(decimal) in SQL, at most days x 3 x 2 rows back.
            var grouped = await query
                .GroupBy(b => new { b.BillingDate, b.DistributorStatus, b.RepStatus })
                .Select(g => new
                {
                    g.Key.BillingDate,
                    g.Key.DistributorStatus,
                    g.Key.RepStatus,
                    Count = g.Count(),
                    TotalAmount = g.Sum(b => b.TotalAmount),
                })
                .ToListAsync(ct);

            return grouped
                .Select(r => new DistributorBillingDashboardGroupRow(
                    r.BillingDate, r.DistributorStatus, r.RepStatus, r.Count, r.TotalAmount))
                .ToList();
        }

        // Other providers (the SQLite test database cannot translate SUM over decimal): fetch only the
        // four needed columns and group in memory. Same rows, same result.
        var rows = await query
            .Select(b => new { b.BillingDate, b.DistributorStatus, b.RepStatus, b.TotalAmount })
            .ToListAsync(ct);

        return rows
            .GroupBy(b => new { b.BillingDate, b.DistributorStatus, b.RepStatus })
            .Select(g => new DistributorBillingDashboardGroupRow(
                g.Key.BillingDate, g.Key.DistributorStatus, g.Key.RepStatus, g.Count(), g.Sum(b => b.TotalAmount)))
            .ToList();
    }
}
