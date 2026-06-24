using FluentValidation;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Features.Stock.Services;
using sfa_api.Features.Stock.Validators;

namespace sfa_api.Features.Stock;

public static class StockServiceExtensions
{
    public static IServiceCollection AddStockFeature(this IServiceCollection services)
    {
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IBinCardRepository, BinCardRepository>();
        services.AddScoped<IBinCardService, BinCardService>();
        services.AddScoped<IStockReconciliationRepository, StockReconciliationRepository>();
        services.AddScoped<IStockReconciliationService, StockReconciliationService>();
        services.AddValidatorsFromAssemblyContaining<BinCardQueryValidator>(includeInternalTypes: false);
        return services;
    }
}
