namespace sfa_api.Features.ProductCategoryPricings.DTOs;

/// <summary>
/// Flat per-product DTO — all 4 category prices in one row.
/// Missing prices (no DB row yet) are returned as 0.
/// </summary>
public record ProductCategoryPricingDto(
    int ProductId,
    string ProductCode,
    string ItemDescription,
    decimal PriceA,
    decimal PriceB,
    decimal PriceC,
    decimal PriceD
);
