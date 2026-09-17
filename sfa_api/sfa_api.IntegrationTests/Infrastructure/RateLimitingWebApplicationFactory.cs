using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Common.Extensions;

namespace sfa_api.IntegrationTests.Infrastructure;

/// <summary>
/// Variant of <see cref="SfaWebApplicationFactory"/> with REAL, deliberately tiny rate limits
/// (the shared factory disables the global limiter). Also lets a test choose the client IP via
/// the <see cref="ClientIpHeader"/> header, because TestServer leaves RemoteIpAddress null.
/// </summary>
public class RateLimitingWebApplicationFactory : SfaWebApplicationFactory
{
    public const string ClientIpHeader = "X-Test-Client-IP";
    public const int GlobalPermitLimit = 3;
    public const int AuthPermitLimit = 2;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // Added after the base source, so these win. The "auth" policy reads its limit lazily
        // when RateLimiterOptions is materialised, so it sees this value.
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimit:AuthPermitLimit"] = AuthPermitLimit.ToString(),
                ["RateLimit:AuthWindowSeconds"] = "3600"
            }));

        builder.ConfigureTestServices(services =>
        {
            // Runs after the base factory's no-op override: restore the production partitioning
            // logic with a small limit.
            services.Configure<RateLimiterOptions>(options =>
                options.GlobalLimiter = RateLimitExtensions.CreateGlobalLimiter(GlobalPermitLimit, 3600));

            services.AddTransient<IStartupFilter, TestClientIpStartupFilter>();
        });
    }

    private sealed class TestClientIpStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (ctx, nextMiddleware) =>
            {
                if (ctx.Request.Headers.TryGetValue(ClientIpHeader, out var raw)
                    && IPAddress.TryParse(raw.ToString(), out var ip))
                    ctx.Connection.RemoteIpAddress = ip;
                await nextMiddleware();
            });
            next(app);
        };
    }
}
