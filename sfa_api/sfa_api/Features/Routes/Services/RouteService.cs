using sfa_api.Common.Errors;
using sfa_api.Features.Routes.DTOs;
using sfa_api.Features.Routes.Repositories;
using sfa_api.Features.Routes.Requests;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.Features.Routes.Services;

public class RouteService(
    IRouteRepository repo,
    ILogger<RouteService> logger) : IRouteService
{
    private readonly IRouteRepository _repo = repo;
    private readonly ILogger<RouteService> _logger = logger;

    public async Task<RouteDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var route = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Route", id);
        return MapToDto(route);
    }

    public async Task<RouteListDto> GetAllAsync(int page, int pageSize, bool? isActive = null, string? search = null, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (routes, totalCount) = await _repo.GetAllAsync(skip, pageSize, isActive, search, ct);
        return new RouteListDto(
            Routes: routes.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );
    }

    public async Task<IEnumerable<RouteDto>> GetAllActiveAsync(CancellationToken ct = default)
    {
        var routes = await _repo.GetAllActiveAsync(ct);
        return routes.Select(MapToDto);
    }

    public async Task<IEnumerable<RouteDto>> GetActiveByDivisionIdAsync(int divisionId, CancellationToken ct = default)
    {
        var routes = await _repo.GetActiveByDivisionIdAsync(divisionId, ct);
        return routes.Select(r => new RouteDto(
            Id: r.Id,
            Name: r.Name,
            PinColor: r.PinColor,
            Description: r.Description,
            DivisionId: r.DivisionId,
            DivisionName: string.Empty,
            TerritoryId: r.TerritoryId,
            TerritoryName: string.Empty,
            AreaId: r.AreaId,
            AreaName: string.Empty,
            RegionId: r.RegionId,
            RegionName: string.Empty,
            IsActive: r.IsActive,
            CreatedAt: r.CreatedAt,
            UpdatedAt: r.UpdatedAt
        ));
    }

    public async Task<RouteDto> CreateAsync(CreateRouteRequest request, int? callerId, CancellationToken ct = default)
    {
        var division = await _repo.GetDivisionWithAncestorsAsync(request.DivisionId, ct)
            ?? throw new NotFoundException("Division", request.DivisionId);

        if (await _repo.ExistsByNameAsync(request.Name, request.DivisionId, ct))
            throw new DuplicateResourceException("Name");

        var route = new RouteEntity
        {
            Name = request.Name,
            PinColor = request.PinColor,
            Description = request.Description,
            DivisionId = division.Id,
            TerritoryId = division.TerritoryId,
            AreaId = division.AreaId,
            RegionId = division.RegionId,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(route, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Route {RouteId} created", route.Id);

        var created = await _repo.GetByIdAsync(route.Id, ct)
            ?? throw new NotFoundException("Route", route.Id);
        return MapToDto(created);
    }

    public async Task<RouteDto> UpdateAsync(int id, UpdateRouteRequest request, int? callerId, CancellationToken ct = default)
    {
        var route = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Route", id);

        var division = await _repo.GetDivisionWithAncestorsAsync(request.DivisionId, ct)
            ?? throw new NotFoundException("Division", request.DivisionId);

        if (await _repo.ExistsByNameAsync(request.Name, request.DivisionId, id, ct))
            throw new DuplicateResourceException("Name");

        route.Name = request.Name;
        route.PinColor = request.PinColor;
        route.Description = request.Description;
        route.DivisionId = division.Id;
        route.TerritoryId = division.TerritoryId;
        route.AreaId = division.AreaId;
        route.RegionId = division.RegionId;
        route.UpdatedBy = callerId;
        route.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(route, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Route {RouteId} updated", id);

        var updated = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Route", id);
        return MapToDto(updated);
    }

    public async Task ActivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var route = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Route", id);

        route.IsActive = true;
        route.UpdatedBy = callerId;
        route.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(route, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Route {RouteId} activated", id);
    }

    public async Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var route = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Route", id);

        route.IsActive = false;
        route.UpdatedBy = callerId;
        route.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(route, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Route {RouteId} deactivated", id);
    }

    private static RouteDto MapToDto(RouteEntity r) => new(
        Id: r.Id,
        Name: r.Name,
        PinColor: r.PinColor,
        Description: r.Description,
        DivisionId: r.DivisionId,
        DivisionName: r.Division?.Name ?? string.Empty,
        TerritoryId: r.TerritoryId,
        TerritoryName: r.Territory?.Name ?? r.Division?.Territory?.Name ?? string.Empty,
        AreaId: r.AreaId,
        AreaName: r.Area?.Name ?? r.Division?.Territory?.Area?.Name ?? string.Empty,
        RegionId: r.RegionId,
        RegionName: r.Region?.Name ?? r.Division?.Territory?.Area?.Region?.Name ?? string.Empty,
        IsActive: r.IsActive,
        CreatedAt: r.CreatedAt,
        UpdatedAt: r.UpdatedAt
    );
}
