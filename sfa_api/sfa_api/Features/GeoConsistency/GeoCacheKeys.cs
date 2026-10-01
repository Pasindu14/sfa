namespace sfa_api.Features.GeoConsistency;

/// <summary>
/// List-cache key prefixes for the live geo descendants. After a re-parent cascade rewrites their
/// denormalized ancestor IDs, these caches are cleared so a stale region/area can't survive in a
/// cached list. Over-invalidation is deliberate and harmless — a re-parent is a rare admin event, so
/// clearing the whole descendant set (rather than computing the exact affected subset) just costs a
/// few cache misses. Routes are intentionally absent: the Routes feature has no read cache.
/// </summary>
public static class GeoCacheKeys
{
    public static readonly string[] DescendantListPrefixes =
    [
        "territories:list:",
        "divisions:list:",
        "distributors:list:",
        "outlets:route:",
    ];
}
