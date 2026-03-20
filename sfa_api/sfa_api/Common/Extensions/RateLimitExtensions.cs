using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace sfa_api.Common.Extensions;

public static class RateLimitExtensions
{
    public static IServiceCollection AddSFARateLimiting(
        this IServiceCollection services, IConfiguration config)
    {
        var globalPermitLimit = config.GetValue<int>("RateLimit:GlobalPermitLimit");
        var globalWindowSeconds = config.GetValue<int>("RateLimit:GlobalWindowSeconds");

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            {
                var ip = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                         ?? ctx.Connection.RemoteIpAddress?.ToString()
                         ?? "unknown";
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
                var ip = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                         ?? ctx.Connection.RemoteIpAddress?.ToString()
                         ?? "unknown";
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

            options.AddPolicy("user", ctx =>
            {
                var userId = ctx.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? ctx.Connection.RemoteIpAddress?.ToString()
                             ?? "anon";
                return RateLimitPartition.GetFixedWindowLimiter(userId,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}
