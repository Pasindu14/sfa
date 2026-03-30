using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.UserReportingLines.Entities;

public class UserReportingLine
{
    public int Id { get; set; }

    public int UserId { get; set; }          // the subordinate
    public User? User { get; set; }

    public int ReportsToUserId { get; set; } // the direct manager
    public User? ReportsToUser { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}
