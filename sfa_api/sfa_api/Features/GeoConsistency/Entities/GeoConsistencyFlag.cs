namespace sfa_api.Features.GeoConsistency.Entities;

/// <summary>
/// A single drifted row found by a <see cref="GeoConsistencyRun"/>: its denormalized ancestor IDs
/// disagree with the ancestor derived from its parent. Capped to a sample per run (drift should be
/// zero once cascades are correct — this table exists to catch a regression, not to hold bulk data).
/// </summary>
public class GeoConsistencyFlag
{
    public int Id { get; set; }

    public int RunId { get; set; }
    public GeoConsistencyRun? Run { get; set; }

    /// <summary>The drifted entity's table: "Territory" | "Division" | "Route" | "Outlet" | "Distributor".</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Primary key of the drifted row.</summary>
    public int EntityId { get; set; }

    /// <summary>Human-readable mismatch, e.g. "RegionId 3 (self) != 5 (parent)".</summary>
    public string Detail { get; set; } = string.Empty;
}
