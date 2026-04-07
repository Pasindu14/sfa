using sfa_api.Common.Errors;
using sfa_api.Features.DailyRouteAssignments.DTOs;
using sfa_api.Features.DailyRouteAssignments.Entities;
using sfa_api.Features.DailyRouteAssignments.Repositories;
using sfa_api.Features.DailyRouteAssignments.Requests;

namespace sfa_api.Features.DailyRouteAssignments.Services;

public class DailyRouteAssignmentService(
    IDailyRouteAssignmentRepository repo,
    ILogger<DailyRouteAssignmentService> logger) : IDailyRouteAssignmentService
{
    private readonly IDailyRouteAssignmentRepository _repo = repo;
    private readonly ILogger<DailyRouteAssignmentService> _logger = logger;

    public async Task<DailyRouteAssignmentDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var assignment = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("DailyRouteAssignment", id);
        return MapToDto(assignment);
    }

    public async Task<DailyRouteAssignmentListDto> GetAllAsync(
        int page,
        int pageSize,
        int? userId = null,
        int? routeId = null,
        DateOnly? date = null,
        CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (items, totalCount) = await _repo.GetAllAsync(skip, pageSize, userId, routeId, date, ct);
        return new DailyRouteAssignmentListDto(
            Assignments: items.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );
    }

    public async Task<IEnumerable<RepSummaryDto>> GetMyRepsAsync(int supervisorId, CancellationToken ct = default)
    {
        var reps = await _repo.GetRepsByReportsToAsync(supervisorId, ct);
        return reps.Select(u => new RepSummaryDto(u.Id, u.Name));
    }

    public async Task<IEnumerable<RepRouteDto>> GetRepRoutesAsync(int userId, CancellationToken ct = default)
    {
        if (!await _repo.UserExistsAsync(userId, ct))
            throw new NotFoundException("User", userId);

        var routes = await _repo.GetActiveRoutesByRepIdAsync(userId, ct);
        return routes.Select(r => new RepRouteDto(r.Id, r.Name));
    }

    public async Task<DailyRouteAssignmentDto> CreateAsync(
        CreateDailyRouteAssignmentRequest request,
        int? callerId,
        CancellationToken ct = default)
    {
        if (!await _repo.UserExistsAsync(request.UserId, ct))
            throw new NotFoundException("User", request.UserId);

        if (!await _repo.RouteExistsAsync(request.RouteId, ct))
            throw new NotFoundException("Route", request.RouteId);

        // Validate: rep not already assigned on this date
        if (await _repo.IsRepAlreadyAssignedOnDateAsync(request.UserId, request.AssignedDate, ct))
            throw new BusinessRuleException(
                "REP_ALREADY_ASSIGNED",
                $"This sales rep already has a route assigned for {request.AssignedDate:yyyy-MM-dd}.");

        // Validate: route not assigned to another user on this date
        var existingRouteAssignment = await _repo.GetByRouteAndDateAsync(request.RouteId, request.AssignedDate, ct);
        if (existingRouteAssignment is not null)
            throw new BusinessRuleException(
                "ROUTE_ALREADY_ASSIGNED",
                $"This route is already assigned to {existingRouteAssignment.User?.Name ?? "another rep"} on {request.AssignedDate:yyyy-MM-dd}.");

        var now = DateTime.UtcNow;
        var entity = new DailyRouteAssignment
        {
            UserId = request.UserId,
            RouteId = request.RouteId,
            AssignedDate = request.AssignedDate,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repo.CreateAsync(entity, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "DailyRouteAssignment created: userId={UserId}, routeId={RouteId}, date={Date}",
            request.UserId, request.RouteId, request.AssignedDate);

        var created = await _repo.GetByIdAsync(entity.Id, ct)
            ?? throw new NotFoundException("DailyRouteAssignment", entity.Id);
        return MapToDto(created);
    }

    public async Task DeleteAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var assignment = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("DailyRouteAssignment", id);

        var now = DateTime.UtcNow;
        assignment.IsActive = false;
        assignment.IsDeleted = true;
        assignment.UpdatedBy = callerId;
        assignment.UpdatedAt = now;

        await _repo.UpdateAsync(assignment, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "DailyRouteAssignment {Id} deleted (userId={UserId})", id, assignment.UserId);
    }

    private static DailyRouteAssignmentDto MapToDto(DailyRouteAssignment a) => new(
        Id: a.Id,
        UserId: a.UserId,
        UserName: a.User?.Name ?? string.Empty,
        RouteId: a.RouteId,
        RouteName: a.Route?.Name ?? string.Empty,
        AssignedDate: a.AssignedDate,
        IsActive: a.IsActive,
        CreatedAt: a.CreatedAt,
        UpdatedAt: a.UpdatedAt
    );
}
