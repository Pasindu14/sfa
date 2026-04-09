using FluentValidation;
using sfa_api.Features.Billings.Repositories;
using sfa_api.Features.Billings.Services;
using sfa_api.Features.Billings.Validators;

namespace sfa_api.Features.Billings;

public static class BillingsServiceExtensions
{
    public static IServiceCollection AddBillingsFeature(this IServiceCollection services)
    {
        services.AddScoped<IBillingRepository, BillingRepository>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddValidatorsFromAssemblyContaining<CreateBillingValidator>(includeInternalTypes: false);
        return services;
    }
}
