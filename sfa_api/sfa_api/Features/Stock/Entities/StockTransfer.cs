using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.Stock.Entities;

/// <summary>
/// Admin move of a closed (inactive) distributor's remaining stock into an active distributor.
/// Immutable once written: the source goes Out (TransferOut ledger rows) and the target goes In
/// (TransferIn ledger rows) in the same transaction, each ledger row referencing this header via
/// ReferenceType "StockTransfer" / ReferenceId = <see cref="Id"/>.
/// </summary>
public class StockTransfer
{
    public int Id { get; set; }

    /// <summary>Human-readable number derived from the Id after insert: ST-000123</summary>
    public string TransferNumber { get; set; } = string.Empty;

    public int SourceDistributorId { get; set; }
    public int TargetDistributorId { get; set; }

    public string? Notes { get; set; }

    public int TransferredBy { get; set; }
    public DateTime TransferredAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // ── Navigation ────────────────────────────────────────────────────────
    public Distributor SourceDistributor { get; set; } = null!;
    public Distributor TargetDistributor { get; set; } = null!;
    public User TransferredByUser { get; set; } = null!;
    public ICollection<StockTransferLine> Lines { get; set; } = new List<StockTransferLine>();
}
