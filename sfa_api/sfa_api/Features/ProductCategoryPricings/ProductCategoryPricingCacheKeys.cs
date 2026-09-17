namespace sfa_api.Features.ProductCategoryPricings;

/// <summary>
/// Cache keys for the per-category distributor price list
/// (<c>GET /product-category-pricings/portal</c> and <c>/for-distributor/{id}</c>).
/// <para>
/// The key is per <b>category</b>, not per distributor: the distributor → category lookup runs
/// outside the cache on every request, so changing a distributor's category needs no invalidation.
/// </para>
/// <para>
/// The cached list reads Product (Id, Code, ItemDescription, IsActive, IsDeleted) and
/// ProductCategoryPrice (ProductId, Category, Price). Every write to either must call
/// <c>RemoveByPrefixAsync(</c><see cref="Prefix"/><c>)</c> — currently pricing BulkUpsert and product
/// create / update / activate / deactivate / delete. The prefix is a single segment, so it bumps the
/// generation of every category at once (see DistributedCacheService).
/// </para>
/// </summary>
public static class ProductCategoryPricingCacheKeys
{
    public const string Prefix = "pricing-category:";

    public static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    public static string ForCategory(string category) => $"{Prefix}{category}";
}
