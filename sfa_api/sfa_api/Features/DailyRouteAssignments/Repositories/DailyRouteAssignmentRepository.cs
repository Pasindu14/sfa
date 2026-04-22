using Microsoft.EntityFrameworkCore;
using sfa_api.Features.DailyRouteAssignments.Entities;
using sfa_api.Features.DailyRouteAssignments.Enums;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.Features.DailyRouteAssignments.Repositories;

public class DailyRouteAssignmentRepository(AppDbContext context) : IDailyRouteAssignmentRepository
{
    private readonly AppDbContext _context = context;

    public async Task<DailyRouteAssignment?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.DailyRouteAssignments
            .Include(a => a.User)
            .Include(a => a.Route)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<(IEnumerable<DailyRouteAssignment>, int)> GetAllAsync(
        int skip,
        int take,
        int? userId = null,
        int? routeId = null,
        DateOnly? date = null,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);

        var query = _context.DailyRouteAssignments
            .Include(a => a.User)
            .Include(a => a.Route)
            .Where(a => a.IsActive && !a.IsDeleted)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (routeId.HasValue)
            query = query.Where(a => a.RouteId == routeId.Value);

        if (date.HasValue)
            query = query.Where(a => a.AssignedDate == date.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .AsNoTracking()
            .OrderByDescending(a => a.AssignedDate)
            .ThenBy(a => a.User!.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IEnumerable<User>> GetRepsByReportsToAsync(int supervisorId, CancellationToken ct = default)
        => await _context.UserReportingLines
            .Include(rl => rl.User)
            .Where(rl => rl.ReportsToUserId == supervisorId
                      && rl.IsActive
                      && !rl.IsDeleted
                      && rl.User!.Role == UserRole.SalesRep
                      && !rl.User.IsDeleted)
            .Select(rl => rl.User!)
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .Take(500)
            .ToListAsync(ct);

    public async Task<IEnumerable<RouteEntity>> GetActiveRoutesByRepIdAsync(int userId, CancellationToken ct = default)
    {
        // Find rep's active geo assignment to get their division
        var geoAssignment = await _context.UserGeoAssignments
            .Where(g => g.UserId == userId && g.IsActive && !g.IsDeleted)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (geoAssignment?.DivisionId is null)
            return [];

        return await _context.Routes
            .IgnoreQueryFilters()
            .Where(r => r.IsActive && !r.IsDeleted && r.DivisionId == geoAssignment.DivisionId)
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Take(500)
            .ToListAsync(ct);
    }

    public async Task<bool> IsRepAlreadyAssignedOnDateAsync(int userId, DateOnly date, CancellationToken ct = default)
        => await _context.DailyRouteAssignments
            .AnyAsync(a => a.UserId == userId && a.AssignedDate == date && a.IsActive && !a.IsDeleted, ct);

    public async Task<DailyRouteAssignment?> GetByRouteAndDateAsync(int routeId, DateOnly date, CancellationToken ct = default)
        => await _context.DailyRouteAssignments
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.RouteId == routeId && a.AssignedDate == date && a.IsActive && !a.IsDeleted, ct);

    public async Task<bool> UserExistsAsync(int userId, CancellationToken ct = default)
        => await _context.Users.AnyAsync(u => u.Id == userId && !u.IsDeleted, ct);

    public async Task<bool> RouteExistsAsync(int routeId, CancellationToken ct = default)
        => await _context.Routes.IgnoreQueryFilters().AnyAsync(r => r.Id == routeId && r.IsActive, ct);

    public async Task<(IEnumerable<DailyRouteAssignment>, int)> GetPendingDeletionsAsync(
        int skip,
        int take,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);

        var query = _context.DailyRouteAssignments
            .Include(a => a.User)
            .Include(a => a.Route)
            .Where(a => a.DeletionStatus == DailyRouteAssignmentDeletionStatus.PendingApproval && !a.IsDeleted)
            .AsQueryable();

        var total = await query.CountAsync(ct);
        var items = await query
            .AsNoTracking()
            .OrderBy(a => a.DeletionRequestedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<DailyRouteAssignment?> GetActiveTodayAssignmentForRepAsync(int repId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _context.DailyRouteAssignments
            .Include(a => a.Route)
            .Where(a => a.UserId == repId && a.AssignedDate == today && a.IsActive && !a.IsDeleted)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
    }

    public async Task CreateAsync(DailyRouteAssignment entity, CancellationToken ct = default)
        => await _context.DailyRouteAssignments.AddAsync(entity, ct);

    public Task UpdateAsync(DailyRouteAssignment entity, CancellationToken ct = default)
    {
        _context.DailyRouteAssignments.Update(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
