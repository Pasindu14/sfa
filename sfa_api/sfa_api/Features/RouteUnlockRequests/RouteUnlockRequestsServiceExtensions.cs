using FluentValidation;
using sfa_api.Features.RouteUnlockRequests.Options;
using sfa_api.Features.RouteUnlockRequests.Repositories;
using sfa_api.Features.RouteUnlockRequests.Requests;
using sfa_api.Features.RouteUnlockRequests.Services;
using sfa_api.Features.RouteUnlockRequests.Validators;

namespace sfa_api.Features.RouteUnlockRequests;

public static class RouteUnlockRequestsServiceExtensions
{
    public static IServiceCollection AddRouteUnlockRequestsFeature(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RouteUnlockOptions>(configuration.GetSection(RouteUnlockOptions.SectionName));
        // Also consumed by ProximityPolicyResolver (enforcement read side).
        services.AddScoped<IRouteUnlockRequestRepository, RouteUnlockRequestRepository>();
        services.AddScoped<IRouteUnlockRequestService, RouteUnlockRequestService>();
        services.AddScoped<IValidator<CreateRouteUnlockRequest>, CreateRouteUnlockRequestValidator>();
        services.AddScoped<IValidator<ApproveRouteUnlockRequest>, ApproveRouteUnlockRequestValidator>();
        services.AddScoped<IValidator<ReasonedRouteUnlockRequest>, ReasonedRouteUnlockRequestValidator>();
        services.AddScoped<IValidator<CancelRouteUnlockRequest>, CancelRouteUnlockRequestValidator>();
        return services;
    }
}
