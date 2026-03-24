namespace sfa_api.Features.SalesInvoices.Requests;

public record ImportSalesInvoiceItemRequest(
    string ItemErpCode,
    string ItemDescription,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal TotalPrice,
    bool IsFreeIssue,
    int LineNumber
);

public record ImportSalesInvoiceRequest(
    string VchBillNo,
    string? BusyOrderRequestNo,
    string? SfaPoNumber,
    int DistributorAlias,
    DateOnly InvoiceDate,
    string InvoiceType,
    decimal TotalAmount,
    List<ImportSalesInvoiceItemRequest> Items
);

public record ImportSalesInvoicesRequest(
    string FileName,
    List<ImportSalesInvoiceRequest> Invoices
);
