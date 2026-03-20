using FluentValidation;
using sfa_api.Features.PurchaseOrders.Repositories;
using sfa_api.Features.PurchaseOrders.Requests;
using sfa_api.Features.PurchaseOrders.Services;
using sfa_api.Features.PurchaseOrders.Validators;

namespace sfa_api.Features.PurchaseOrders;

public static class PurchaseOrdersServiceExtensions
{
    public static IServiceCollection AddPurchaseOrdersFeature(this IServiceCollection services)
    {
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IValidator<CreatePurchaseOrderRequest>, CreatePurchaseOrderValidator>();
        services.AddScoped<IValidator<UpdatePurchaseOrderRequest>, UpdatePurchaseOrderValidator>();
        return services;
    }
}
