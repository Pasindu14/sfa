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
    // DEPRECATED — legacy fields for app builds that predate pricing structures. Filled from the
    // DEFAULT pricing structure (0 when unpriced). New builds read /mobile/pricing-structures.
    // Remove in the release-2 cleanup once every device runs the new build.
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

/// <summary>An active pricing structure with its priced products, for the rep's offline price lists.</summary>
public record MobilePricingStructureDto(
    int Id,
    string Name,
    bool IsDefault,
    List<MobilePricingItemDto> Items
);

/// <summary>Only priced items are sent — a product absent here is "No price" in that structure.</summary>
public record MobilePricingItemDto(
    int ProductId,
    decimal DealerPackPrice,
    decimal? DealerCasePrice,
    decimal? Mrp
);

public record MobilePricingStructureListDto(
    List<MobilePricingStructureDto> PricingStructures,
    int TotalCount,
    DateTime CachedAt
);
