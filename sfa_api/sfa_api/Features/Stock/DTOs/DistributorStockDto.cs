namespace sfa_api.Features.Stock.DTOs;

public record DistributorStockDto(
    int      Id,
    int      DistributorId,
    string   DistributorName,
    int      ProductId,
    string   ProductCode,
    string   ProductDescription,
    string   StockType,
    decimal  QuantityOnHand,
    DateTime LastUpdatedAt,
    int?     FleetId,
    string?  FleetName
);

public record StockTransactionDto(
    int      Id,
    int      ProductId,
    string   ProductCode,
    string   TransactionType,
    string   Direction,
    decimal  Quantity,
    decimal  QuantityBefore,
    decimal  QuantityAfter,
    string   ReferenceType,
    int      ReferenceId,
    DateTime TransactedAt,
    string?  Notes
);
