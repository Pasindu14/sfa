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
    DateOnly? ExpireDate = null
);
