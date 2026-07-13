using Microsoft.EntityFrameworkCore;
using sfa_api.Features.GeoConsistency.DTOs;
using sfa_api.Features.GeoConsistency.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.GeoConsistency.Repositories;

/// <summary>
/// Detects and repairs drift between a live geo descendant's denormalized ancestor IDs and the ancestor
/// derived from its parent chain. Scope matches the cascade: <c>IgnoreQueryFilters()</c> + not-deleted
/// (inactive rows are checked/fixed; soft-deleted rows are ignored on both sides so they never flag).
/// </summary>
public class GeoConsistencyRepository(AppDbContext context) : IGeoConsistencyRepository
{
    private readonly AppDbContext _context = context;

    public async Task<GeoDriftScan> DetectDriftAsync(int perTypeSampleCap, CancellationToken ct = default)
    {
        var sample = new List<GeoConsistencyFlag>();
        var driftCount = 0;
        var rowsChecked = 0;

        // ---- Territory: RegionId must equal its Area's RegionId ----
        var terr = _context.Territories.IgnoreQueryFilters()
            .Where(t => !t.IsDeleted && t.Area != null);
        rowsChecked += await terr.CountAsync(ct);
        var terrDrift = terr.Where(t => t.RegionId != t.Area!.RegionId);
        driftCount += await terrDrift.CountAsync(ct);
        sample.AddRange((await terrDrift
            .OrderBy(t => t.Id).Take(perTypeSampleCap)
            .Select(t => new { t.Id, Self = t.RegionId, Parent = t.Area!.RegionId })
            .ToListAsync(ct))
            .Select(x => new GeoConsistencyFlag
            {
                EntityType = "Territory", EntityId = x.Id,
                Detail = $"RegionId {x.Self} (self) != {x.Parent} (area)"
            }));

        // ---- Division: AreaId + RegionId must equal its Territory's ----
        var div = _context.Divisions.IgnoreQueryFilters()
            .Where(d => !d.IsDeleted && d.Territory != null);
        rowsChecked += await div.CountAsync(ct);
        var divDrift = div.Where(d => d.AreaId != d.Territory!.AreaId || d.RegionId != d.Territory!.RegionId);
        driftCount += await divDrift.CountAsync(ct);
        sample.AddRange((await divDrift
            .OrderBy(d => d.Id).Take(perTypeSampleCap)
            .Select(d => new { d.Id, SelfArea = d.AreaId, ParentArea = d.Territory!.AreaId, SelfRegion = d.RegionId, ParentRegion = d.Territory!.RegionId })
            .ToListAsync(ct))
            .Select(x => new GeoConsistencyFlag
            {
                EntityType = "Division", EntityId = x.Id,
                Detail = $"AreaId {x.SelfArea}!={x.ParentArea}, RegionId {x.SelfRegion}!={x.ParentRegion} (territory)"
            }));

        // ---- Route: Territory + Area + Region must equal its Division's ----
        var route = _context.Routes.IgnoreQueryFilters()
            .Where(r => !r.IsDeleted && r.Division != null);
        rowsChecked += await route.CountAsync(ct);
        var routeDrift = route.Where(r =>
            r.TerritoryId != r.Division!.TerritoryId || r.AreaId != r.Division!.AreaId || r.RegionId != r.Division!.RegionId);
        driftCount += await routeDrift.CountAsync(ct);
        sample.AddRange((await routeDrift
            .OrderBy(r => r.Id).Take(perTypeSampleCap)
            .Select(r => new { r.Id, r.TerritoryId, PT = r.Division!.TerritoryId, r.AreaId, PA = r.Division!.AreaId, r.RegionId, PR = r.Division!.RegionId })
            .ToListAsync(ct))
            .Select(x => new GeoConsistencyFlag
            {
                EntityType = "Route", EntityId = x.Id,
                Detail = $"TerritoryId {x.TerritoryId}!={x.PT}, AreaId {x.AreaId}!={x.PA}, RegionId {x.RegionId}!={x.PR} (division)"
            }));

        // ---- Outlet: Division + Territory + Area + Region must equal its Route's ----
        var outlet = _context.Outlets.IgnoreQueryFilters()
            .Where(o => !o.IsDeleted && o.Route != null);
        rowsChecked += await outlet.CountAsync(ct);
        var outletDrift = outlet.Where(o =>
            o.DivisionId != o.Route!.DivisionId || o.TerritoryId != o.Route!.TerritoryId ||
            o.AreaId != o.Route!.AreaId || o.RegionId != o.Route!.RegionId);
        driftCount += await outletDrift.CountAsync(ct);
        sample.AddRange((await outletDrift
            .OrderBy(o => o.Id).Take(perTypeSampleCap)
            .Select(o => new { o.Id, o.DivisionId, PD = o.Route!.DivisionId, o.RegionId, PR = o.Route!.RegionId })
            .ToListAsync(ct))
            .Select(x => new GeoConsistencyFlag
            {
                EntityType = "Outlet", EntityId = x.Id,
                Detail = $"DivisionId {x.DivisionId}!={x.PD}, RegionId {x.RegionId}!={x.PR} (route)"
            }));

        // ---- Distributor: AreaId + RegionId must equal its Territory's (only when assigned) ----
        var dist = _context.Distributors.IgnoreQueryFilters()
            .Where(d => !d.IsDeleted && d.TerritoryId != null && d.Territory != null);
        rowsChecked += await dist.CountAsync(ct);
        var distDrift = dist.Where(d => d.AreaId != d.Territory!.AreaId || d.RegionId != d.Territory!.RegionId);
        driftCount += await distDrift.CountAsync(ct);
        sample.AddRange((await distDrift
            .OrderBy(d => d.Id).Take(perTypeSampleCap)
            .Select(d => new { d.Id, SelfArea = d.AreaId, ParentArea = d.Territory!.AreaId, SelfRegion = d.RegionId, ParentRegion = d.Territory!.RegionId })
            .ToListAsync(ct))
            .Select(x => new GeoConsistencyFlag
            {
                EntityType = "Distributor", EntityId = x.Id,
                Detail = $"AreaId {x.SelfArea}!={x.ParentArea}, RegionId {x.SelfRegion}!={x.ParentRegion} (territory)"
            }));

        return new GeoDriftScan(rowsChecked, driftCount, sample);
    }

