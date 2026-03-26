using FluentValidation;
using sfa_api.Features.UserGeoAssignments.Repositories;
using sfa_api.Features.UserGeoAssignments.Requests;
using sfa_api.Features.UserGeoAssignments.Services;
using sfa_api.Features.UserGeoAssignments.Validators;

namespace sfa_api.Features.UserGeoAssignments;

public static class UserGeoAssignmentsServiceExtensions
{
    public static IServiceCollection AddUserGeoAssignmentsFeature(this IServiceCollection services)
    {
        services.AddScoped<IUserGeoAssignmentRepository, UserGeoAssignmentRepository>();
        services.AddScoped<IUserGeoAssignmentService, UserGeoAssignmentService>();
        services.AddScoped<IValidator<CreateUserAssignmentRequest>, CreateUserAssignmentValidator>();
        services.AddScoped<IValidator<UpdateUserAssignmentRequest>, UpdateUserAssignmentValidator>();
        return services;
    }
}
