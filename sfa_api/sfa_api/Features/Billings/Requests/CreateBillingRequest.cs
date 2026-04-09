using sfa_api.Features.Billings.Enums;

namespace sfa_api.Features.Billings.Requests;

public record CreateBillingRequest(
    int OutletId,
    BillingType BillingType,
    ReturnType? ReturnType,
    int? OriginalBillingId,
    decimal BillDiscountRate,
    string? Notes,
    List<CreateBillingItemRequest> Items
);
