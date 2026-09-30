using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.RepTimelines.DTOs;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.RepTimelines.Repositories;

public interface IRepTimelineRepository
{
    Task<string?> GetRepNameAsync(int repId, CancellationToken ct = default);
    Task<IReadOnlyList<TimelineBill>> GetBillsAsync(int repId, DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<TimelineVisit>> GetVisitsAsync(int repId, DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<TimelineUnlockEvent>> GetUnlockEventsAsync(int repId, DateOnly date, CancellationToken ct = default);
    Task<TimelineAssignment?> GetAssignmentAsync(int repId, DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<TimelineOutlet>> GetRouteOutletsAsync(int routeId, CancellationToken ct = default);
}

/// Read-only projections across features — everything the timeline needs for one rep-day,
/// and nothing more.
public class RepTimelineRepository(AppDbContext context) : IRepTimelineRepository
{
    private readonly AppDbContext _context = context;

    public async Task<string?> GetRepNameAsync(int repId, CancellationToken ct = default)
        => await _context.Users.AsNoTracking()
            .Where(u => u.Id == repId)
            .Select(u => u.Name)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<TimelineBill>> GetBillsAsync(int repId, DateOnly date, CancellationToken ct = default)
        => await _context.Billings.AsNoTracking()
            .Where(b => b.SalesRepId == repId && b.BillingDate == date)
            .Select(b => new TimelineBill(
                b.Id, b.BillingNumber, b.OutletId, b.Outlet.Name,
                b.CapturedAt, b.CreatedAt,
                b.Latitude, b.Longitude,
                b.TotalAmount, b.RepStatus == RepBillingStatus.Cancelled,
                b.DistanceFromOutletMeters, b.ProximityOverridden))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TimelineVisit>> GetVisitsAsync(int repId, DateOnly date, CancellationToken ct = default)
        => await _context.NotBillings.AsNoTracking()
            .Where(n => n.SalesRepId == repId && n.NotBillingDate == date && !n.IsDeleted)
            .Select(n => new TimelineVisit(
                n.Id, n.OutletId, n.Outlet.Name, n.Outlet.Latitude, n.Outlet.Longitude,
                n.Reason.ToString(), n.CapturedAt, n.CreatedAt))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TimelineUnlockEvent>> GetUnlockEventsAsync(int repId, DateOnly date, CancellationToken ct = default)
        => await _context.RouteUnlockRequestEvents.AsNoTracking()
            .Where(e => e.Request!.UserId == repId && e.Request.BusinessDate == date)
            .OrderBy(e => e.PerformedAt)
            .Select(e => new TimelineUnlockEvent(
                e.Action.ToString(), e.PerformedAt,
                e.PerformedByUser != null ? e.PerformedByUser.Name : null,
                e.PerformedByRole, e.Note,
                e.Request!.RequestLatitude, e.Request.RequestLongitude))
            .ToListAsync(ct);

    public async Task<TimelineAssignment?> GetAssignmentAsync(int repId, DateOnly date, CancellationToken ct = default)
        => await _context.DailyRouteAssignments.AsNoTracking()
            .Where(a => a.UserId == repId && a.AssignedDate == date && a.IsActive && !a.IsDeleted)
            .Select(a => new TimelineAssignment(a.RouteId, a.Route != null ? a.Route.Name : string.Empty))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<TimelineOutlet>> GetRouteOutletsAsync(int routeId, CancellationToken ct = default)
        => await _context.Outlets.AsNoTracking()
            .Where(o => o.RouteId == routeId && o.IsActive && !o.IsDeleted)
            .Select(o => new TimelineOutlet(o.Id, o.Name, o.Latitude, o.Longitude))
            .ToListAsync(ct);
}
