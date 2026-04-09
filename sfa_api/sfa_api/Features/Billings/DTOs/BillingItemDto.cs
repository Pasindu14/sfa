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
    bool IsFreeIssue,
    int LineNumber
);
