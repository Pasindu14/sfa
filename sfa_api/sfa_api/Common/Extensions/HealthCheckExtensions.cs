using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace sfa_api.Common.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddSFAHealthChecks(
        this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection");
        var healthChecks = services.AddHealthChecks();
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            healthChecks.AddNpgSql(connectionString, name: "postgresql", tags: ["ready"]);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapSFAHealthChecks(this IEndpointRouteBuilder app)
    {
        // AllowAnonymous so the global authorization FallbackPolicy (#24) doesn't make liveness/
        // readiness probes require a JWT — probes must reach these without authentication.
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        }).AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = hc => hc.Tags.Contains("ready")
        }).AllowAnonymous();

        return app;
    }
}
