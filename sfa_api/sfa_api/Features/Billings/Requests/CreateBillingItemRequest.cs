namespace sfa_api.Features.Billings.Requests;

public record CreateBillingItemRequest(
    int ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountRate = 0m,
    bool IsFreeIssue = false
);
