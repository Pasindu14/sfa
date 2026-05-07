namespace sfa_api.Features.SalesInvoices.DTOs;

public record SalesInvoiceListDto(
    int     Id,
    string  VchBillNo,
    string? BusyOrderRequestNo,
    string? SfaPoNumber,
    int     DistributorId,
    string  DistributorName,
    string  InvoiceDate,   // "YYYY-MM-DD"
    string  InvoiceType,
    bool    HasFreeIssueItems,
    decimal TotalAmount,
    string  Status,
    string  BatchNumber,
    DateTime CreatedAt
);

public record SalesInvoiceDetailDto(
    int     Id,
    string  VchBillNo,
    string? BusyOrderRequestNo,
    string? SfaPoNumber,
    int?    PurchaseOrderId,
    int     DistributorId,
    string  DistributorName,
    string  InvoiceDate,
    string  InvoiceType,
    bool    HasFreeIssueItems,
    decimal TotalAmount,
    string  Status,
    int     ImportBatchId,
    string  BatchNumber,
    DateTime CreatedAt,
    List<SalesInvoiceItemDto> Items
);

public record SalesInvoiceItemDto(
    int     Id,
    int     ProductId,
    string  ProductCode,
    string  ItemErpCode,
    string  ItemDescription,
    decimal Quantity,
    string  Unit,
    decimal UnitPrice,
    decimal TotalPrice,
    bool    IsFreeIssue,
    int     LineNumber
);
