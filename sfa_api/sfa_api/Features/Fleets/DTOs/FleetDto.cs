namespace sfa_api.Features.Fleets.DTOs;

public record FleetDto(
    int Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record FleetListDto(
    IEnumerable<FleetDto> Fleets,
    int TotalCount,
    int Page,
    int PageSize
);
