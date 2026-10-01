namespace sfa_api.Features.Billings.DTOs;

public record BillLineDto(
    int Id,
    string BillingNumber,
    DateOnly BillingDate,
    decimal TotalAmount,
    string Status,
    // Breakdown shown on every bill list: Sales (gross) − Discount − Returns = Total; FreeIssueValue is informational.
    decimal GrossAmount,
    decimal TotalDiscount,
    decimal ReturnValue,
    decimal FreeIssueValue
);
