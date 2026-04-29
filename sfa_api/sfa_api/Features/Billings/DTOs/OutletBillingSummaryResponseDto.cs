namespace sfa_api.Features.Billings.DTOs;

public record OutletBillingSummaryResponseDto(
    decimal GrandTotal,
    int TotalBillingCount,
    IReadOnlyList<OutletBillingSummaryDto> OutletSummaries
);
