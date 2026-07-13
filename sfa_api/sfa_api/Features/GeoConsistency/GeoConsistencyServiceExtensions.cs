using sfa_api.Features.GeoConsistency.Repositories;
using sfa_api.Features.GeoConsistency.Services;

namespace sfa_api.Features.GeoConsistency;

public static class GeoConsistencyServiceExtensions
{
    public static IServiceCollection AddGeoConsistencyFeature(this IServiceCollection services)
    {
        // Cascade — consumed by Area/Territory/Division/Route services on a re-parent.
        services.AddScoped<IGeoCascadeService, GeoCascadeService>();

        // Reconciliation — detect drift + idempotent top-down repair (backfill).
        services.AddScoped<IGeoConsistencyRepository, GeoConsistencyRepository>();
        services.AddScoped<IGeoConsistencyService, GeoConsistencyService>();
        return services;
    }
}
