namespace sfa_api.Features.GRNs.DTOs;

public record GrnDto(
    int    Id,
    string GrnNumber,
    int    SalesInvoiceId,
    string SalesInvoiceVchBillNo,
    int    DistributorId,
    string DistributorName,
    string Status,
    DateTime? ReceivedAt,
    int?   ConfirmedBy,
    string? ConfirmedByName,
    DateTime? ConfirmedAt,
    string? Notes,
    DateTime CreatedAt,
    List<GrnItemDto> Items
);

public record GrnItemDto(
    int     Id,
    int     ProductId,
    string  ProductName,
    string  ProductCode,
    decimal Quantity,
    string  Unit,
    string? Notes
);
