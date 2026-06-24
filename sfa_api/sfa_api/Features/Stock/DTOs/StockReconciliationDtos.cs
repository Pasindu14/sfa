namespace sfa_api.Features.Stock.DTOs;

/// <summary>One flagged (distributor, product, stock-type) group, enriched with names for display.</summary>
public record StockDiscrepancyDto(
    int     DistributorId,
    string  DistributorName,
    int     ProductId,
    string  ProductCode,
    string  StockType,
    string  Kind,
    decimal ExpectedQuantity,   // Σ(In) − Σ(Out)
    decimal ActualQuantity,     // observed: live balance (#1) or latest snapshot (#2)
    decimal Delta
);

/// <summary>Result of a reconciliation pass — the run summary plus every discrepancy it found.</summary>
public record StockReconciliationResultDto(
    int                                RunId,
    DateTime                           RunAt,
    string                             TriggeredBy,
    int                                GroupsChecked,
    int                                DiscrepancyCount,
    IReadOnlyList<StockDiscrepancyDto> Discrepancies
);
