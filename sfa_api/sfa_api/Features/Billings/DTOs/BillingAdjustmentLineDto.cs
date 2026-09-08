namespace sfa_api.Features.Billings.DTOs;

public record BillingAdjustmentLineDto(
    int BillingItemId,
    int ProductId,
    string ProductCode,
    string ProductDescription,
    decimal OldQuantity,
    decimal NewQuantity,
    decimal OldTotalPrice,
    decimal NewTotalPrice,
    decimal ReturnedQuantity,
    decimal ReturnValue
);
