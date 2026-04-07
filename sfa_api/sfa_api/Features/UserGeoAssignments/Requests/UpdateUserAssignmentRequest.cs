namespace sfa_api.Features.UserGeoAssignments.Requests;

public class UpdateUserAssignmentRequest
{
    public int? RegionId { get; set; }
    public int? AreaId { get; set; }
    public int? TerritoryId { get; set; }
    public int? DivisionId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
}
