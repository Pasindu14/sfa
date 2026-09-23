using sfa_api.Features.Products.Entities;
using sfa_api.Features.Stock.Enums;

namespace sfa_api.Features.Stock.Entities;

public class StockAdjustmentLine
{
    public int Id { get; set; }

    public int StockAdjustmentId { get; set; }
    public int ProductId { get; set; }
    public StockType StockType { get; set; } = StockType.Normal;

    /// <summary>Locked balance (pieces) at the time of the adjustment — 0 when no stock row existed.</summary>
    public decimal QuantityBefore { get; set; }

    /// <summary>Balance (pieces) the admin set.</summary>
    public decimal NewQuantity { get; set; }

    /// <summary>NewQuantity − QuantityBefore; never 0 (unchanged lines are not stored).</summary>
    public decimal Difference { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────
    public StockAdjustment StockAdjustment { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
