using sfa_api.Features.Dashboard.Repositories;
using sfa_api.Features.Dashboard.Services;

namespace sfa_api.Features.Dashboard;

public static class DashboardServiceExtensions
{
    public static IServiceCollection AddDashboardFeature(this IServiceCollection services)
    {
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
