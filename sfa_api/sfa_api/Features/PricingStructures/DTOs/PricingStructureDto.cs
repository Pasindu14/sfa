namespace sfa_api.Features.PricingStructures.DTOs;

public record PricingStructureDto(
    int Id,
    string Name,
    string? Description,
    bool IsDefault,
    bool IsActive,
    int PricedCount,
    uint RowVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record PricingStructureListDto(
    IEnumerable<PricingStructureDto> PricingStructures,
    int TotalCount,
    int Page,
    int PageSize
);

/// <summary>
/// One grid row on the admin prices page — every non-deleted product, priced or not.
/// Null prices mean "not priced in this structure".
/// </summary>
public record PricingStructureItemRowDto(
    int ProductId,
    string ProductCode,
    string ItemDescription,
    int PiecesPerPack,
    bool IsProductActive,
    decimal? DealerPackPrice,
    decimal? DealerCasePrice,
    decimal? Mrp
);

public record PricingStructurePriceDto(
    int ProductId,
    decimal? DealerPackPrice,
    decimal? DealerCasePrice,
    decimal? Mrp
);

/// <summary>The default structure's priced items — used by back-office screens (staff PO editors).</summary>
public record DefaultPricingStructurePricesDto(
    int PricingStructureId,
    string Name,
    IReadOnlyList<PricingStructurePriceDto> Items
);
