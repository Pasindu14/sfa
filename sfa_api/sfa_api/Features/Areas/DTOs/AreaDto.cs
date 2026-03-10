namespace sfa_api.Features.Areas.DTOs;

public record AreaDto(
    int Id,
    string Name,
    int RegionId,
    string RegionName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record AreaListDto(
    IEnumerable<AreaDto> Areas,
    int TotalCount,
    int Page,
    int PageSize
);
