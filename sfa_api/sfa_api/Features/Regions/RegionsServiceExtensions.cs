using FluentValidation;
using sfa_api.Features.Regions.Repositories;
using sfa_api.Features.Regions.Requests;
using sfa_api.Features.Regions.Services;
using sfa_api.Features.Regions.Validators;

namespace sfa_api.Features.Regions;

public static class RegionsServiceExtensions
{
    public static IServiceCollection AddRegionsFeature(this IServiceCollection services)
    {
        services.AddScoped<IRegionRepository, RegionRepository>();
        services.AddScoped<IRegionService, RegionService>();
        services.AddScoped<IValidator<CreateRegionRequest>, CreateRegionValidator>();
        services.AddScoped<IValidator<UpdateRegionRequest>, UpdateRegionValidator>();
        return services;
    }
}
