using sfa_api.Features.Billings.Enums;

namespace sfa_api.Features.Billings.Requests;

public record CreateBillingItemRequest(
    int ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountRate = 0m,
    BillingItemType BillingItemType = BillingItemType.Sale,
    ReturnType? ReturnType = null,
    FreeIssueSource? FreeIssueSource = null,
    DateOnly? ExpireDate = null,
    // Pricing snapshot — optional so older app builds keep working. When PricingStructureId is
    // null the line inherits the bill's structure (or the default, for builds that send neither).
    int? PricingStructureId = null,
    PriceBasis? PriceBasis = null,
    /// The structure price the line used for its basis (the exact case price on a Case line).
    decimal? ListUnitPrice = null
);
