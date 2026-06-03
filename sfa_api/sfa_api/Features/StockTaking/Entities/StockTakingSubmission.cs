using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.StockTaking.Enums;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.StockTaking.Entities;

public class StockTakingSubmission
{
    public int Id { get; set; }

    public int StockTakingPeriodId { get; set; }
    public int DistributorId       { get; set; }

    public StockTakingSubmissionStatus Status { get; set; } = StockTakingSubmissionStatus.Draft;

    public DateTime? SubmittedAt { get; set; }
    public int?      SubmittedBy { get; set; }

    public bool IsActive  { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int?     CreatedBy { get; set; }
    public int?     UpdatedBy { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────
    public StockTakingPeriod  Period      { get; set; } = null!;
    public Distributor        Distributor { get; set; } = null!;
    public User?              SubmittedByUser { get; set; }
    public ICollection<StockTakingLine> Lines { get; set; } = [];
}
