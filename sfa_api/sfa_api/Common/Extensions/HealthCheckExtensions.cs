using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace sfa_api.Common.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddSFAHealthChecks(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddHealthChecks()
            .AddNpgSql(
                config.GetConnectionString("DefaultConnection")!,
                name: "postgresql",
                tags: ["ready"]);

        return services;
    }

    public static IEndpointRouteBuilder MapSFAHealthChecks(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = hc => hc.Tags.Contains("ready")
        });

        return app;
    }
}
