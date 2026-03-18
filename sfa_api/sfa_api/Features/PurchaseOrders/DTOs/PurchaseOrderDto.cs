using sfa_api.Features.PurchaseOrders.Enums;

namespace sfa_api.Features.PurchaseOrders.DTOs;

public record PurchaseOrderDto(
    int Id,
    string OrderNumber,
    int DistributorId,
    string DistributorName,
    PurchaseOrderStatus Status,
    string StatusLabel,
    string? Notes,
    IEnumerable<PurchaseOrderItemDto> Items,
    IEnumerable<PurchaseOrderHistoryDto> History,
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
