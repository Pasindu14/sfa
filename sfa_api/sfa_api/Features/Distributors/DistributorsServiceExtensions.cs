using FluentValidation;
using sfa_api.Features.Distributors.Repositories;
using sfa_api.Features.Distributors.Requests;
using sfa_api.Features.Distributors.Services;
using sfa_api.Features.Distributors.Validators;

namespace sfa_api.Features.Distributors;

public static class DistributorsServiceExtensions
{
    public static IServiceCollection AddDistributorsFeature(this IServiceCollection services)
    {
        services.AddScoped<IDistributorRepository, DistributorRepository>();
        services.AddScoped<IDistributorService, DistributorService>();
        services.AddScoped<IValidator<CreateDistributorRequest>, CreateDistributorValidator>();
        services.AddScoped<IValidator<UpdateDistributorRequest>, UpdateDistributorValidator>();
        return services;
    }
}
