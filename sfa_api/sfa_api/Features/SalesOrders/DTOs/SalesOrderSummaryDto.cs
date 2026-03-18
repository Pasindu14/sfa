using sfa_api.Features.SalesOrders.Enums;

namespace sfa_api.Features.SalesOrders.DTOs;

public record SalesOrderSummaryDto(
    int Id,
    string OrderNumber,
    int DistributorId,
    string DistributorName,
    SalesOrderStatus Status,
    string StatusLabel,
    decimal TotalAmount,
    int ItemCount,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? SubmittedAt
);

public record SalesOrderListDto(
    IEnumerable<SalesOrderSummaryDto> SalesOrders,
    int TotalCount,
    int Page,
    int PageSize
);

public record SalesOrderStatsDto(
    int PendingRepApproval,
    int PendingManagerApproval,
    int PendingAcknowledgement,
    int Finalized,
    int Total
);
