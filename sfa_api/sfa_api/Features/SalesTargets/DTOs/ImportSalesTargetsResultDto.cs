using sfa_api.Features.SalesTargets.Enums;

namespace sfa_api.Features.SalesTargets.DTOs;

public record ImportSalesTargetsResultDto(
    int    BatchId,
    string BatchNumber,
    int    Year,
    int    Month,
    int    TotalRows,
    int    InsertedRows,
    int    UpdatedRows,
    int    SkippedRows,
    SalesTargetImportBatchStatus Status,
    IEnumerable<SalesTargetImportErrorDto> Errors
);
