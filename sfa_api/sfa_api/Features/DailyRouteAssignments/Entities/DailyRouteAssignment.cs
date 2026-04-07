using sfa_api.Features.Users.Entities;
using RouteEntity = sfa_api.Features.Routes.Entities.Route;

namespace sfa_api.Features.DailyRouteAssignments.Entities;

public class DailyRouteAssignment
{
    public int Id { get; set; }

    public int UserId { get; set; }      // the sales rep being assigned
    public int RouteId { get; set; }     // the route to cover on that date

    public DateOnly AssignedDate { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation
    public User? User { get; set; }
    public RouteEntity? Route { get; set; }
}
