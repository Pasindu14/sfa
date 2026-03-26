namespace sfa_api.Features.UserGeoAssignments.Requests;

public class CreateUserAssignmentRequest
{
    public int UserId { get; set; }
    public int ReportsToUserId { get; set; }
    public int? DivisionId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
}
