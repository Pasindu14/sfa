namespace sfa_api.Features.Divisions.Requests;

public class UpdateDivisionRequest
{
    public string Name { get; set; } = string.Empty;
    public int TerritoryId { get; set; }

    // Optimistic concurrency token (PostgreSQL xmin) the client read on GET.
    public uint RowVersion { get; set; }
}
