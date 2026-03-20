using FluentValidation;
using sfa_api.Features.Users.Repositories;
using sfa_api.Features.Users.Requests;
using sfa_api.Features.Users.Services;
using sfa_api.Features.Users.Validators;

namespace sfa_api.Features.Users;

public static class UsersServiceExtensions
{
    public static IServiceCollection AddUsersFeature(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IValidator<CreateUserRequest>, CreateUserValidator>();
        services.AddScoped<IValidator<UpdateUserRequest>, UpdateUserValidator>();
        services.AddScoped<IValidator<ChangePasswordRequest>, ChangePasswordValidator>();
        services.AddScoped<IValidator<ResetPasswordRequest>, ResetPasswordValidator>();
        return services;
    }
}
