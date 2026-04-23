using sfa_api.Features.Billings.Enums;

namespace sfa_api.Features.Billings.Requests;

public record CreateBillingItemRequest(
    int ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountRate = 0m,
    bool IsFreeIssue = false,
    BillingItemType BillingItemType = BillingItemType.Sale,
    ReturnType? ReturnType = null,
    DateOnly? ExpireDate = null
);