    public async Task<GeoRepairResultDto> RepairAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        int territoriesFixed = 0, divisionsFixed = 0, routesFixed = 0, outletsFixed = 0, distributorsFixed = 0;

        // Re-derive per parent using constant-value ExecuteUpdate (the direct-parent FK — Territory.AreaId,
        // Division.TerritoryId, Route.DivisionId, Outlet.RouteId — is authoritative and never denormalized,
        // so it is a safe, stable selector). A cross-table navigation in the SetProperty value doesn't
        // translate on every provider; a constant per parent does, and works identically on PostgreSQL and
        // the SQLite test provider. Strictly top-down: each level is re-read AFTER its parent is corrected.

        // 1. Territories.RegionId ← Area.RegionId
        var areas = await _context.Areas.IgnoreQueryFilters().AsNoTracking()
            .Select(a => new { a.Id, a.RegionId }).ToListAsync(ct);
        foreach (var a in areas)
            territoriesFixed += await _context.Territories.IgnoreQueryFilters()
                .Where(t => !t.IsDeleted && t.AreaId == a.Id && t.RegionId != a.RegionId)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RegionId, a.RegionId).SetProperty(t => t.UpdatedAt, now), ct);

        // 2. Divisions.{AreaId,RegionId} and Distributors.{AreaId,RegionId} ← Territory (now corrected)
        var territories = await _context.Territories.IgnoreQueryFilters().AsNoTracking()
            .Select(t => new { t.Id, t.AreaId, t.RegionId }).ToListAsync(ct);
        foreach (var t in territories)
        {
            divisionsFixed += await _context.Divisions.IgnoreQueryFilters()
                .Where(d => !d.IsDeleted && d.TerritoryId == t.Id && (d.AreaId != t.AreaId || d.RegionId != t.RegionId))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.AreaId, t.AreaId)
                    .SetProperty(d => d.RegionId, t.RegionId)
                    .SetProperty(d => d.UpdatedAt, now), ct);

            distributorsFixed += await _context.Distributors.IgnoreQueryFilters()
                .Where(d => !d.IsDeleted && d.TerritoryId == t.Id && (d.AreaId != t.AreaId || d.RegionId != t.RegionId))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.AreaId, (int?)t.AreaId)
                    .SetProperty(d => d.RegionId, (int?)t.RegionId)
                    .SetProperty(d => d.UpdatedAt, now), ct);
        }

        // 3. Routes.{TerritoryId,AreaId,RegionId} ← Division (re-read after step 2)
        var divisions = await _context.Divisions.IgnoreQueryFilters().AsNoTracking()
            .Select(d => new { d.Id, d.TerritoryId, d.AreaId, d.RegionId }).ToListAsync(ct);
        foreach (var d in divisions)
            routesFixed += await _context.Routes.IgnoreQueryFilters()
                .Where(r => !r.IsDeleted && r.DivisionId == d.Id &&
                            (r.TerritoryId != d.TerritoryId || r.AreaId != d.AreaId || r.RegionId != d.RegionId))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.TerritoryId, d.TerritoryId)
                    .SetProperty(r => r.AreaId, d.AreaId)
                    .SetProperty(r => r.RegionId, d.RegionId)
                    .SetProperty(r => r.UpdatedAt, now), ct);

        // 4. Outlets.{DivisionId,TerritoryId,AreaId,RegionId} ← Route (re-read after step 3)
        var routes = await _context.Routes.IgnoreQueryFilters().AsNoTracking()
            .Select(r => new { r.Id, r.DivisionId, r.TerritoryId, r.AreaId, r.RegionId }).ToListAsync(ct);
        foreach (var r in routes)
            outletsFixed += await _context.Outlets.IgnoreQueryFilters()
                .Where(o => !o.IsDeleted && o.RouteId == r.Id &&
                            (o.DivisionId != r.DivisionId || o.TerritoryId != r.TerritoryId || o.AreaId != r.AreaId || o.RegionId != r.RegionId))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.DivisionId, r.DivisionId)
                    .SetProperty(o => o.TerritoryId, r.TerritoryId)
                    .SetProperty(o => o.AreaId, r.AreaId)
                    .SetProperty(o => o.RegionId, r.RegionId)
                    .SetProperty(o => o.UpdatedAt, now), ct);

        return new GeoRepairResultDto(territoriesFixed, divisionsFixed, routesFixed, outletsFixed, distributorsFixed);
    }

    public async Task SaveRunAsync(GeoConsistencyRun run, CancellationToken ct = default)
    {
        await _context.Set<GeoConsistencyRun>().AddAsync(run, ct);
        await _context.SaveChangesAsync(ct);
    }

    public Task<GeoConsistencyRun?> GetLatestRunAsync(CancellationToken ct = default)
        => _context.Set<GeoConsistencyRun>()
            .Include(r => r.Flags)
            .OrderByDescending(r => r.RunAt).ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync(ct);

    public async Task<int> PurgeRunsBeforeAsync(DateTime cutoff, CancellationToken ct = default)
        => await _context.Set<GeoConsistencyRun>()
            .Where(r => r.RunAt < cutoff)
            .ExecuteDeleteAsync(ct);
}
