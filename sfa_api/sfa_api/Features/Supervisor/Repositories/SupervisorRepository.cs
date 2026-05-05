using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Supervisor.Repositories;

public class SupervisorRepository(AppDbContext context) : ISupervisorRepository
{
    private readonly AppDbContext _context = context;

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
}
