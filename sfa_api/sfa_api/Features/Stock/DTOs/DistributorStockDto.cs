namespace sfa_api.Features.Stock.DTOs;

/// <param name="Id">
/// 0 for a zero-fill placeholder — an active product the distributor has never held, so no
/// <c>DistributorStock</c> row exists yet. Any other value is a real stock row.
/// </param>
/// <param name="LastUpdatedAt">Null for zero-fill placeholders — the stock was never touched.</param>
public record DistributorStockDto(
    int       Id,
    int       DistributorId,
    string    DistributorName,
    int       ProductId,
    string    ProductCode,
    string    ProductDescription,
    string    StockType,
    decimal   QuantityOnHand,
    DateTime? LastUpdatedAt,
    int?      FleetId,
    string?   FleetName
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
