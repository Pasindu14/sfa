namespace sfa_api.Features.Areas.DTOs;

public record AreaDto(
    int Id,
    string Name,
    int RegionId,
    string RegionName,
    bool IsActive,
    uint RowVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record AreaListDto(
    IReadOnlyList<AreaDto> Areas,
    int TotalCount,
    int Page,
    int PageSize
);
