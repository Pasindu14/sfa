namespace sfa_api.Features.Outlets;

/// <summary>
/// Cache keys for the company-wide active-outlet list (GET /api/v1/outlets/active).
/// Its rows carry outlet fields (incl. LastBillDate), the route name and the route's
/// Division/Territory/Area/Region names, and only outlets whose route and route ancestors are
/// active appear. So it is invalidated on every outlet write, every route write, every geo
/// write (rename / activate / deactivate / delete / re-parent cascade — via
/// <c>GeoCacheKeys.DescendantListPrefixes</c> too) and when a bill stamps LastBillDate.
/// </summary>
public static class OutletCacheKeys
{
    public const string ActivePrefix = "outlets:active:";
    public const string ActiveAll = ActivePrefix + "all";
    public static readonly TimeSpan ActiveTtl = TimeSpan.FromMinutes(10);
}
