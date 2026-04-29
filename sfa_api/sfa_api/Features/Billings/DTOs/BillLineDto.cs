namespace sfa_api.Features.Billings.DTOs;

public record BillLineDto(
    int Id,
    string BillingNumber,
    DateOnly BillingDate,
    decimal TotalAmount,
    string Status
);
