using FluentValidation;
using sfa_api.Features.Routes.Repositories;
using sfa_api.Features.Routes.Requests;
using sfa_api.Features.Routes.Services;
using sfa_api.Features.Routes.Validators;

namespace sfa_api.Features.Routes;

public static class RoutesServiceExtensions
{
    public static IServiceCollection AddRoutesFeature(this IServiceCollection services)
    {
        services.AddScoped<IRouteRepository, RouteRepository>();
        services.AddScoped<IRouteService, RouteService>();
        services.AddScoped<IValidator<CreateRouteRequest>, CreateRouteValidator>();
        services.AddScoped<IValidator<UpdateRouteRequest>, UpdateRouteValidator>();
        return services;
    }
}
