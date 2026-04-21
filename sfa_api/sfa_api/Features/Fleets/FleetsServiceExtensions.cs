using FluentValidation;
using sfa_api.Features.Fleets.Repositories;
using sfa_api.Features.Fleets.Requests;
using sfa_api.Features.Fleets.Services;
using sfa_api.Features.Fleets.Validators;

namespace sfa_api.Features.Fleets;

public static class FleetsServiceExtensions
{
    public static IServiceCollection AddFleetsFeature(this IServiceCollection services)
    {
        services.AddScoped<IFleetRepository, FleetRepository>();
        services.AddScoped<IFleetService, FleetService>();
        services.AddScoped<IValidator<CreateFleetRequest>, CreateFleetValidator>();
        services.AddScoped<IValidator<UpdateFleetRequest>, UpdateFleetValidator>();
        return services;
    }
}
