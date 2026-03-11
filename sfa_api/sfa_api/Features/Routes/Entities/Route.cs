using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Territories.Entities;

namespace sfa_api.Features.Routes.Entities;

public class Route
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PinColor { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Ancestor IDs (denormalized)
    public int DivisionId { get; set; }   // direct parent (FK)
    public int TerritoryId { get; set; }  // denormalized
    public int AreaId { get; set; }       // denormalized
    public int RegionId { get; set; }     // denormalized

    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation
    public Division? Division { get; set; }
    public Territory? Territory { get; set; }
    public Area? Area { get; set; }
    public Region? Region { get; set; }
}
