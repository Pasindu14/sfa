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

    // Partition key for general (non-auth) limits: the authenticated user id when the JWT was
    // validated, else the client IP. Many reps sit behind one carrier-grade NAT / office egress
    // IP, so a pure per-IP bucket made unrelated users throttle each other. Requires
    // UseRateLimiter to run AFTER UseAuthentication (see Program.cs) so HttpContext.User is
    // populated; an invalid/expired/revoked token leaves the principal unauthenticated, so
    // forged tokens fall back to the IP bucket and cannot mint fresh partitions. The "user:"/"ip:"
    // prefixes keep a numeric user id from ever colliding with an IP string.
    public static string UserOrIpKey(HttpContext ctx)
    {
        if (ctx.User?.Identity?.IsAuthenticated == true)
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? ctx.User.FindFirstValue("sub");
            if (!string.IsNullOrEmpty(userId))
                return "user:" + userId;
        }

        return "ip:" + ClientIpKey(ctx);
    }

    /// <summary>Global per-user (or per-IP for anonymous) sliding-window limiter.</summary>
    public static PartitionedRateLimiter<HttpContext> CreateGlobalLimiter(
        int permitLimit, int windowSeconds)
        => PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            RateLimitPartition.GetSlidingWindowLimiter(UserOrIpKey(ctx),
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

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

            options.GlobalLimiter = CreateGlobalLimiter(globalPermitLimit, globalWindowSeconds);

            var authPermitLimit = config.GetValue<int>("RateLimit:AuthPermitLimit");
            var authWindowSeconds = config.GetValue<int>("RateLimit:AuthWindowSeconds");

            // "auth" — per-IP sliding window (brute-force protection on login/refresh).
            // Deliberately stays per-IP even when a (valid) bearer token is attached: credential
            // stuffing must not be able to spread attempts across partitions.
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
                return RateLimitPartition.GetFixedWindowLimiter(UserOrIpKey(ctx),
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
