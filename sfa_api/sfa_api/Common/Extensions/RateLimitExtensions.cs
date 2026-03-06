using Microsoft.AspNetCore.RateLimiting;
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
                RateLimitPartition.GetSlidingWindowLimiter(
                    ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = globalPermitLimit,
                        Window = TimeSpan.FromSeconds(globalWindowSeconds),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.AddSlidingWindowLimiter("global", opt =>
            {
                opt.PermitLimit = globalPermitLimit;
                opt.Window = TimeSpan.FromSeconds(globalWindowSeconds);
                opt.SegmentsPerWindow = 6;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            options.AddSlidingWindowLimiter("auth", opt =>
            {
                opt.PermitLimit = config.GetValue<int>("RateLimit:AuthPermitLimit");
                opt.Window = TimeSpan.FromSeconds(
                    config.GetValue<int>("RateLimit:AuthWindowSeconds"));
                opt.SegmentsPerWindow = 6;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
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
        });

        return services;
    }
}
