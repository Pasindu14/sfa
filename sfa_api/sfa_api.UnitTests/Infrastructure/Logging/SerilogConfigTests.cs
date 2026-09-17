using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using sfa_api.Common.Extensions;
using sfa_api.Infrastructure.Logging;

namespace sfa_api.UnitTests.Infrastructure.Logging;

public class SerilogConfigTests
{
    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    private static (Logger Logger, CollectingSink Sink) Build(
        Dictionary<string, string?> settings, bool isDevelopment)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var sink = new CollectingSink();
        var lc = new LoggerConfiguration().WriteTo.Sink(sink);
        SerilogConfig.ApplyMinimumLevels(lc, config, isDevelopment);
        return (lc.CreateLogger(), sink);
    }

    private static bool Emits(Logger logger, CollectingSink sink, string source, LogEventLevel level)
    {
        sink.Events.Clear();
        logger.ForContext(Constants.SourceContextPropertyName, source).Write(level, "probe");
        return sink.Events.Count == 1;
    }

    [Fact]
    public void NoConfig_NonDevelopment_MatchesHistoricalDefaults()
    {
        var (logger, sink) = Build([], isDevelopment: false);

        Emits(logger, sink, "sfa_api.Features.X", LogEventLevel.Information).Should().BeTrue();
        Emits(logger, sink, "sfa_api.Features.X", LogEventLevel.Debug).Should().BeFalse();
        Emits(logger, sink, "Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information).Should().BeFalse();
        Emits(logger, sink, "Microsoft.AspNetCore.Hosting", LogEventLevel.Warning).Should().BeTrue();
        Emits(logger, sink, "System.Net.Http.HttpClient", LogEventLevel.Information).Should().BeFalse();
    }

    [Fact]
    public void NoConfig_Development_MatchesHistoricalDefaults()
    {
        var (logger, sink) = Build([], isDevelopment: true);

        Emits(logger, sink, "sfa_api.Features.X", LogEventLevel.Debug).Should().BeTrue();
        Emits(logger, sink, "sfa_api.Features.X", LogEventLevel.Verbose).Should().BeFalse();
        Emits(logger, sink, "Microsoft.AspNetCore.Routing", LogEventLevel.Information).Should().BeFalse();
    }

    [Fact]
    public void ProductionStyleConfig_WarningDefault_KeepsRequestLogsAndWarnings()
    {
        var (logger, sink) = Build(new()
        {
            ["Serilog:MinimumLevel:Default"] = "Warning",
            ["Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore"] = "Warning",
            ["Serilog:MinimumLevel:Override:Serilog.AspNetCore.RequestLoggingMiddleware"] = "Information",
        }, isDevelopment: false);

        Emits(logger, sink, "sfa_api.Features.X", LogEventLevel.Information).Should().BeFalse();
        Emits(logger, sink, "sfa_api.Infrastructure.Logging.SlowQueryInterceptor", LogEventLevel.Warning).Should().BeTrue();
        Emits(logger, sink, "Serilog.AspNetCore.RequestLoggingMiddleware", LogEventLevel.Information).Should().BeTrue();
        Emits(logger, sink, "Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information).Should().BeFalse();
    }

    [Fact]
    public void InvalidLevelValues_AreIgnored()
    {
        var (logger, sink) = Build(new()
        {
            ["Serilog:MinimumLevel:Default"] = "Loud",
            ["Serilog:MinimumLevel:Override:Microsoft"] = "",
        }, isDevelopment: false);

        Emits(logger, sink, "sfa_api.Features.X", LogEventLevel.Information).Should().BeTrue();
        Emits(logger, sink, "Microsoft.AspNetCore.Hosting", LogEventLevel.Information).Should().BeFalse();
    }

    [Theory]
    [InlineData("/health/live", 200, LogEventLevel.Verbose)]
    [InlineData("/health/ready", 200, LogEventLevel.Verbose)]
    [InlineData("/health/ready", 503, LogEventLevel.Error)]
    [InlineData("/api/v1/regions", 200, LogEventLevel.Information)]
    [InlineData("/api/v1/regions", 404, LogEventLevel.Information)]
    [InlineData("/api/v1/regions", 500, LogEventLevel.Error)]
    [InlineData("/healthcheck-lookalike", 200, LogEventLevel.Information)]
    public void GetRequestLogLevel_DropsHealthProbes_OtherwiseMatchesSerilogDefault(
        string path, int status, LogEventLevel expected)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Response.StatusCode = status;

        SerilogConfig.GetRequestLogLevel(ctx, 1, null).Should().Be(expected);
    }

    [Fact]
    public void GetRequestLogLevel_Exception_IsError()
        => SerilogConfig.GetRequestLogLevel(new DefaultHttpContext(), 1, new InvalidOperationException())
            .Should().Be(LogEventLevel.Error);

    [Fact]
    public void UserOrIpKey_AuthenticatedUsesUserId_AnonymousUsesIp()
    {
        var anon = new DefaultHttpContext();
        anon.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.9");
        RateLimitExtensions.UserOrIpKey(anon).Should().Be("ip:10.0.0.9");

        var authed = new DefaultHttpContext();
        authed.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.9");
        authed.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "42")], authenticationType: "Bearer"));
        RateLimitExtensions.UserOrIpKey(authed).Should().Be("user:42");

        // Claims on an unauthenticated identity must not be trusted.
        var unauthenticated = new DefaultHttpContext();
        unauthenticated.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "42")]));
        RateLimitExtensions.UserOrIpKey(unauthenticated).Should().Be("ip:unknown");
    }
}
