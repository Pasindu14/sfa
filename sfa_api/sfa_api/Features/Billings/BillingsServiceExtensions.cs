using FluentValidation;
using Microsoft.Extensions.Options;
using sfa_api.Features.Billings.Options;
using sfa_api.Features.Billings.Repositories;
using sfa_api.Features.Billings.Services;
using sfa_api.Features.Billings.Validators;

namespace sfa_api.Features.Billings;

public static class BillingsServiceExtensions
{
    public static IServiceCollection AddBillingsFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BillingGeoOptions>(configuration.GetSection("BillingGeo"));
        services.AddScoped<IBillingRepository, BillingRepository>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IDistributorBillingDashboardRepository, DistributorBillingDashboardRepository>();
        services.AddScoped<IDistributorBillingDashboardService, DistributorBillingDashboardService>();
        services.AddValidatorsFromAssemblyContaining<CreateBillingValidator>(includeInternalTypes: false);
        return services;
    }
}
