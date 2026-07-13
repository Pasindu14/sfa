using Microsoft.EntityFrameworkCore;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.GeoConsistency.Services;

/// <summary>
/// Set-based implementation of the geo re-parent cascade. See <see cref="IGeoCascadeService"/>.
///
/// Selector rule: filter descendants by the ancestor key that DID NOT change in the move (e.g. when an
/// Area moves region, every descendant still has the same AreaId), then set the id(s) that did. Because
/// each level stores the FULL ancestor set, the cascade is flat — one UPDATE per table, no tree walk.
///
/// Scope: <c>IgnoreQueryFilters()</c> so inactive rows are corrected too (a reactivated row must be
/// consistent), but soft-deleted rows are skipped — the reconciliation check likewise ignores deleted
/// rows, so the two agree. UpdatedAt is bumped so mobile delta-sync re-pulls the corrected outlets.
/// </summary>
public class GeoCascadeService(AppDbContext context) : IGeoCascadeService
{
    private readonly AppDbContext _context = context;

    public async Task<int> CascadeAreaRegionChangeAsync(int areaId, int newRegionId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var affected = 0;

        affected += await _context.Territories.IgnoreQueryFilters()
            .Where(t => !t.IsDeleted && t.AreaId == areaId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RegionId, newRegionId)
                .SetProperty(t => t.UpdatedAt, now), ct);

        affected += await _context.Divisions.IgnoreQueryFilters()
            .Where(d => !d.IsDeleted && d.AreaId == areaId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.RegionId, newRegionId)
                .SetProperty(d => d.UpdatedAt, now), ct);

        affected += await _context.Routes.IgnoreQueryFilters()
            .Where(r => !r.IsDeleted && r.AreaId == areaId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.RegionId, newRegionId)
                .SetProperty(r => r.UpdatedAt, now), ct);

        affected += await _context.Outlets.IgnoreQueryFilters()
            .Where(o => !o.IsDeleted && o.AreaId == areaId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.RegionId, newRegionId)
                .SetProperty(o => o.UpdatedAt, now), ct);

        affected += await _context.Distributors.IgnoreQueryFilters()
            .Where(d => !d.IsDeleted && d.AreaId == areaId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.RegionId, (int?)newRegionId)
                .SetProperty(d => d.UpdatedAt, now), ct);

        return affected;
    }

    public async Task<int> CascadeTerritoryAreaChangeAsync(int territoryId, int newAreaId, int newRegionId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var affected = 0;

        affected += await _context.Divisions.IgnoreQueryFilters()
            .Where(d => !d.IsDeleted && d.TerritoryId == territoryId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.AreaId, newAreaId)
                .SetProperty(d => d.RegionId, newRegionId)
                .SetProperty(d => d.UpdatedAt, now), ct);

        affected += await _context.Routes.IgnoreQueryFilters()
            .Where(r => !r.IsDeleted && r.TerritoryId == territoryId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.AreaId, newAreaId)
                .SetProperty(r => r.RegionId, newRegionId)
                .SetProperty(r => r.UpdatedAt, now), ct);

        affected += await _context.Outlets.IgnoreQueryFilters()
            .Where(o => !o.IsDeleted && o.TerritoryId == territoryId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.AreaId, newAreaId)
                .SetProperty(o => o.RegionId, newRegionId)
                .SetProperty(o => o.UpdatedAt, now), ct);

        affected += await _context.Distributors.IgnoreQueryFilters()
            .Where(d => !d.IsDeleted && d.TerritoryId == territoryId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.AreaId, (int?)newAreaId)
                .SetProperty(d => d.RegionId, (int?)newRegionId)
                .SetProperty(d => d.UpdatedAt, now), ct);

        return affected;
    }

    public async Task<int> CascadeDivisionTerritoryChangeAsync(int divisionId, int newTerritoryId, int newAreaId, int newRegionId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var affected = 0;

        affected += await _context.Routes.IgnoreQueryFilters()
            .Where(r => !r.IsDeleted && r.DivisionId == divisionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.TerritoryId, newTerritoryId)
                .SetProperty(r => r.AreaId, newAreaId)
                .SetProperty(r => r.RegionId, newRegionId)
                .SetProperty(r => r.UpdatedAt, now), ct);

        affected += await _context.Outlets.IgnoreQueryFilters()
            .Where(o => !o.IsDeleted && o.DivisionId == divisionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.TerritoryId, newTerritoryId)
                .SetProperty(o => o.AreaId, newAreaId)
                .SetProperty(o => o.RegionId, newRegionId)
                .SetProperty(o => o.UpdatedAt, now), ct);

        return affected;
    }

    public async Task<int> CascadeRouteDivisionChangeAsync(int routeId, int newDivisionId, int newTerritoryId, int newAreaId, int newRegionId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        return await _context.Outlets.IgnoreQueryFilters()
            .Where(o => !o.IsDeleted && o.RouteId == routeId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.DivisionId, newDivisionId)
                .SetProperty(o => o.TerritoryId, newTerritoryId)
                .SetProperty(o => o.AreaId, newAreaId)
                .SetProperty(o => o.RegionId, newRegionId)
                .SetProperty(o => o.UpdatedAt, now), ct);
    }
}
