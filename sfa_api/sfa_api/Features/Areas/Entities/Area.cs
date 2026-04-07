using sfa_api.Features.Regions.Entities;

namespace sfa_api.Features.Areas.Entities;

public class Area
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Optimistic concurrency — maps to PostgreSQL xmin system column
    public uint RowVersion { get; set; }

    // Navigation
    public Region? Region { get; set; }
}
