using FluentValidation;
using sfa_api.Features.Outlets.Repositories;
using sfa_api.Features.Outlets.Requests;
using sfa_api.Features.Outlets.Services;
using sfa_api.Features.Outlets.Validators;

namespace sfa_api.Features.Outlets;

public static class OutletsServiceExtensions
{
    public static IServiceCollection AddOutletsFeature(this IServiceCollection services)
    {
        services.AddScoped<IOutletRepository, OutletRepository>();
        services.AddScoped<IOutletService, OutletService>();
        services.AddScoped<IValidator<CreateOutletRequest>, CreateOutletValidator>();
        services.AddScoped<IValidator<UpdateOutletRequest>, UpdateOutletValidator>();
        return services;
    }
}
