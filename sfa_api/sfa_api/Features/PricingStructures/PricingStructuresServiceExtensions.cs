using FluentValidation;
using sfa_api.Features.PricingStructures.Repositories;
using sfa_api.Features.PricingStructures.Requests;
using sfa_api.Features.PricingStructures.Services;
using sfa_api.Features.PricingStructures.Validators;

namespace sfa_api.Features.PricingStructures;

public static class PricingStructuresServiceExtensions
{
    public static IServiceCollection AddPricingStructuresFeature(this IServiceCollection services)
    {
        services.AddScoped<IPricingStructureRepository, PricingStructureRepository>();
        services.AddScoped<IPricingStructureService, PricingStructureService>();
        services.AddScoped<IValidator<CreatePricingStructureRequest>, CreatePricingStructureValidator>();
        services.AddScoped<IValidator<UpdatePricingStructureRequest>, UpdatePricingStructureValidator>();
        services.AddScoped<IValidator<BulkUpdateItemsRequest>, BulkUpdateItemsValidator>();
        return services;
    }
}
