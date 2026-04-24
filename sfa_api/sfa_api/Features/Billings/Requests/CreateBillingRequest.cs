namespace sfa_api.Features.Billings.Requests;

public record CreateBillingRequest(
    int OutletId,
    decimal BillDiscountRate,
    string? Notes,
    List<CreateBillingItemRequest> Items,
    DateOnly? BillingDate = null,
    double? Latitude = null,
    double? Longitude = null
);
