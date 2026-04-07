namespace sfa_api.Features.Areas.Requests;

public record UpdateAreaRequest(string Name, int RegionId, uint RowVersion);
