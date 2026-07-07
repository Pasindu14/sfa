using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using sfa_api.Common.Errors;

namespace sfa_api.Common.Extensions;

public static class RateLimitExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Partition key for per-client rate limiting. We read Connection.RemoteIpAddress, which
    // UseForwardedHeaders has already rewritten from X-Forwarded-For *only* when the request
    // arrived through a trusted proxy (configured KnownProxies/KnownNetworks). We must NOT parse
    // the raw X-Forwarded-For header ourselves: an attacker could then set an arbitrary value per
    // request, land in a fresh partition every time, and bypass the limit entirely (credential
    // stuffing on /auth/login). See finding #8.
    private static string ClientIpKey(HttpContext ctx)
        => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    public static IServiceCollection AddSFARateLimiting(
        this IServiceCollection services, IConfiguration config)
    {
        var globalPermitLimit = config.GetValue<int>("RateLimit:GlobalPermitLimit");
        var globalWindowSeconds = config.GetValue<int>("RateLimit:GlobalWindowSeconds");

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;

            options.OnRejected = async (ctx, token) =>
            {
                ctx.HttpContext.Response.StatusCode = 429;
                ctx.HttpContext.Response.ContentType = "application/json";

                var correlationId = ctx.HttpContext.Items["CorrelationId"]?.ToString()
                                    ?? string.Empty;

                var error = new ApiError(
                    "RATE_LIMITED",
                    "Too many requests.",
                    "Retry after the indicated time.",
                    null, null, correlationId, DateTime.UtcNow);

                await ctx.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(new ApiErrorResponse(false, error), _jsonOptions),
                    token);
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            {
                var ip = ClientIpKey(ctx);
                return RateLimitPartition.GetSlidingWindowLimiter(ip,
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = globalPermitLimit,
                        Window = TimeSpan.FromSeconds(globalWindowSeconds),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            var authPermitLimit = config.GetValue<int>("RateLimit:AuthPermitLimit");
            var authWindowSeconds = config.GetValue<int>("RateLimit:AuthWindowSeconds");

            // "auth" — per-IP sliding window (brute-force protection on login/refresh)
            options.AddPolicy("auth", ctx =>
            {
                var ip = ClientIpKey(ctx);
                return RateLimitPartition.GetSlidingWindowLimiter(ip,
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = authPermitLimit,
                        Window = TimeSpan.FromSeconds(authWindowSeconds),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddSlidingWindowLimiter("test", opt =>
            {
                opt.PermitLimit = config.GetValue<int>("RateLimit:TestPermitLimit");
                opt.Window = TimeSpan.FromSeconds(
                    config.GetValue<int>("RateLimit:TestWindowSeconds"));
                opt.SegmentsPerWindow = 6;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            var userPermitLimit = config.GetValue<int>("RateLimit:UserPermitLimit", 30);
            var userWindowSeconds = config.GetValue<int>("RateLimit:UserWindowSeconds", 60);

            options.AddPolicy("user", ctx =>
            {
                var userId = ctx.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? ctx.Connection.RemoteIpAddress?.ToString()
                             ?? "anon";
                return RateLimitPartition.GetFixedWindowLimiter(userId,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = userPermitLimit,
                        Window = TimeSpan.FromSeconds(userWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}
