using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Territories.Entities;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.UserGeoAssignments.Entities;

public class UserGeoAssignment
{
    public int Id { get; set; }

    public int UserId { get; set; }          // the field user being assigned

    public int? DivisionId { get; set; }     // nullable — NSM/RSM may not map to a specific Division
    public int? TerritoryId { get; set; }    // denormalized from Division at write time
    public int? AreaId { get; set; }         // denormalized from Division at write time
    public int? RegionId { get; set; }       // denormalized from Division at write time

    public DateOnly EffectiveFrom { get; set; }

    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation
    public User? User { get; set; }
    public Division? Division { get; set; }
    public Territory? Territory { get; set; }
    public Area? Area { get; set; }
    public Region? Region { get; set; }
}
