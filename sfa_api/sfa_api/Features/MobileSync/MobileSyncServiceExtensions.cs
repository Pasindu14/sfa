using sfa_api.Features.MobileSync.Repositories;
using sfa_api.Features.MobileSync.Services;

namespace sfa_api.Features.MobileSync;

public static class MobileSyncServiceExtensions
{
    public static IServiceCollection AddMobileSyncFeature(this IServiceCollection services)
    {
        services.AddScoped<IMobileSyncRepository, MobileSyncRepository>();
        services.AddScoped<IMobileSyncService, MobileSyncService>();
        return services;
    }
}
