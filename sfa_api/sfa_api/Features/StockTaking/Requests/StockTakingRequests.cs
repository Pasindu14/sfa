namespace sfa_api.Features.StockTaking.Requests;

public record CreatePeriodRequest(int Month, int Year);

public record UpsertSubmissionRequest(
    int PeriodId,
    IReadOnlyList<UpsertSubmissionLineItem> Lines
);

public record UpsertSubmissionLineItem(
    int     ProductId,
    string  StockType,
    decimal CountedQuantity
);

public record AdjustLineRequest(decimal AdjustedQuantity);
