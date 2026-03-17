namespace sfa_api.Features.SalesOrders.DTOs;

public record SalesOrderItemDto(
    int Id,
    int ProductId,
    string ProductCode,
    string ProductDescription,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal LineTotal
);
