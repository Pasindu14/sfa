using FluentValidation;
using sfa_api.Features.Divisions.Repositories;
using sfa_api.Features.Divisions.Requests;
using sfa_api.Features.Divisions.Services;
using sfa_api.Features.Divisions.Validators;

namespace sfa_api.Features.Divisions;

public static class DivisionsServiceExtensions
{
    public static IServiceCollection AddDivisionsFeature(this IServiceCollection services)
    {
        services.AddScoped<IDivisionRepository, DivisionRepository>();
        services.AddScoped<IDivisionService, DivisionService>();
        services.AddScoped<IValidator<CreateDivisionRequest>, CreateDivisionValidator>();
        services.AddScoped<IValidator<UpdateDivisionRequest>, UpdateDivisionValidator>();
        return services;
    }
}
