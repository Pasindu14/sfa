namespace sfa_api.Features.Stock.DTOs;

/// <summary>History list row — LineCount / TotalQuantity (pieces) summarise the lines.</summary>
public record StockTransferSummaryDto(
    int      Id,
    string   TransferNumber,
    int      SourceDistributorId,
    string   SourceDistributorName,
    int      TargetDistributorId,
    string   TargetDistributorName,
    string?  Notes,
    string   TransferredByName,
    DateTime TransferredAt,
    int      LineCount,
    decimal  TotalQuantity);

public record StockTransferDto(
    int      Id,
    string   TransferNumber,
    int      SourceDistributorId,
    string   SourceDistributorName,
    int      TargetDistributorId,
    string   TargetDistributorName,
    string?  Notes,
    string   TransferredByName,
    DateTime TransferredAt,
    int      LineCount,
    decimal  TotalQuantity,
    List<StockTransferLineDto> Lines);

public record StockTransferLineDto(
    int     Id,
    int     ProductId,
    string  ProductCode,
    string  ProductDescription,
    string  StockType,
    decimal Quantity,
    int     PiecesPerPack);
