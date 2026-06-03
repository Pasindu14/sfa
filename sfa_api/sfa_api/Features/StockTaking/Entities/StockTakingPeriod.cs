using sfa_api.Features.StockTaking.Enums;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.StockTaking.Entities;

public class StockTakingPeriod
{
    public int Id { get; set; }

    public int Month { get; set; }
    public int Year  { get; set; }

    public StockTakingPeriodStatus Status { get; set; } = StockTakingPeriodStatus.Open;

    public DateTime? LockedAt { get; set; }
    public int?      LockedBy { get; set; }

    public bool IsActive  { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int?     CreatedBy { get; set; }
    public int?     UpdatedBy { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────
    public User? LockedByUser { get; set; }
    public ICollection<StockTakingSubmission> Submissions { get; set; } = [];
}
