using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Fleets.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.Stock.Entities;

/// <summary>
/// Immutable append-only ledger. Never update or delete rows.
/// Each row captures a before/after snapshot for full auditability.
/// </summary>
public class StockTransaction
{
    public int Id { get; set; }

    public int DistributorId { get; set; }
    public int ProductId { get; set; }

    /// <summary>
    /// The distributor's fleet AT THE TIME OF THIS TRANSACTION. A historical fact — frozen on write
    /// and never rewritten, so a later fleet re-assignment cannot retroactively change what the
    /// ledger says. This is why the fleet cascade touches DistributorStock but never this table.
    /// </summary>
    public int? FleetId { get; set; }

    public StockTransactionType TransactionType { get; set; }
    public StockTransactionDirection Direction { get; set; }
    public StockType StockType { get; set; } = StockType.Normal;

    /// <summary>Always stored as a positive value</summary>
    public decimal Quantity { get; set; }

    /// <summary>DistributorStock.QuantityOnHand BEFORE this transaction</summary>
    public decimal QuantityBefore { get; set; }

    /// <summary>DistributorStock.QuantityOnHand AFTER this transaction (= Before ± Quantity)</summary>
    public decimal QuantityAfter { get; set; }

    /// <summary>e.g. "GRN", "Sale"</summary>
    public string ReferenceType { get; set; } = string.Empty;

    /// <summary>ID of the source document (GrnId, SaleId, etc.)</summary>
    public int ReferenceId { get; set; }

    public DateTime TransactedAt { get; set; }
    public int TransactedBy { get; set; }

    public string? Notes { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────
    public Distributor Distributor { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public User TransactedByUser { get; set; } = null!;
    public Fleet? Fleet { get; set; }
}
