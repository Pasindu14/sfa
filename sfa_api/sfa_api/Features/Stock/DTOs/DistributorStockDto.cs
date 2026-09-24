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
    // Units per case, from Product.PiecesPerPack. QuantityOnHand is stored in pieces; callers
    // divide by this to split it into a case + piece balance. 0 means the product has no pack
    // size configured — callers should treat the whole quantity as loose pieces.
    int       PiecesPerPack,
    DateTime? LastUpdatedAt,
    int?      FleetId,
    string?   FleetName
);

/// <summary>
/// Admin stock balance row with its value at the current default pricing structure's dealer prices.
/// Prices are null when the product has no item in the default structure (value is then null too).
/// </summary>
/// <param name="StockValue">
/// QuantityOnHand (pieces) × dealer pack price — the same basis as the bin card's closing value.
/// Falls back to case price ÷ PiecesPerPack when only a case price is set.
/// </param>
public record DistributorStockBalanceDto(
    int       Id,
    int       DistributorId,
    string    DistributorName,
    int       ProductId,
    string    ProductCode,
    string    ProductDescription,
    string    StockType,
    decimal   QuantityOnHand,
    int       PiecesPerPack,
    DateTime? LastUpdatedAt,
    int?      FleetId,
    string?   FleetName,
    decimal?  DealerPackPrice,
    decimal?  DealerCasePrice,
    decimal?  StockValue
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
