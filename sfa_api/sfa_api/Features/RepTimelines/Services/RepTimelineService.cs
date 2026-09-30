using sfa_api.Common.Errors;
using sfa_api.Features.LocationPings.Services;
using sfa_api.Features.RepTimelines.DTOs;
using sfa_api.Features.RepTimelines.Repositories;

namespace sfa_api.Features.RepTimelines.Services;

public interface IRepTimelineService
{
    Task<RepDayTimelineDto> GetAsync(int repId, DateOnly date, CancellationToken ct = default);
}

public class RepTimelineService(
    IRepTimelineRepository repo,
    ILocationPingService locationPings) : IRepTimelineService
{
    private readonly IRepTimelineRepository _repo = repo;
    private readonly ILocationPingService _locationPings = locationPings;

    public async Task<RepDayTimelineDto> GetAsync(int repId, DateOnly date, CancellationToken ct = default)
    {
        var repName = await _repo.GetRepNameAsync(repId, ct)
            ?? throw new NotFoundException("User", repId);

        // Sequential on purpose — one DbContext does not support concurrent queries.
        var route = await _locationPings.GetRepRouteAsync(repId, date, ct);
        var bills = await _repo.GetBillsAsync(repId, date, ct);
        var visits = await _repo.GetVisitsAsync(repId, date, ct);
        var unlocks = await _repo.GetUnlockEventsAsync(repId, date, ct);
        var assignment = await _repo.GetAssignmentAsync(repId, date, ct);
        var routeOutlets = assignment is null
            ? []
            : await _repo.GetRouteOutletsAsync(assignment.RouteId, ct);

        var (summary, events) = RepTimelineBuilder.Build(
            route.Points, route.Summary.GapThresholdMinutes,
            bills, visits, unlocks, routeOutlets, assignment);

        return new RepDayTimelineDto(
            RepId: repId,
            RepName: repName,
            Date: date,
            Assignment: assignment is null
                ? null
                : new RepTimelineAssignmentDto(assignment.RouteId, assignment.RouteName, routeOutlets.Count),
            Summary: summary,
            Events: events,
            Route: route);
    }
}
