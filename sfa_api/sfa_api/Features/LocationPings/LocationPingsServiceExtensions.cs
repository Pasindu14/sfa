using FluentValidation;
using sfa_api.Features.LocationPings.Repositories;
using sfa_api.Features.LocationPings.Requests;
using sfa_api.Features.LocationPings.Services;
using sfa_api.Features.LocationPings.Validators;

namespace sfa_api.Features.LocationPings;

public static class LocationPingsServiceExtensions
{
    public static IServiceCollection AddLocationPingsFeature(this IServiceCollection services)
    {
        services.AddScoped<ILocationPingRepository, LocationPingRepository>();
        services.AddScoped<ILocationPingService, LocationPingService>();
        services.AddScoped<IValidator<CreateLocationPingsRequest>, CreateLocationPingsValidator>();
        return services;
    }
}
