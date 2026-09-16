using FluentValidation;
using sfa_api.Features.UserProximityExemptions.Repositories;
using sfa_api.Features.UserProximityExemptions.Requests;
using sfa_api.Features.UserProximityExemptions.Services;
using sfa_api.Features.UserProximityExemptions.Validators;

namespace sfa_api.Features.UserProximityExemptions;

public static class UserProximityExemptionsServiceExtensions
{
    public static IServiceCollection AddUserProximityExemptionsFeature(this IServiceCollection services)
    {
        services.AddScoped<IUserProximityExemptionRepository, UserProximityExemptionRepository>();
        services.AddScoped<IUserProximityExemptionService, UserProximityExemptionService>();
        // Consumed by both OutletService (read side) and BillingService (write side).
        services.AddScoped<IProximityPolicyResolver, ProximityPolicyResolver>();
        services.AddScoped<IValidator<GrantProximityExemptionRequest>, GrantProximityExemptionValidator>();
        services.AddScoped<IValidator<RevokeProximityExemptionRequest>, RevokeProximityExemptionValidator>();
        return services;
    }
}
