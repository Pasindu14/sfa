namespace sfa_api.Features.GeoConsistency.DTOs;

/// <summary>Result of a geo-consistency reconciliation pass.</summary>
public record GeoConsistencyResultDto(
    int RunId,
    DateTime RunAt,
    string TriggeredBy,
    int RowsChecked,
    int DriftCount,
    IReadOnlyList<GeoDriftDto> Drifts);

/// <summary>One drifted row (sample) surfaced by a reconciliation pass.</summary>
public record GeoDriftDto(
    string EntityType,
    int EntityId,
    string Detail);

/// <summary>Result of a top-down repair (backfill) — rows corrected per level.</summary>
public record GeoRepairResultDto(
    int TerritoriesFixed,
    int DivisionsFixed,
    int RoutesFixed,
    int OutletsFixed,
    int DistributorsFixed)
{
    public int TotalFixed => TerritoriesFixed + DivisionsFixed + RoutesFixed + OutletsFixed + DistributorsFixed;
}
