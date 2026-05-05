namespace sfa_api.Features.SalesTargets.Requests;

public record ImportSalesTargetsRequest(
    string FileName,
    int    Year,
    int    Month,
    List<TargetRowRequest> Rows
);

public record TargetRowRequest(
    int     RowIndex,
    int     RepsCode,
    string  ItemCode,
    decimal TargetQty
);
