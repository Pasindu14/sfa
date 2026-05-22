namespace sfa_api.Features.ProductCategoryPricings.DTOs;

public record ProductPriceForDistributorDto(
    int ProductId,
    string ProductCode,
    string ItemDescription,
    decimal UnitPrice
);
