using sfa_api.Features.Products.Entities;
using sfa_api.Features.Stock.Enums;

namespace sfa_api.Features.Stock.Entities;

public class StockTransferLine
{
    public int Id { get; set; }

    public int StockTransferId { get; set; }
    public int ProductId { get; set; }
    public StockType StockType { get; set; } = StockType.Normal;

    /// <summary>Pieces moved — always positive.</summary>
    public decimal Quantity { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────
    public StockTransfer StockTransfer { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
