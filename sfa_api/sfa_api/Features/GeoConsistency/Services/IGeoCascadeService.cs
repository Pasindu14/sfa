namespace sfa_api.Features.GeoConsistency.Services;

/// <summary>
/// Propagates a geographic re-parent down to the denormalized ancestor IDs on every LIVE descendant
/// (Territory → Division → Route → Outlet, plus Distributor off Territory). The geo hierarchy stores
/// each level's full ancestor chain (join-free reads); those copies are a cache that MUST be kept in
/// sync when a node is moved to a new parent — otherwise descendants show a stale region/area.
///
/// Deliberately does NOT touch the transactional snapshots (Billing / NotBilling / SalesTarget): those
/// freeze geo "as it was at write time" and must survive a later reorganization (reporting convention).
///
/// Each method is a set of bulk <c>ExecuteUpdate</c> statements (no rows loaded) and enlists in the
/// caller's ambient transaction (same scoped <c>AppDbContext</c>), so the parent move and the cascade
/// commit atomically. Only call when the ancestor FK actually changed — a rename needs no cascade.
/// </summary>
public interface IGeoCascadeService
{
    /// <summary>An Area moved to a new Region → refresh RegionId on all its live descendants.</summary>
    Task<int> CascadeAreaRegionChangeAsync(int areaId, int newRegionId, CancellationToken ct = default);

    /// <summary>A Territory moved to a new Area → refresh AreaId + RegionId on its live descendants.</summary>
    Task<int> CascadeTerritoryAreaChangeAsync(int territoryId, int newAreaId, int newRegionId, CancellationToken ct = default);

    /// <summary>A Division moved to a new Territory → refresh TerritoryId + AreaId + RegionId on its live descendants.</summary>
    Task<int> CascadeDivisionTerritoryChangeAsync(int divisionId, int newTerritoryId, int newAreaId, int newRegionId, CancellationToken ct = default);

    /// <summary>A Route moved to a new Division → refresh DivisionId + TerritoryId + AreaId + RegionId on its live outlets.</summary>
    Task<int> CascadeRouteDivisionChangeAsync(int routeId, int newDivisionId, int newTerritoryId, int newAreaId, int newRegionId, CancellationToken ct = default);
}
