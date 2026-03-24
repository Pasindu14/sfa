using sfa_api.Features.Stock.Repositories;

namespace sfa_api.Features.Stock;

public static class StockServiceExtensions
{
    public static IServiceCollection AddStockFeature(this IServiceCollection services)
    {
        services.AddScoped<IStockRepository, StockRepository>();
        return services;
    }
}
