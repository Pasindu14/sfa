using sfa_api.Features.RepTimelines.Repositories;
using sfa_api.Features.RepTimelines.Services;

namespace sfa_api.Features.RepTimelines;

public static class RepTimelinesServiceExtensions
{
    public static IServiceCollection AddRepTimelinesFeature(this IServiceCollection services)
    {
        services.AddScoped<IRepTimelineRepository, RepTimelineRepository>();
        services.AddScoped<IRepTimelineService, RepTimelineService>();
        return services;
    }
}
