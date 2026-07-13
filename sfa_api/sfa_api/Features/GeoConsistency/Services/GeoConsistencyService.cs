using System.Diagnostics;
using sfa_api.Features.GeoConsistency.DTOs;
using sfa_api.Features.GeoConsistency.Entities;
using sfa_api.Features.GeoConsistency.Repositories;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.GeoConsistency.Services;

/// <summary>
/// Proves the geo hierarchy is self-consistent: every live descendant's denormalized ancestor IDs must
/// equal the ancestor derived from its parent chain. A non-zero drift count means a re-parent cascade
/// was missed somewhere — the nightly job surfaces it, and <see cref="RepairAsync"/> corrects it.
/// </summary>
public class GeoConsistencyService(
    IGeoConsistencyRepository repo,
    ICacheService cache,
    ILogger<GeoConsistencyService> logger) : IGeoConsistencyService
{
    private readonly IGeoConsistencyRepository _repo = repo;
    private readonly ICacheService _cache = cache;
    private readonly ILogger<GeoConsistencyService> _logger = logger;

    private const int PerTypeSampleCap = 50;

    public async Task<GeoConsistencyResultDto> RunAsync(string triggeredBy, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        var scan = await _repo.DetectDriftAsync(PerTypeSampleCap, ct);

        var run = new GeoConsistencyRun
        {
            RunAt       = DateTime.UtcNow,
            TriggeredBy = triggeredBy,
            RowsChecked = scan.RowsChecked,
            DriftCount  = scan.DriftCount,
            Flags       = scan.Sample,
        };
        run.DurationMs = (int)sw.ElapsedMilliseconds;

        await _repo.SaveRunAsync(run, ct);

        if (scan.DriftCount == 0)
            _logger.LogInformation(
                "Geo-consistency ({TriggeredBy}) clean: {Rows} rows checked, 0 drift in {Ms}ms",
                triggeredBy, scan.RowsChecked, run.DurationMs);
        else
            _logger.LogWarning(
                "Geo-consistency ({TriggeredBy}) found {Drift} drifted rows out of {Rows} checked — a re-parent cascade may have been missed. Run repair to correct.",
                triggeredBy, scan.DriftCount, scan.RowsChecked);

        return ToDto(run);
    }

    public async Task<GeoConsistencyResultDto?> GetLatestRunAsync(CancellationToken ct = default)
    {
        var run = await _repo.GetLatestRunAsync(ct);
        return run is null ? null : ToDto(run);
    }

    public async Task<GeoRepairResultDto> RepairAsync(CancellationToken ct = default)
    {
        var result = await _repo.RepairAsync(ct);

        if (result.TotalFixed > 0)
        {
            _logger.LogWarning(
                "Geo repair corrected {Total} drifted rows (territories={T}, divisions={D}, routes={R}, outlets={O}, distributors={Dis})",
                result.TotalFixed, result.TerritoriesFixed, result.DivisionsFixed, result.RoutesFixed, result.OutletsFixed, result.DistributorsFixed);

            // Rows changed → drop the descendants' list caches so corrected geo is served immediately.
            foreach (var prefix in GeoCacheKeys.DescendantListPrefixes)
                await _cache.RemoveByPrefixAsync(prefix, ct);
        }
        else
        {
            _logger.LogInformation("Geo repair: nothing to fix — hierarchy already consistent.");
        }

        return result;
    }

    private static GeoConsistencyResultDto ToDto(GeoConsistencyRun run) => new(
        run.Id, run.RunAt, run.TriggeredBy, run.RowsChecked, run.DriftCount,
        run.Flags.Select(f => new GeoDriftDto(f.EntityType, f.EntityId, f.Detail)).ToList());
}
