using FluentValidation;
using sfa_api.Features.Products.Repositories;
using sfa_api.Features.Products.Requests;
using sfa_api.Features.Products.Services;
using sfa_api.Features.Products.Validators;

namespace sfa_api.Features.Products;

public static class ProductsServiceExtensions
{
    public static IServiceCollection AddProductsFeature(this IServiceCollection services)
    {
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IValidator<CreateProductRequest>, CreateProductValidator>();
        services.AddScoped<IValidator<UpdateProductRequest>, UpdateProductValidator>();
        return services;
    }
}
