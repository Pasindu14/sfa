namespace sfa_api.Features.Stock.DTOs;

/// <summary>
/// One stock-ledger row with the names resolved for display. ReferenceNumber is the source
/// document's human-readable number (GRN, bill, ST-…, SA-…) when one exists, otherwise null.
/// </summary>
public record StockActivityDto(
    int      Id,
    DateTime TransactedAt,
    int      TransactedById,
    string   TransactedByName,
    int      DistributorId,
    string   DistributorName,
    int      ProductId,
    string   ProductCode,
    string   ProductDescription,
    int      PiecesPerPack,
    string   StockType,
    string   TransactionType,
    string   Direction,
    decimal  Quantity,
    decimal  QuantityBefore,
    decimal  QuantityAfter,
    string   ReferenceType,
    int      ReferenceId,
    string?  ReferenceNumber,
    string?  Notes);

public record StockActivityUserDto(int Id, string Name);
