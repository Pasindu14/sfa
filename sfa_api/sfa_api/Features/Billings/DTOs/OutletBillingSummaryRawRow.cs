using sfa_api.Features.Billings.Enums;

namespace sfa_api.Features.Billings.DTOs;

public record OutletBillingSummaryRawRow(
    int OutletId,
    string OutletName,
    int Id,
    string BillingNumber,
    DateOnly BillingDate,
    decimal TotalAmount,
    RepBillingStatus RepStatus
);
