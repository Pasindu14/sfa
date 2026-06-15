using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using sfa_api.Common.Errors;
using sfa_api.Infrastructure.Caching;
using System.IdentityModel.Tokens.Jwt;
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
                    // Enforce the access-token denylist: a token whose jti has been
                    // revoked (e.g. on logout) is rejected even though it is still
                    // cryptographically valid and unexpired. jti/expiry are also stashed
                    // into HttpContext.Items so the logout endpoint can revoke precisely
                    // without depending on inbound claim mapping.
                    OnTokenValidated = async ctx =>
                    {
                        var jti = (ctx.SecurityToken as JsonWebToken)?.Id
                                  ?? (ctx.SecurityToken as JwtSecurityToken)?.Id;
                        var expiresAt = (ctx.SecurityToken as JsonWebToken)?.ValidTo
                                        ?? (ctx.SecurityToken as JwtSecurityToken)?.ValidTo
                                        ?? DateTime.MinValue;

                        if (string.IsNullOrEmpty(jti)) return;

                        ctx.HttpContext.Items["AccessTokenJti"] = jti;
                        ctx.HttpContext.Items["AccessTokenExpiresAt"] = expiresAt;

                        var revocationService = ctx.HttpContext.RequestServices
                            .GetRequiredService<ITokenRevocationService>();

                        if (await revocationService.IsRevokedAsync(jti, ctx.HttpContext.RequestAborted))
                            ctx.Fail("Token has been revoked.");
                    },

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
                    },

                    OnForbidden = async ctx =>
                    {
                        ctx.Response.StatusCode = 403;
                        ctx.Response.ContentType = "application/json";

                        var correlationId = ctx.HttpContext.Items["CorrelationId"]?.ToString()
                                            ?? string.Empty;

                        var error = new ApiError(
                            "FORBIDDEN_ACCESS",
                            "You do not have permission to perform this action.",
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
