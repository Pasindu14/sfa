using sfa_api.Features.SalesTargets.Enums;

namespace sfa_api.Features.SalesTargets.DTOs;

public record SalesTargetImportBatchDto(
    int     Id,
    string  BatchNumber,
    string  FileName,
    int     Year,
    int     Month,
    int     TotalRows,
    int     InsertedRows,
    int     UpdatedRows,
    int     SkippedRows,
    SalesTargetImportBatchStatus Status,
    int     ImportedBy,
    string  ImportedByName,
    DateTime ImportedAt
);
