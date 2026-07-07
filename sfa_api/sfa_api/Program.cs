using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using sfa_api.Common.Audit;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Common.Middleware;
using sfa_api.Features.Auth;
using sfa_api.Features.Areas;
using sfa_api.Features.Distributors;
using sfa_api.Features.Divisions;
using sfa_api.Features.Outlets;
using sfa_api.Features.ProductCategories;
using sfa_api.Features.ProductCategoryPricings;
using sfa_api.Features.Products;
using sfa_api.Features.PurchaseOrders;
using sfa_api.Features.Billings;
using sfa_api.Features.NotBillings;
using sfa_api.Features.MobileSync;
using sfa_api.Features.GRNs;
using sfa_api.Features.Supervisor;
using sfa_api.Features.SalesInvoices;
using sfa_api.Features.SalesTargets;
using sfa_api.Features.LocationPings;
using sfa_api.Features.Notifications;
using sfa_api.Features.Stock;
using sfa_api.Features.StockTaking;
using sfa_api.Features.Fleets;
using sfa_api.Features.Regions;
using sfa_api.Features.Routes;
using sfa_api.Features.Territories;
using sfa_api.Features.DailyRouteAssignments;
using sfa_api.Features.UserGeoAssignments;
using sfa_api.Features.UserReportingLines;
using sfa_api.Features.Users;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using sfa_api.Infrastructure.Audit;
using sfa_api.Infrastructure.Caching;
using sfa_api.Infrastructure.Locking;
using sfa_api.Infrastructure.Logging;
using sfa_api.Infrastructure.Notifications;
using sfa_api.Infrastructure.Persistence;

