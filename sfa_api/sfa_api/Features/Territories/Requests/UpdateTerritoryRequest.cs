namespace sfa_api.Features.Territories.Requests;

public class UpdateTerritoryRequest
{
    public string Name { get; set; } = string.Empty;
    public int AreaId { get; set; }

    // Optimistic concurrency token (PostgreSQL xmin) the client read on GET.
    public uint RowVersion { get; set; }
}
