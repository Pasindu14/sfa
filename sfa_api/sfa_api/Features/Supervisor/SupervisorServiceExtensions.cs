using sfa_api.Features.Supervisor.Repositories;
using sfa_api.Features.Supervisor.Services;

namespace sfa_api.Features.Supervisor;

public static class SupervisorServiceExtensions
{
    public static IServiceCollection AddSupervisorFeature(this IServiceCollection services)
    {
        services.AddScoped<ISupervisorRepository, SupervisorRepository>();
        services.AddScoped<ISupervisorService, SupervisorService>();
        return services;
    }
}
