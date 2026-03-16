namespace sfa_api.Features.PricingStructures.DTOs;

public record PricingStructureDto(
    int Id,
    string Name,
    string? Description,
    bool IsDefault,
    bool IsActive,
    int ItemCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record PricingStructureDetailDto(
    int Id,
    string Name,
    string? Description,
    bool IsDefault,
    bool IsActive,
    int ItemCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IEnumerable<PricingStructureItemDto> Items
);

public record PricingStructureItemDto(
    int Id,
    int PricingStructureId,
    int ProductId,
    string ProductCode,
    string ProductItemDescription,
    decimal UnitPrice,
    decimal? PackPrice
);

public record PricingStructureListDto(
    IEnumerable<PricingStructureDto> PricingStructures,
    int TotalCount,
    int Page,
    int PageSize
);
