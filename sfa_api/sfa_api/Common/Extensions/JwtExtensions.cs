using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using sfa_api.Common.Errors;
using System.Text;
using System.Text.Json;

namespace sfa_api.Common.Extensions;

public static class JwtExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration config)
    {
        var key = Encoding.UTF8.GetBytes(config["Jwt:SecretKey"]!);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode = 401;
                        ctx.Response.ContentType = "application/json";

                        var correlationId = ctx.HttpContext.Items["CorrelationId"]?.ToString()
                                            ?? string.Empty;

                        var error = new ApiError(
                            "AUTH_INVALID_TOKEN",
                            "Token is invalid or has been revoked.",
                            null, null, null,
                            correlationId,
                            DateTime.UtcNow);

                        await ctx.Response.WriteAsync(
                            JsonSerializer.Serialize(new ApiErrorResponse(false, error), _jsonOptions));
                    }
                };
            });

        return services;
    }
}
