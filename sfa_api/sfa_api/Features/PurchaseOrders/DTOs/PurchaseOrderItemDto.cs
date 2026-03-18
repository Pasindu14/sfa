namespace sfa_api.Features.PurchaseOrders.DTOs;

public record PurchaseOrderItemDto(
    int Id,
    int ProductId,
    string ProductCode,
    string ProductDescription,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal LineTotal
);
