namespace sfa_api.Features.UserGeoAssignments.Requests;

public class UpdateUserAssignmentRequest
{
    public int ReportsToUserId { get; set; }
    public int? DivisionId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
}
