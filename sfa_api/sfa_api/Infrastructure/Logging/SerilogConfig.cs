using Serilog;
using Serilog.Events;

namespace sfa_api.Infrastructure.Logging;

public static class SerilogConfig
{
    public static void Apply(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, lc) => lc
            .MinimumLevel.Is(ctx.HostingEnvironment.IsDevelopment()
                ? LogEventLevel.Debug
                : LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
            .WriteTo.Seq(ctx.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));
    }
}
