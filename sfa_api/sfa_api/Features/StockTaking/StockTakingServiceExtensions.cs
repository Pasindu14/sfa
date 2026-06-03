using FluentValidation;
using sfa_api.Features.StockTaking.Repositories;
using sfa_api.Features.StockTaking.Requests;
using sfa_api.Features.StockTaking.Services;
using sfa_api.Features.StockTaking.Validators;

namespace sfa_api.Features.StockTaking;

public static class StockTakingServiceExtensions
{
    public static IServiceCollection AddStockTakingFeature(this IServiceCollection services)
    {
        services.AddScoped<IStockTakingRepository, StockTakingRepository>();
        services.AddScoped<IStockTakingService, StockTakingService>();
        services.AddScoped<IValidator<CreatePeriodRequest>, CreatePeriodValidator>();
        services.AddScoped<IValidator<UpsertSubmissionRequest>, UpsertSubmissionValidator>();
        services.AddScoped<IValidator<AdjustLineRequest>, AdjustLineValidator>();
        return services;
    }
}
