using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Regions.Entities;

namespace sfa_api.Features.Territories.Entities;

public class Territory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AreaId { get; set; }         // direct parent (FK)
    public int RegionId { get; set; }       // denormalized ancestor
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Optimistic concurrency — maps to PostgreSQL xmin system column
    public uint RowVersion { get; set; }

    public Area? Area { get; set; }
    public Region? Region { get; set; }
}
