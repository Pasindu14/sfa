using sfa_api.Features.PurchaseOrders.Enums;

namespace sfa_api.Features.PurchaseOrders.DTOs;

public record PurchaseOrderSummaryDto(
    int Id,
    string OrderNumber,
    int DistributorId,
    string DistributorName,
    PurchaseOrderStatus Status,
    string StatusLabel,
    decimal TotalAmount,
    int ItemCount,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? SubmittedAt
);

public record PurchaseOrderListDto(
    IEnumerable<PurchaseOrderSummaryDto> PurchaseOrders,
    int TotalCount,
    int Page,
    int PageSize
);

public record PurchaseOrderStatsDto(
    int PendingRepApproval,
    int PendingManagerApproval,
    int PendingAcknowledgement,
    int Finalized,
    int Total
);
