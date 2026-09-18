namespace sfa_api.Features.PricingStructures;

/// <summary>
/// Cache keys owned by the pricing-structure feature. Every write to a structure or its items must
/// clear every key below — including <c>mobile:products</c>,
/// whose legacy price fields are filled from the default structure for older app builds.
/// </summary>
public static class PricingStructureCacheKeys
{
    public const string DefaultPrices = "pricing-structures:default-prices";
    public const string MobileSync = "mobile:pricing-structures";
    public const string MobileProducts = "mobile:products";

    public static readonly TimeSpan DefaultPricesTtl = TimeSpan.FromMinutes(10);
}
