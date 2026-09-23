namespace sfa_api.Features.Stock.DTOs;

/// <summary>History list row — TotalIncrease / TotalDecrease (pieces) are both positive magnitudes.</summary>
public record StockAdjustmentSummaryDto(
    int      Id,
    string   AdjustmentNumber,
    int      DistributorId,
    string   DistributorName,
    string   Reason,
    string?  Notes,
    string   AdjustedByName,
    DateTime AdjustedAt,
    int      LineCount,
    decimal  TotalIncrease,
    decimal  TotalDecrease);

public record StockAdjustmentDto(
    int      Id,
    string   AdjustmentNumber,
    int      DistributorId,
    string   DistributorName,
    string   Reason,
    string?  Notes,
    string   AdjustedByName,
    DateTime AdjustedAt,
    int      LineCount,
    decimal  TotalIncrease,
    decimal  TotalDecrease,
    List<StockAdjustmentLineDto> Lines);

public record StockAdjustmentLineDto(
    int     Id,
    int     ProductId,
    string  ProductCode,
    string  ProductDescription,
    string  StockType,
    decimal QuantityBefore,
    decimal NewQuantity,
    decimal Difference,
    int     PiecesPerPack);
