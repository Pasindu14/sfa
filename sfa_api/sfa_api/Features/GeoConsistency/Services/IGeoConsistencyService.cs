using sfa_api.Features.GeoConsistency.DTOs;

namespace sfa_api.Features.GeoConsistency.Services;

public interface IGeoConsistencyService
{
    /// <summary>Runs a read-only drift scan, persists the run (even when clean), and returns the result.</summary>
    Task<GeoConsistencyResultDto> RunAsync(string triggeredBy, CancellationToken ct = default);

    /// <summary>Returns the most recent persisted run, or null if none has run yet.</summary>
    Task<GeoConsistencyResultDto?> GetLatestRunAsync(CancellationToken ct = default);

    /// <summary>Runs the idempotent top-down backfill, invalidates descendant caches, and returns the fix counts.</summary>
    Task<GeoRepairResultDto> RepairAsync(CancellationToken ct = default);
}
