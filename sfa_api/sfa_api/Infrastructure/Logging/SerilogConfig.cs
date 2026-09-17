using Serilog;
using Serilog.Events;

namespace sfa_api.Infrastructure.Logging;

public static class SerilogConfig
{
    /// <summary>Configuration section holding Serilog minimum levels.</summary>
    public const string MinimumLevelSection = "Serilog:MinimumLevel";

    public static void Apply(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, lc) =>
        {
            ApplyMinimumLevels(lc, ctx.Configuration, ctx.HostingEnvironment.IsDevelopment());

            lc.Enrich.FromLogContext()
              .Enrich.WithMachineName()
              .Enrich.WithEnvironmentName()
              .Enrich.WithThreadId()
              .WriteTo.Console(outputTemplate:
                  "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}");

            // Absent key keeps the historical localhost default; an explicitly empty value
            // (e.g. appsettings.Production.json) means "no Seq" — don't register a dead sink.
            var seqUrl = ctx.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341";
            if (!string.IsNullOrWhiteSpace(seqUrl))
                lc.WriteTo.Seq(seqUrl);
        });
    }

    /// <summary>
    /// Applies minimum levels. Built-in defaults (Debug in Development, Information elsewhere;
    /// Microsoft and System overridden to Warning) are applied first, then any values under
    /// <c>Serilog:MinimumLevel</c> (<c>Default</c> and <c>Override:&lt;SourceContext&gt;</c>)
    /// replace them. With no section present the result equals the historical hardcoded config.
    /// Invalid level strings are ignored (the default stands) rather than failing startup.
    /// </summary>
    public static LoggerConfiguration ApplyMinimumLevels(
        LoggerConfiguration lc, IConfiguration configuration, bool isDevelopment)
    {
        var defaultLevel = isDevelopment ? LogEventLevel.Debug : LogEventLevel.Information;
        var overrides = new Dictionary<string, LogEventLevel>(StringComparer.Ordinal)
        {
            ["Microsoft"] = LogEventLevel.Warning,
            ["System"] = LogEventLevel.Warning
        };

        var section = configuration.GetSection(MinimumLevelSection);
        if (TryParseLevel(section["Default"], out var configuredDefault))
            defaultLevel = configuredDefault;

        foreach (var child in section.GetSection("Override").GetChildren())
        {
            if (TryParseLevel(child.Value, out var level))
                overrides[child.Key] = level;
        }

        lc.MinimumLevel.Is(defaultLevel);
        foreach (var (source, level) in overrides)
            lc.MinimumLevel.Override(source, level);

        return lc;
    }

    /// <summary>
    /// Request-completion log level. Identical to Serilog.AspNetCore's default (Error for an
    /// exception or 5xx, Information otherwise) except that liveness/readiness probes under
    /// <c>/health</c> are logged at Verbose, which is below every configured minimum and is
    /// therefore never emitted — probes otherwise flood the logs every few seconds.
    /// </summary>
    public static LogEventLevel GetRequestLogLevel(HttpContext ctx, double elapsedMs, Exception? ex)
    {
        if (ex is not null || ctx.Response.StatusCode > 499)
            return LogEventLevel.Error;

        if (ctx.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
            return LogEventLevel.Verbose;

        return LogEventLevel.Information;
    }

    private static bool TryParseLevel(string? value, out LogEventLevel level)
    {
        level = default;
        return !string.IsNullOrWhiteSpace(value)
               && Enum.TryParse(value.Trim(), ignoreCase: true, out level)
               && Enum.IsDefined(level);
    }
}
