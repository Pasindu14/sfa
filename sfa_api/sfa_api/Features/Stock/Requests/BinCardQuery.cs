namespace sfa_api.Features.Stock.Requests;

/// <summary>
/// Query parameters for the distributor bin-card report.
/// DistributorId comes from the route; From/To are inclusive business dates from the query string.
/// </summary>
public record BinCardQuery(int DistributorId, DateOnly From, DateOnly To);
