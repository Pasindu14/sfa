namespace sfa_api.Features.SalesOrders.Enums;

public enum SalesOrderStatus
{
    Draft = 0,
    PendingRepApproval = 1,
    PendingManagerApproval = 2,
    PendingDistributorFinalization = 3,
    Finalized = 4,
    Cancelled = 5,
    PendingDistributorAcknowledgement = 6
}
