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
    DateTime UpdatedAt
);

public record SalesOrderListDto(
    IEnumerable<SalesOrderSummaryDto> SalesOrders,
    int TotalCount,
    int Page,
    int PageSize
);
