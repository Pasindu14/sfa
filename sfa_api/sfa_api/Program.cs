using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;
using sfa_api.Common.Audit;
using sfa_api.Common.Extensions;
using sfa_api.Common.Middleware;
using sfa_api.Features.Auth;
using sfa_api.Features.Areas;
using sfa_api.Features.Distributors;
using sfa_api.Features.Divisions;
using sfa_api.Features.Outlets;
using sfa_api.Features.ProductCategoryPricings;
using sfa_api.Features.PricingStructures;
using sfa_api.Features.Products;
using sfa_api.Features.PurchaseOrders;
using sfa_api.Features.GRNs;
using sfa_api.Features.SalesInvoices;
using sfa_api.Features.Stock;
using sfa_api.Features.Regions;
using sfa_api.Features.Routes;
using sfa_api.Features.Territories;
using sfa_api.Features.UserGeoAssignments;
using sfa_api.Features.UserReportingLines;
using sfa_api.Features.Users;
using sfa_api.Infrastructure.Audit;
using sfa_api.Infrastructure.Caching;
using sfa_api.Infrastructure.Locking;
using sfa_api.Infrastructure.Logging;
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
    builder.Services.AddScoped<AuditInterceptor>();
    builder.Services.AddDbContext<AppDbContext>((sp, opt) =>
        opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));

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

    // ── Auth ──────────────────────────────────────────────────────────────
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddAuthorization();

    // ── HTTP & API ────────────────────────────────────────────────────────
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddControllers();
    builder.Services.AddSFACors(builder.Configuration);
    builder.Services.AddSFARateLimiting(builder.Configuration);
    builder.Services.AddSFASwagger();
    builder.Services.AddSFAHealthChecks(builder.Configuration);

    // ── Features ──────────────────────────────────────────────────────────
    builder.Services.AddAuthFeature();
    builder.Services.AddUsersFeature();
    builder.Services.AddDistributorsFeature();
    builder.Services.AddRegionsFeature();
    builder.Services.AddAreasFeature();
    builder.Services.AddTerritoriesFeature();
    builder.Services.AddDivisionsFeature();
    builder.Services.AddUserReportingLinesFeature();
    builder.Services.AddUserGeoAssignmentsFeature();
    builder.Services.AddRoutesFeature();
    builder.Services.AddOutletsFeature();
    builder.Services.AddProductsFeature();
    builder.Services.AddProductCategoryPricingsFeature();
    builder.Services.AddPricingStructuresFeature();
    builder.Services.AddPurchaseOrdersFeature();
    builder.Services.AddSalesInvoicesFeature();
    builder.Services.AddGrnsFeature();
    builder.Services.AddStockFeature();

    var app = builder.Build();

    // ── Seed ──────────────────────────────────────────────────────────────
    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        await DataSeeder.SeedAsync(app.Services, app.Logger);
    }

    // ── Middleware Pipeline (ORDER MATTERS) ───────────────────────────────
    app.UseMiddleware<CorrelationIdMiddleware>();    // 1. Correlation ID first
    app.UseSerilogRequestLogging();                 // 2. Log every request (sees final status)
    app.UseMiddleware<GlobalExceptionMiddleware>();  // 3. Catch all exceptions
    app.UseHttpsRedirection();                      // 4. HTTPS only
    app.UseCors("SFAPolicy");                       // 5. CORS
    app.UseForwardedHeaders(new ForwardedHeadersOptions // 6. Trust X-Forwarded-For from proxy
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
    app.UseRateLimiter();                           // 7. Rate limiting
    app.UseAuthentication();                        // 8. Validate JWT
    app.UseAuthorization();                         // 9. Permissions
    app.UseMiddleware<IdempotencyMiddleware>();      // 9. Idempotency (after auth — needs User claims)

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
