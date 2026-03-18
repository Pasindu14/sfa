namespace sfa_api.Features.PurchaseOrders.Enums;

public enum PurchaseOrderStatus
{
    Draft = 0,
    PendingRepApproval = 1,
    PendingManagerApproval = 2,
    PendingDistributorFinalization = 3,
    Finalized = 4,
    Cancelled = 5,
    PendingDistributorAcknowledgement = 6
}
