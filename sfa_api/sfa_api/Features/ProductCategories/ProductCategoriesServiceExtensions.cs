using FluentValidation;
using sfa_api.Features.ProductCategories.Repositories;
using sfa_api.Features.ProductCategories.Requests;
using sfa_api.Features.ProductCategories.Services;
using sfa_api.Features.ProductCategories.Validators;

namespace sfa_api.Features.ProductCategories;

public static class ProductCategoriesServiceExtensions
{
    public static IServiceCollection AddProductCategoriesFeature(this IServiceCollection services)
    {
        services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
        services.AddScoped<IProductCategoryService, ProductCategoryService>();
        services.AddScoped<IValidator<CreateProductCategoryRequest>, CreateProductCategoryValidator>();
        services.AddScoped<IValidator<UpdateProductCategoryRequest>, UpdateProductCategoryValidator>();
        return services;
    }
}
