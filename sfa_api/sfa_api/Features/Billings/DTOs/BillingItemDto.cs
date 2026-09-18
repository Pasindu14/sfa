using sfa_api.Features.Billings.Enums;

namespace sfa_api.Features.Billings.DTOs;

public record BillingItemDto(
    int Id,
    int ProductId,
    string ProductCode,
    string ProductDescription,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountRate,
    decimal DiscountAmount,
    decimal TotalPrice,
    BillingItemType BillingItemType,
    ReturnType? ReturnType,
    FreeIssueSource? FreeIssueSource,
    DateOnly? ExpireDate,
    int LineNumber,
    BillingItemSource Source,
    int? SourceBillingItemId,
    decimal? OriginalQuantity,

    // Pricing snapshot — frozen at billing time; null on legacy lines.
    int? PricingStructureId,
    string? PricingStructureName,
    PriceBasis? PriceBasis,
    decimal? ListUnitPrice
);
