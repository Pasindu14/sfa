namespace sfa_api.Features.SalesInvoices.DTOs;

public record ImportBatchErrorDto(
    string VchBillNo,
    string Reason
);

public record ImportBatchResultDto(
    int BatchId,
    string BatchNumber,
    int TotalInvoices,
    int ImportedInvoices,
    int SkippedInvoices,
    int TotalItems,
    decimal TotalAmount,
    string Status,
    IReadOnlyList<ImportBatchErrorDto> Errors
);
