using sfa_api.Common.Errors;
using sfa_api.Features.GeoConsistency;
using sfa_api.Features.GeoConsistency.Services;
using sfa_api.Features.Routes.DTOs;
using sfa_api.Features.Routes.Repositories;
using sfa_api.Features.Routes.Requests;
using sfa_api.Infrastructure.Caching;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.Features.Routes.Services;

public class RouteService(
    IRouteRepository repo,
    ICacheService cache,
    IGeoCascadeService cascade,
    ILogger<RouteService> logger) : IRouteService
{
    private readonly IRouteRepository _repo = repo;
    private readonly ICacheService _cache = cache;
    private readonly IGeoCascadeService _cascade = cascade;
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

        // Colour is unique per route: honour the caller's pick only if it's valid and unused,
        // otherwise auto-assign a unique colour from the curated palette (random fallback).
        var usedColors = await _repo.GetUsedPinColorsAsync(null, ct);
        var pinColor = RouteColorPalette.Resolve(request.PinColor, usedColors);

        var route = new RouteEntity
        {
            Name = request.Name,
            PinColor = pinColor,
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

        // Keep the colour unique across routes. Excluding this route's own id means an
        // unchanged colour is preserved, while a duplicate/blank pick is re-assigned.
        var usedColors = await _repo.GetUsedPinColorsAsync(id, ct);

        // Capture the pre-change parent so we only cascade on an actual division MOVE (not a rename).
        var oldDivisionId = route.DivisionId;

        route.Name = request.Name;
        route.PinColor = RouteColorPalette.Resolve(request.PinColor, usedColors);
        route.Description = request.Description;
        route.DivisionId = division.Id;
        route.TerritoryId = division.TerritoryId;
        route.AreaId = division.AreaId;
        route.RegionId = division.RegionId;
        route.UpdatedBy = callerId;
        route.UpdatedAt = DateTime.UtcNow;

        if (oldDivisionId != division.Id)
        {
            // Re-parent: the route's full geo chain is denormalized onto its live outlets. Persist the
            // move and fan the new Division/Territory/Area/Region down to those outlets atomically.
            await using var tx = await _repo.BeginTransactionAsync(ct);
            await _repo.UpdateAsync(route, ct);
            await _repo.SaveChangesAsync(ct);
            var cascaded = await _cascade.CascadeRouteDivisionChangeAsync(
                id, division.Id, division.TerritoryId, division.AreaId, division.RegionId, ct);
            await tx.CommitAsync(ct);
            _logger.LogInformation(
                "Route {RouteId} moved from Division {OldDivisionId} to {NewDivisionId}; cascaded {Count} outlets",
                id, oldDivisionId, division.Id, cascaded);
            await _cache.RemoveByPrefixAsync("outlets:route:", ct);
        }
        else
        {
            await _repo.UpdateAsync(route, ct);
            await _repo.SaveChangesAsync(ct);
            _logger.LogInformation("Route {RouteId} updated", id);
        }

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

        // Integrity guard: deactivating a route with active outlets would leave them
        // orphaned on an inactive route. Block it, same as delete.
        if (await _repo.HasActiveOutletsAsync(id, ct))
            throw new BusinessRuleException(
                "ROUTE_HAS_ACTIVE_OUTLETS",
                "Cannot deactivate a route that still has active outlets. Reassign or deactivate them first.",
                new { routeId = id });

        route.IsActive = false;
        route.UpdatedBy = callerId;
        route.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(route, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Route {RouteId} deactivated", id);
    }

    public async Task DeleteAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var route = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Route", id);

        // Integrity guard: refuse to delete a route that still has active outlets on it.
        if (await _repo.HasActiveOutletsAsync(id, ct))
            throw new BusinessRuleException(
                "ROUTE_HAS_ACTIVE_OUTLETS",
                "Cannot delete a route that still has active outlets. Reassign or deactivate them first.",
                new { routeId = id });

        // Soft-delete: IsDeleted is the audit flag for an explicit delete, distinct from
        // deactivate (IsActive = false). Never hard-delete.
        route.IsActive = false;
        route.IsDeleted = true;
        route.UpdatedBy = callerId;
        route.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(route, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Route {RouteId} deleted by {CallerId}", id, callerId);
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
