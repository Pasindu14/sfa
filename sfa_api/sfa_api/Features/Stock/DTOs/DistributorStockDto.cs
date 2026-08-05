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
