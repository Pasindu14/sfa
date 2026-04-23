namespace sfa_api.Features.NotBillings.Enums;

public enum NotBillingReason
{
    OutletClosed,   // Shop physically closed on visit
    OwnerAbsent,    // Open but decision-maker unavailable
    CreditIssue,    // Outstanding payment / credit dispute
    NoOrder,        // Owner present, sufficient stock, placed no order
    OutOfStock      // Outlet's own stock depleted, no purchase needed
}
