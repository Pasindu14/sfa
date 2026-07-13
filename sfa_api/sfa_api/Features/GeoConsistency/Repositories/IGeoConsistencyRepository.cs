using sfa_api.Features.GeoConsistency.DTOs;
using sfa_api.Features.GeoConsistency.Entities;

namespace sfa_api.Features.GeoConsistency.Repositories;

/// <summary>Outcome of a read-only drift scan: totals plus a capped sample of drifted rows.</summary>
public record GeoDriftScan(int RowsChecked, int DriftCount, List<GeoConsistencyFlag> Sample);

public interface IGeoConsistencyRepository
{
    /// <summary>
    /// Read-only pass: counts every live descendant whose denormalized ancestor IDs disagree with the
    /// ancestor derived from its parent, and returns up to <paramref name="perTypeSampleCap"/> example
    /// rows per entity type for the flag table.
    /// </summary>
    Task<GeoDriftScan> DetectDriftAsync(int perTypeSampleCap, CancellationToken ct = default);

    /// <summary>
    /// Idempotent top-down backfill: re-derives each level's ancestor IDs from its (already-corrected)
    /// parent — Territories←Areas, Divisions←Territories, Routes←Divisions, Outlets←Routes,
    /// Distributors←Territories — updating only rows that actually drift. Set-based. Never touches the
    /// frozen transaction tables (Billing / NotBilling / SalesTarget).
    /// </summary>
    Task<GeoRepairResultDto> RepairAsync(CancellationToken ct = default);

    Task SaveRunAsync(GeoConsistencyRun run, CancellationToken ct = default);
    Task<GeoConsistencyRun?> GetLatestRunAsync(CancellationToken ct = default);
    Task<int> PurgeRunsBeforeAsync(DateTime cutoff, CancellationToken ct = default);
}
