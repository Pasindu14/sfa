namespace sfa_api.Features.Billings.DTOs;

public record OutletBillingSummaryDto(
    int OutletId,
    string OutletName,
    int BillingCount,
    decimal TotalAmount,
    IReadOnlyList<BillLineDto> Bills
);
