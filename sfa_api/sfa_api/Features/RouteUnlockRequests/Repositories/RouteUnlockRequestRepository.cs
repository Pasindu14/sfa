using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Errors;
using sfa_api.Features.RouteUnlockRequests.DTOs;
using sfa_api.Features.RouteUnlockRequests.Entities;
using sfa_api.Features.RouteUnlockRequests.Requests;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.RouteUnlockRequests.Repositories;

public class RouteUnlockRequestRepository(AppDbContext context) : IRouteUnlockRequestRepository
{
    private readonly AppDbContext _context = context;

    public async Task<RouteUnlockRequest?> GetForUpdateAsync(int id, CancellationToken ct = default)
        => await _context.RouteUnlockRequests.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<RouteUnlockRequest?> GetWithNamesAsync(int id, CancellationToken ct = default)
        => await WithNames(_context.RouteUnlockRequests.AsNoTracking())
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<RouteUnlockRequest?> GetEffectiveAsync(
        int userId, int routeId, DateTime atUtc, CancellationToken ct = default)
        => await _context.RouteUnlockRequests
            .AsNoTracking()
            .Where(x => x.UserId == userId
                     && x.RouteId == routeId
                     && x.Status == RouteUnlockStatus.Approved
                     && x.ValidFrom <= atUtc
                     && x.ValidTo > atUtc)
            .OrderByDescending(x => x.ValidTo)
            .FirstOrDefaultAsync(ct);

    public async Task<RouteUnlockRequest?> GetLatestForRepOnDateAsync(
        int userId, DateOnly date, CancellationToken ct = default)
        => await WithNames(_context.RouteUnlockRequests.AsNoTracking())
            .Where(x => x.UserId == userId && x.BusinessDate == date)
            .OrderByDescending(x => x.RequestedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<int> CountForRepOnDateAsync(int userId, DateOnly date, CancellationToken ct = default)
        => await _context.RouteUnlockRequests
            .CountAsync(x => x.UserId == userId && x.BusinessDate == date, ct);

    public async Task<bool> HasOpenForRepOnDateAsync(int userId, DateOnly date, CancellationToken ct = default)
        => await _context.RouteUnlockRequests
            .AnyAsync(x => x.UserId == userId
                        && x.BusinessDate == date
                        && (x.Status == RouteUnlockStatus.Pending || x.Status == RouteUnlockStatus.Approved), ct);

    public async Task<(IReadOnlyList<RouteUnlockRequest> Items, int TotalCount)> GetPagedAsync(
        RouteUnlockListQuery q, IReadOnlyCollection<int>? scopeUserIds,
        DateOnly today, DateTime nowUtc, int skip, int take, CancellationToken ct = default)
    {
        var query = _context.RouteUnlockRequests.AsNoTracking();

        if (scopeUserIds is not null)
            query = query.Where(x => scopeUserIds.Contains(x.UserId));

        if (q.From is { } from) query = query.Where(x => x.BusinessDate >= from);
        if (q.To is { } to) query = query.Where(x => x.BusinessDate <= to);

        // The filter speaks effectiveStatus: Pending/Approved exclude rows the
        // clock has already expired, and Expired is derived exactly as the DTO
        // derives it.
        query = q.Status switch
        {
            "Pending" => query.Where(x => x.Status == RouteUnlockStatus.Pending && x.BusinessDate >= today),
            "Approved" => query.Where(x => x.Status == RouteUnlockStatus.Approved && x.ValidTo > nowUtc),
            "Expired" => query.Where(x =>
                (x.Status == RouteUnlockStatus.Pending && x.BusinessDate < today)
                || (x.Status == RouteUnlockStatus.Approved && x.ValidTo <= nowUtc)),
            "Rejected" => query.Where(x => x.Status == RouteUnlockStatus.Rejected),
            "Cancelled" => query.Where(x => x.Status == RouteUnlockStatus.Cancelled),
            "Revoked" => query.Where(x => x.Status == RouteUnlockStatus.Revoked),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim().ToLower();
            query = query.Where(x =>
                (x.User != null && (x.User.Name.ToLower().Contains(term) || x.User.Username.ToLower().Contains(term)))
                || (x.Route != null && x.Route.Name.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await WithNames(query)
            .OrderByDescending(x => x.RequestedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<int> CountPendingAsync(
        IReadOnlyCollection<int>? scopeUserIds, DateOnly today, CancellationToken ct = default)
    {
        var query = _context.RouteUnlockRequests
            .Where(x => x.Status == RouteUnlockStatus.Pending && x.BusinessDate >= today);
        if (scopeUserIds is not null)
            query = query.Where(x => scopeUserIds.Contains(x.UserId));
        return await query.CountAsync(ct);
    }

    public async Task<IReadOnlyList<RouteUnlockRequestEvent>> GetEventsAsync(int requestId, CancellationToken ct = default)
        => await _context.RouteUnlockRequestEvents
            .AsNoTracking()
            .Include(e => e.PerformedByUser)
            .Where(e => e.RouteUnlockRequestId == requestId)
            .OrderBy(e => e.PerformedAt).ThenBy(e => e.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<RouteUnlockBillDto>> GetBillsAsync(int requestId, CancellationToken ct = default)
        => await _context.Billings
            .AsNoTracking()
            .Where(b => b.RouteUnlockRequestId == requestId)
            .OrderBy(b => b.CreatedAt)
            .Select(b => new RouteUnlockBillDto(
                b.Id, b.BillingNumber, b.BillingDate, b.OutletId, b.Outlet.Name,
                b.DistanceFromOutletMeters, b.TotalAmount, b.CreatedAt))
            .ToListAsync(ct);

    public async Task AddAsync(RouteUnlockRequest entity, CancellationToken ct = default)
        => await _context.RouteUnlockRequests.AddAsync(entity, ct);

    public void ApplyConcurrencyToken(RouteUnlockRequest entity, uint rowVersion)
        => _context.Entry(entity).Property(x => x.RowVersion).OriginalValue = rowVersion;

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }
    }

    private static IQueryable<RouteUnlockRequest> WithNames(IQueryable<RouteUnlockRequest> q)
        => q.Include(x => x.User)
            .Include(x => x.Route)
            .Include(x => x.SupervisorUser)
            .Include(x => x.ReviewedByUser)
            .Include(x => x.RevokedByUser);
}
