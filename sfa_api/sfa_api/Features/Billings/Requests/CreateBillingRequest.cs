namespace sfa_api.Features.Billings.Requests;

public record CreateBillingRequest(
    int OutletId,
    decimal BillDiscountRate,
    string? Notes,
    List<CreateBillingItemRequest> Items,
    DateOnly? BillingDate = null,
    double? Latitude = null,
    double? Longitude = null,
    /// Accuracy radius (metres) the device reported for the position above.
    /// Optional — older app builds do not send it, and a bill must not be refused
    /// for that. Recorded so a distance can be judged against the fix that produced it.
    double? GpsAccuracyMeters = null
);
