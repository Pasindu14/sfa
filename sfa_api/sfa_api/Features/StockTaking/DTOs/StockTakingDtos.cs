namespace sfa_api.Features.StockTaking.DTOs;

public record StockTakingPeriodDto(
    int      Id,
    int      Month,
    int      Year,
    string   Status,
    DateTime? LockedAt,
    int?     LockedBy,
    string?  LockedByName,
    bool     IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record StockTakingSubmissionDto(
    int      Id,
    int      StockTakingPeriodId,
    int      Month,
    int      Year,
    int      DistributorId,
    string   DistributorName,
    string   Status,
    DateTime? SubmittedAt,
    int?     SubmittedBy,
    string?  SubmittedByName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<StockTakingLineDto> Lines
);

public record StockTakingLineDto(
    int      Id,
    int      StockTakingSubmissionId,
    int      ProductId,
    string   ProductCode,
    string   ProductDescription,
    string   StockType,
    decimal  CountedQuantity,
    decimal  SystemQuantity,
    decimal  Variance,
    bool     IsAdjusted,
    decimal? AdjustedQuantity,
    int?     AdjustedBy,
    string?  AdjustedByName,
    DateTime? AdjustedAt
);
