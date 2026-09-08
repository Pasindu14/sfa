namespace sfa_api.Features.Billings.DTOs;

public record BillingAdjustmentDto(
    int Id,
    int AdjustedByUserId,
    string AdjustedByName,
    DateTime AdjustedAt,
    string? Note,
    decimal OldTotalAmount,
    decimal NewTotalAmount,
    List<BillingAdjustmentLineDto> Lines
);
