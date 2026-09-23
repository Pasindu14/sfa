using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.Stock.Entities;

/// <summary>
/// Admin correction of a distributor's stock balance (active or closed distributor). Immutable once
/// written: each line's signed difference is posted to the ledger as a Correction row (In for an
/// increase, Out for a decrease) referencing this header via ReferenceType "StockAdjustment" /
/// ReferenceId = <see cref="Id"/>.
/// </summary>
public class StockAdjustment
{
    public int Id { get; set; }

    /// <summary>Human-readable number derived from the Id after insert: SA-000123</summary>
    public string AdjustmentNumber { get; set; } = string.Empty;

    public int DistributorId { get; set; }
    public StockAdjustmentReason Reason { get; set; }

    public string? Notes { get; set; }

    public int AdjustedBy { get; set; }
    public DateTime AdjustedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // ── Navigation ────────────────────────────────────────────────────────
    public Distributor Distributor { get; set; } = null!;
    public User AdjustedByUser { get; set; } = null!;
    public ICollection<StockAdjustmentLine> Lines { get; set; } = new List<StockAdjustmentLine>();
}
