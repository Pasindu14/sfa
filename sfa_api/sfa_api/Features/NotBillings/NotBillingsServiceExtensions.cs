using FluentValidation;
using sfa_api.Features.NotBillings.Repositories;
using sfa_api.Features.NotBillings.Services;
using sfa_api.Features.NotBillings.Validators;

namespace sfa_api.Features.NotBillings;

public static class NotBillingsServiceExtensions
{
    public static IServiceCollection AddNotBillingsFeature(this IServiceCollection services)
    {
        services.AddScoped<INotBillingRepository, NotBillingRepository>();
        services.AddScoped<INotBillingService, NotBillingService>();
        services.AddValidatorsFromAssemblyContaining<CreateNotBillingValidator>(includeInternalTypes: false);
        return services;
    }
}
