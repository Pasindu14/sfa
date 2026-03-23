using FluentValidation;
using sfa_api.Features.ProductCategoryPricings.Repositories;
using sfa_api.Features.ProductCategoryPricings.Requests;
using sfa_api.Features.ProductCategoryPricings.Services;
using sfa_api.Features.ProductCategoryPricings.Validators;

namespace sfa_api.Features.ProductCategoryPricings;

public static class ProductCategoryPricingsServiceExtensions
{
    public static IServiceCollection AddProductCategoryPricingsFeature(this IServiceCollection services)
    {
        services.AddScoped<IProductCategoryPricingRepository, ProductCategoryPricingRepository>();
        services.AddScoped<IProductCategoryPricingService, ProductCategoryPricingService>();
        services.AddScoped<IValidator<BulkUpsertPricingRequest>, BulkUpsertPricingValidator>();
        return services;
    }
}
