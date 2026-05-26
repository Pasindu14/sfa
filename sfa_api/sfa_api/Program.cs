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
using sfa_api.Features.PricingStructures;
using sfa_api.Features.Products;
using sfa_api.Features.PurchaseOrders;
using sfa_api.Features.Billings;
using sfa_api.Features.NotBillings;
using sfa_api.Features.MobileSync;
using sfa_api.Features.GRNs;
using sfa_api.Features.Supervisor;
using sfa_api.Features.SalesInvoices;
using sfa_api.Features.SalesTargets;
using sfa_api.Features.Notifications;
using sfa_api.Features.Stock;
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

    // ── JWT Revocation ────────────────────────────────────────────────────
    builder.Services.AddScoped<ITokenRevocationService, PostgresTokenRevocationService>();

    // ── Distributed Locking (Redis / Upstash Redlock) ─────────────────────
    builder.Services.AddSingleton<RedLockNet.IDistributedLockFactory>(
        _ => RedisDistributedLockService.CreateFactory(builder.Configuration));
    builder.Services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();

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
    builder.Services.AddAuthorization();

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
    builder.Services.AddPricingStructuresFeature();
    builder.Services.AddPurchaseOrdersFeature();
    builder.Services.AddSalesInvoicesFeature();
    builder.Services.AddSalesTargetsFeature();
    builder.Services.AddGrnsFeature();
    builder.Services.AddBillingsFeature();
    builder.Services.AddNotBillingsFeature();
    builder.Services.AddStockFeature();
    builder.Services.AddMobileSyncFeature();
    builder.Services.AddSupervisorFeature();
    builder.Services.AddNotificationsFeature();

    var app = builder.Build();

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
    app.UseForwardedHeaders(new ForwardedHeadersOptions // 6. Trust X-Forwarded-For from proxy
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
    app.UseRequestTimeouts();                        // 7. Request timeouts (before rate limiter)
    app.UseRateLimiter();                           // 8. Rate limiting
    app.UseAuthentication();                        // 9. Validate JWT
    app.UseAuthorization();                         // 10. Permissions
    app.UseMiddleware<IdempotencyMiddleware>();      // 11. Idempotency (after auth — needs User claims)

    // ── Endpoints ─────────────────────────────────────────────────────────
    app.MapControllers();
    app.MapSFAHealthChecks();

    // ── Swagger ───────────────────────────────────────────────────────────
    if (app.Environment.IsDevelopment())
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
