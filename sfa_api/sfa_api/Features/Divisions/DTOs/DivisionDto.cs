namespace sfa_api.Features.Divisions.DTOs;

public record DivisionDto(
    int Id,
    string Name,
    int TerritoryId,
    string TerritoryName,
    int AreaId,
    string AreaName,
    int RegionId,
    string RegionName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record DivisionListDto(
    IEnumerable<DivisionDto> Divisions,
    int TotalCount,
    int Page,
    int PageSize
);
