using FluentValidation;
using sfa_api.Common.Extensions;
using sfa_api.Features.Auth.Repositories;
using sfa_api.Features.Auth.Requests;
using sfa_api.Features.Auth.Services;
using sfa_api.Features.Auth.Validators;

namespace sfa_api.Features.Auth;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddAuthFeature(this IServiceCollection services)
    {
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtTokenHelper, JwtTokenHelper>();
        services.AddScoped<IValidator<LoginRequest>, LoginValidator>();
        services.AddScoped<IValidator<RefreshRequest>, RefreshValidator>();
        return services;
    }
}
