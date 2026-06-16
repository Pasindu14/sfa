namespace sfa_api.Features.Territories.DTOs;

public record TerritoryDto(
    int Id,
    string Name,
    int AreaId,
    string AreaName,
    int RegionId,
    string RegionName,
    bool IsActive,
    uint RowVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record TerritoryListDto(
    IEnumerable<TerritoryDto> Territories,
    int TotalCount,
    int Page,
    int PageSize
);
