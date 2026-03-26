namespace sfa_api.Features.UserGeoAssignments.Requests;

public class CreateUserAssignmentRequest
{
    public int UserId { get; set; }
    public int? RegionId { get; set; }
    public int? AreaId { get; set; }
    public int? TerritoryId { get; set; }
    public int? DivisionId { get; set; }
    public int? RouteId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
}
