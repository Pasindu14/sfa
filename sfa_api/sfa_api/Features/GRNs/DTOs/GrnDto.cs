namespace sfa_api.Features.GRNs.DTOs;

public record GrnDto(
    int    Id,
    string GrnNumber,
    int    SalesInvoiceId,
    string SalesInvoiceVchBillNo,
    int    DistributorId,
    string DistributorName,
    string Status,
    DateTime? ReceivedAt,
    int?   ConfirmedBy,
    string? ConfirmedByName,
    DateTime? ConfirmedAt,
    string? Notes,
    DateTime CreatedAt,
    decimal TotalAmount,
    List<GrnItemDto> Items
);

public record GrnItemDto(
    int     Id,
    int     ProductId,
    string  ProductName,
    string  ProductCode,
    decimal Quantity,
    string  Unit,
    decimal UnitPrice,
    decimal TotalPrice,
    // Units per case, from Product.PiecesPerPack. Quantity above is the raw Case count from the
    // invoice; the pieces actually credited to stock on confirm = Quantity * PiecesPerPack. 0
    // means not configured (confirming is blocked in that state — see
    // GRN_PRODUCT_MISSING_PIECES_PER_PACK).
    int     PiecesPerPack,
    string? Notes
);
