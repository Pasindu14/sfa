namespace sfa_api.Features.Territories.Requests;

public class CreateTerritoryRequest
{
    public string Name { get; set; } = string.Empty;
    public int AreaId { get; set; }
}
