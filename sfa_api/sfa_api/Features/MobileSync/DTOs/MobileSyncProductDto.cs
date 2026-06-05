namespace sfa_api.Features.MobileSync.DTOs;

public record MobileSyncProductDto(
    int Id,
    string Code,
    string ItemDescription,
    string? PrintDescription,
    int PiecesPerPack,
    string? ImageUrl,
    int? CategoryId,
    string? CategoryName,
    decimal DealerPackPrice,
    decimal DealerCasePrice,
    decimal Mrp
);

public record MobileProductListDto(
    List<MobileSyncProductDto> Products,
    int TotalCount,
    DateTime CachedAt
);

public record MobileProductCategoryDto(
    int Id,
    string Name
);

public record MobileProductCategoryListDto(
    List<MobileProductCategoryDto> Categories,
    int TotalCount,
    DateTime CachedAt
);
