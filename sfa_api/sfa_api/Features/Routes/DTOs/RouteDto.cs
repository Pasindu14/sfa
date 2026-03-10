namespace sfa_api.Features.Routes.DTOs;

public record RouteDto(
    int Id,
    string Name,
    string PinColor,
    string? Description,
    int DivisionId,
    string DivisionName,
    int TerritoryId,
    string TerritoryName,
    int AreaId,
    string AreaName,
    int RegionId,
    string RegionName,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record RouteListDto(
    IEnumerable<RouteDto> Routes,
    int TotalCount,
    int Page,
    int PageSize
);
