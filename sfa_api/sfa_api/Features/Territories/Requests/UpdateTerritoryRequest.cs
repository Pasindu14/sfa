namespace sfa_api.Features.Territories.Requests;

public class UpdateTerritoryRequest
{
    public string Name { get; set; } = string.Empty;
    public int AreaId { get; set; }
}
