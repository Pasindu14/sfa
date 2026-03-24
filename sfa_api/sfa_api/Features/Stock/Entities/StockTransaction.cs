using sfa_api.Features.Distributors.Entities;
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

    public StockTransactionType TransactionType { get; set; }
    public StockTransactionDirection Direction { get; set; }

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
}
