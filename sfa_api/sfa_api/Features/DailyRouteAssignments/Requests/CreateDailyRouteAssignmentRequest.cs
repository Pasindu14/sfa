namespace sfa_api.Features.DailyRouteAssignments.Requests;

public class CreateDailyRouteAssignmentRequest
{
    public int UserId { get; set; }
    public int RouteId { get; set; }
    public DateOnly AssignedDate { get; set; }
}
