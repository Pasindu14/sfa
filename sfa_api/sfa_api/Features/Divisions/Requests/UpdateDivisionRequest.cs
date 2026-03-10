namespace sfa_api.Features.Divisions.Requests;

public class UpdateDivisionRequest
{
    public string Name { get; set; } = string.Empty;
    public int TerritoryId { get; set; }
}
