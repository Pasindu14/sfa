namespace sfa_api.Features.Billings.Requests;

/// <summary>
/// Distributor-side quantity adjustment on a pending bill. Only lines actually being changed need to
/// be sent. Quantities may only be reduced — the difference becomes a DistributorReturn line and is
/// credited back to distributor stock.
/// </summary>
public record AdjustBillingItemsRequest(
    List<AdjustBillingItemLine> Items,
    string? Note
);

public record AdjustBillingItemLine(
    int BillingItemId,
    decimal Quantity
);
