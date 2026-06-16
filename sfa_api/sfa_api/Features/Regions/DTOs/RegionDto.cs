namespace sfa_api.Features.Regions.DTOs;

public record RegionDto(
    int Id,
    string Name,
    bool IsActive,
    uint RowVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record RegionListDto(
    IEnumerable<RegionDto> Regions,
    int TotalCount,
    int Page,
    int PageSize
);
