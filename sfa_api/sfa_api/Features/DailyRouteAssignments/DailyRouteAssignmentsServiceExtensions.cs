using FluentValidation;
using sfa_api.Features.DailyRouteAssignments.Repositories;
using sfa_api.Features.DailyRouteAssignments.Requests;
using sfa_api.Features.DailyRouteAssignments.Services;
using sfa_api.Features.DailyRouteAssignments.Validators;

namespace sfa_api.Features.DailyRouteAssignments;

public static class DailyRouteAssignmentsServiceExtensions
{
    public static IServiceCollection AddDailyRouteAssignmentsFeature(this IServiceCollection services)
    {
        services.AddScoped<IDailyRouteAssignmentRepository, DailyRouteAssignmentRepository>();
        services.AddScoped<IDailyRouteAssignmentService, DailyRouteAssignmentService>();
        services.AddScoped<IValidator<CreateDailyRouteAssignmentRequest>, CreateDailyRouteAssignmentValidator>();
        return services;
    }
}
