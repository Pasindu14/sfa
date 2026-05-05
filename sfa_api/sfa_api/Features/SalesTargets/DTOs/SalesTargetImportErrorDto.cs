namespace sfa_api.Features.SalesTargets.DTOs;

public record SalesTargetImportErrorDto(
    int    RowIndex,
    int    RepsCode,
    string ItemCode,
    string Reason
);
