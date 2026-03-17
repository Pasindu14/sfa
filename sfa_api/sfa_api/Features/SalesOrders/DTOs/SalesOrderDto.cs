using sfa_api.Features.SalesOrders.Enums;

namespace sfa_api.Features.SalesOrders.DTOs;

public record SalesOrderDto(
    int Id,
    string OrderNumber,
    int DistributorId,
    string DistributorName,
    SalesOrderStatus Status,
    string StatusLabel,
    string? Notes,
    IEnumerable<SalesOrderItemDto> Items,
    IEnumerable<SalesOrderHistoryDto> History,
    decimal TotalAmount,

    // Transition audit trail
    int? SubmittedBy,
    DateTime? SubmittedAt,
    int? RepApprovedBy,
    DateTime? RepApprovedAt,
    int? ManagerApprovedBy,
    DateTime? ManagerApprovedAt,
    int? FinalizedBy,
    DateTime? FinalizedAt,
    int? CancelledBy,
    DateTime? CancelledAt,
    string? CancelReason,
    int? AcknowledgedBy,
    DateTime? AcknowledgedAt,

    // Standard audit
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int? CreatedBy,
    int? UpdatedBy
);