// Bootstrap logger
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Fail-fast secret validation ───────────────────────────────────────
    // Secrets are injected at runtime — user-secrets/env vars locally, a secrets
    // manager in prod — and are NEVER committed to source control. Refuse to boot
    // with a missing or weak signing key rather than running with broken or
    // forgeable auth. (Tests inject a dedicated test-only key via the factory.)
    var jwtSecret = builder.Configuration["Jwt:SecretKey"];
    if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
        throw new InvalidOperationException(
            "Jwt:SecretKey is missing or shorter than 32 bytes. Provide it via the environment " +
            "variable 'Jwt__SecretKey' or user-secrets — it must never be committed to source control.");

    if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is missing. Provide it via the environment variable " +
            "'ConnectionStrings__DefaultConnection' or user-secrets — connection strings must never be committed.");

    // ── Request body size limit ───────────────────────────────────────────
    builder.WebHost.ConfigureKestrel(options =>
        options.Limits.MaxRequestBodySize = 10 * 1024 * 1024); // 10 MB

    // ── Logging ──────────────────────────────────────────────────────────
    SerilogConfig.Apply(builder);

    // ── Database ─────────────────────────────────────────────────────────
    builder.Services.AddSingleton<AuditInterceptor>();
    builder.Services.AddSingleton<SlowQueryInterceptor>();
    var baseConnStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    var pooledConnStr = baseConnStr.TrimEnd(';')
        + ";Maximum Pool Size=100;Minimum Pool Size=10;Connection Idle Lifetime=300";

    builder.Services.AddDbContextPool<AppDbContext>((sp, opt) =>
        opt.UseNpgsql(
               pooledConnStr,
               npgsql => npgsql
                   .CommandTimeout(30)
                   .EnableRetryOnFailure(3))
           .AddInterceptors(
               sp.GetRequiredService<AuditInterceptor>(),
               sp.GetRequiredService<SlowQueryInterceptor>()));

    // ── Caching ──────────────────────────────────────────────────────────
    var redisConnection = builder.Configuration["REDIS_CONNECTION"];
    if (!string.IsNullOrWhiteSpace(redisConnection))
    {
        builder.Services.AddStackExchangeRedisCache(o => o.Configuration = redisConnection);
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
    }
    builder.Services.AddScoped<ICacheService, DistributedCacheService>();

    // ── Idempotency ───────────────────────────────────────────────────────
    builder.Services.AddScoped<IIdempotencyService, PostgresIdempotencyService>();
    builder.Services.AddHostedService<IdempotencyCleanupService>();

    // ── Audit Log Cleanup ─────────────────────────────────────────────────
    builder.Services.AddHostedService<AuditLogCleanupService>();

    // ── Refresh Token Cleanup ─────────────────────────────────────────────
    builder.Services.AddHostedService<sfa_api.Features.Auth.Services.RefreshTokenCleanupService>();

    // ── Nightly Stock Reconciliation ──────────────────────────────────────
    builder.Services.AddHostedService<sfa_api.Features.Stock.Services.StockReconciliationBackgroundService>();

    // ── JWT Revocation ────────────────────────────────────────────────────
    builder.Services.AddScoped<ITokenRevocationService, PostgresTokenRevocationService>();

    // ── Distributed Locking (Redis / Upstash Redlock, fallback to Postgres advisory locks) ──
    var upstashUrl = builder.Configuration["UPSTASH_REDIS_REST_URL"];
    if (!string.IsNullOrWhiteSpace(upstashUrl))
    {
        builder.Services.AddSingleton<RedLockNet.IDistributedLockFactory>(
            _ => RedisDistributedLockService.CreateFactory(builder.Configuration));
        builder.Services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();
    }
    else
    {
        builder.Services.AddSingleton<IDistributedLockService, PostgresAdvisoryLockService>();
    }

    // ── OpenTelemetry ─────────────────────────────────────────────────────
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("sfa-api"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter())
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter());

    // ── Auth ──────────────────────────────────────────────────────────────
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddAuthorization(options =>
    {
        // Defense-in-depth (#24): any endpoint without an explicit [Authorize]/[AllowAnonymous]
        // requires an authenticated user by default, so a future controller can't accidentally
        // ship public. Public endpoints (Auth, health checks) opt out explicitly.
        options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });

    // ── Response Compression ─────────────────────────────────────────────
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    });

    // ── HTTP & API ────────────────────────────────────────────────────────
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = ctx =>
            {
                var fields = ctx.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        k => k.Key,
                        v => v.Value!.Errors
                            .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage)
                            .ToArray());

                var correlationId = ctx.HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
                var error = new ApiError(
                    "VALIDATION_FAILED",
                    "One or more validation errors occurred.",
                    null, fields, null, correlationId, DateTime.UtcNow);

                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new ApiErrorResponse(false, error));
            };
        });
    builder.Services.AddSFACors(builder.Configuration);
    builder.Services.AddSFARateLimiting(builder.Configuration);
    builder.Services.AddSFASwagger();
    builder.Services.AddSFAHealthChecks(builder.Configuration);
    builder.Services.AddRequestTimeouts(o =>
        o.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(30)
        });

    // ── Firebase Push Notifications ───────────────────────────────────────
    var firebaseJson = builder.Configuration["FIREBASE_SERVICE_ACCOUNT_JSON"];
    if (!string.IsNullOrWhiteSpace(firebaseJson))
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromJson(firebaseJson)
        });
    }
    builder.Services.AddScoped<INotificationService, FirebaseNotificationService>();

    // ── Features ──────────────────────────────────────────────────────────
    builder.Services.AddAuthFeature();
    builder.Services.AddUsersFeature();
    builder.Services.AddDistributorsFeature();
    builder.Services.AddFleetsFeature();
    builder.Services.AddRegionsFeature();
    builder.Services.AddAreasFeature();
    builder.Services.AddTerritoriesFeature();
    builder.Services.AddDivisionsFeature();
    builder.Services.AddUserReportingLinesFeature();
    builder.Services.AddUserGeoAssignmentsFeature();
    builder.Services.AddDailyRouteAssignmentsFeature();
    builder.Services.AddRoutesFeature();
    builder.Services.AddOutletsFeature();
    builder.Services.AddProductsFeature();
    builder.Services.AddProductCategoriesFeature();
    builder.Services.AddProductCategoryPricingsFeature();
    builder.Services.AddPurchaseOrdersFeature();
    builder.Services.AddSalesInvoicesFeature();
    builder.Services.AddSalesTargetsFeature();
    builder.Services.AddGrnsFeature();
    builder.Services.AddBillingsFeature(builder.Configuration);
    builder.Services.AddNotBillingsFeature();
    builder.Services.AddStockFeature();
    builder.Services.AddStockTakingFeature();
    builder.Services.AddMobileSyncFeature();
    builder.Services.AddSupervisorFeature();
    builder.Services.AddNotificationsFeature();
    builder.Services.AddLocationPingsFeature();

    var app = builder.Build();

    // ── Schema guard (finding #3) ─────────────────────────────────────────
    // Refuse to start if the database is missing migrations this build expects. Without this the
    // app boots green and then throws "column/relation does not exist" 500s the moment a query hits
    // an un-applied schema change. Migrations must be applied as an explicit deploy step
    // (dotnet ef database update / a migration bundle) BEFORE this build is rolled out. Skipped for
    // local Development and for the SQLite test provider (which uses EnsureCreated, not migrations).
    if (!app.Environment.IsDevelopment())
    {
        await using var schemaScope = app.Services.CreateAsyncScope();
        var schemaDb = schemaScope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (schemaDb.Database.IsNpgsql())
        {
            var pending = (await schemaDb.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count > 0)
            {
                Log.Fatal("Startup aborted: {Count} un-applied database migration(s): {Migrations}. " +
                          "Apply migrations (dotnet ef database update / migration bundle) before deploying this build.",
                          pending.Count, string.Join(", ", pending));
                throw new InvalidOperationException(
                    $"Database has {pending.Count} un-applied migration(s): {string.Join(", ", pending)}. " +
                    "Run migrations before starting the API.");
            }
        }
    }

    // ── Seed ──────────────────────────────────────────────────────────────
    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        await DataSeeder.SeedAsync(app.Services, app.Logger);
    }

    // ── Middleware Pipeline (ORDER MATTERS) ───────────────────────────────
    app.UseResponseCompression();                    // 0. Compress responses (before logging)
    app.UseMiddleware<GlobalExceptionMiddleware>();  // 1. Catch all exceptions (must be first to wrap all errors)
    app.UseMiddleware<CorrelationIdMiddleware>();    // 2. Correlation ID
    app.UseSerilogRequestLogging();                 // 3. Log every request (sees final status)
    app.UseHttpsRedirection();                      // 4. HTTPS only
    app.UseCors("SFAPolicy");                       // 5. CORS
    // 6. Trust X-Forwarded-For ONLY from explicitly configured proxies/load balancers.
    //    Without this, an attacker could spoof the header to defeat the per-IP rate limiter
    //    (finding #8). Configure the deployment's ingress IPs/CIDRs via
    //    ForwardedHeaders:KnownProxies / ForwardedHeaders:KnownNetworks; if none are set, the
    //    framework's secure loopback-only default stands (header ignored from public clients).
    var forwardedOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        ForwardLimit = 1
    };
    var knownProxies = app.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
    var knownNetworks = app.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
    if (knownProxies.Length > 0 || knownNetworks.Length > 0)
    {
        forwardedOptions.KnownProxies.Clear();
        forwardedOptions.KnownNetworks.Clear();
        foreach (var proxy in knownProxies)
            if (System.Net.IPAddress.TryParse(proxy, out var ip))
                forwardedOptions.KnownProxies.Add(ip);
        foreach (var network in knownNetworks)
        {
            var parts = network.Split('/');
            if (parts.Length == 2
                && System.Net.IPAddress.TryParse(parts[0], out var prefix)
                && int.TryParse(parts[1], out var prefixLength))
                forwardedOptions.KnownNetworks.Add(
                    new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLength));
        }
    }
    app.UseForwardedHeaders(forwardedOptions);
    app.UseRequestTimeouts();                        // 7. Request timeouts (before rate limiter)
    app.UseRateLimiter();                           // 8. Rate limiting
    app.UseAuthentication();                        // 9. Validate JWT
    app.UseAuthorization();                         // 10. Permissions
    app.UseMiddleware<IdempotencyMiddleware>();      // 11. Idempotency (after auth — needs User claims)

    // ── Endpoints ─────────────────────────────────────────────────────────
    app.MapControllers();
    app.MapSFAHealthChecks();

    // ── Swagger (non-production only — don't expose the full API surface in prod) (#24) ──
    if (!app.Environment.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SFA API v1"));
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program accessible to WebApplicationFactory in integration tests
public partial class Program { }
