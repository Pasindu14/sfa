using sfa_api.Features.SalesOrders.Enums;

namespace sfa_api.Features.SalesOrders.DTOs;

public record SalesOrderHistoryDto(
    int Id,
    string Action,
    SalesOrderStatus? FromStatus,
    SalesOrderStatus? ToStatus,
    int PerformedBy,
    string? PerformedByName,
    DateTime PerformedAt,
    string? Notes
);
