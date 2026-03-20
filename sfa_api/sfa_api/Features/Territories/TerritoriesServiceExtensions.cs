using FluentValidation;
using sfa_api.Features.Territories.Repositories;
using sfa_api.Features.Territories.Requests;
using sfa_api.Features.Territories.Services;
using sfa_api.Features.Territories.Validators;

namespace sfa_api.Features.Territories;

public static class TerritoriesServiceExtensions
{
    public static IServiceCollection AddTerritoriesFeature(this IServiceCollection services)
    {
        services.AddScoped<ITerritoryRepository, TerritoryRepository>();
        services.AddScoped<ITerritoryService, TerritoryService>();
        services.AddScoped<IValidator<CreateTerritoryRequest>, CreateTerritoryValidator>();
        services.AddScoped<IValidator<UpdateTerritoryRequest>, UpdateTerritoryValidator>();
        return services;
    }
}
