namespace sfa_api.Features.RouteUnlockRequests.Options;

public class RouteUnlockOptions
{
    public const string SectionName = "RouteUnlock";

    /// Total requests (any outcome) a rep may raise per business day — enough to
    /// re-ask after a rejection, not enough to spam the supervisor.
    public int MaxRequestsPerDay { get; set; } = 3;
}
