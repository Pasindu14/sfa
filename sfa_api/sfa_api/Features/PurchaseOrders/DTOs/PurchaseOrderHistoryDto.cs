using sfa_api.Features.PurchaseOrders.Enums;

namespace sfa_api.Features.PurchaseOrders.DTOs;

public record PurchaseOrderHistoryDto(
    int Id,
    string Action,
    PurchaseOrderStatus? FromStatus,
    PurchaseOrderStatus? ToStatus,
    int PerformedBy,
    string? PerformedByName,
    DateTime PerformedAt,
    string? Notes
);
