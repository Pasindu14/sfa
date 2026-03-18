using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;
using sfa_api.Features.PurchaseOrders.Repositories;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory that uses SQLite in-memory for testing.
/// No Docker required — the DB lives entirely in process memory.
/// Shared across all tests via IClassFixture for performance.
/// </summary>
public class SfaWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public SfaWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Create the schema BEFORE the app starts, so DataSeeder doesn't crash.
        // Use TestAppDbContext (not AppDbContext) to suppress the PostgreSQL sequence
        // definition that SQLite cannot generate.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var db = new TestAppDbContext(options);
        db.Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                ["RateLimit:GlobalPermitLimit"]         = "100000",
                ["RateLimit:GlobalWindowSeconds"]       = "3600",
                ["RateLimit:AuthPermitLimit"]           = "100000",
                ["RateLimit:AuthWindowSeconds"]         = "3600",
                ["RateLimit:TestPermitLimit"]           = "100000",
                ["RateLimit:TestWindowSeconds"]         = "3600"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove all DbContext-related registrations
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(AppDbContext))
                .ToList();

            foreach (var d in descriptorsToRemove)
                services.Remove(d);

            // Add DbContext using the shared SQLite in-memory connection
            services.AddDbContext<AppDbContext>(opt =>
                opt.UseSqlite(_connection));

            // Remove background services that use PostgreSQL-specific features
            services.RemoveAll<IHostedService>();

            // Replace IPurchaseOrderRepository to stub GetNextOrderNumberAsync, which calls
            // PostgreSQL's nextval() sequence — not supported by SQLite in-memory.
            services.RemoveAll<IPurchaseOrderRepository>();
            services.AddScoped<IPurchaseOrderRepository>(sp =>
            {
                var db = sp.GetRequiredService<AppDbContext>();
                return new TestPurchaseOrderRepository(new PurchaseOrderRepository(db));
            });

            // Disable rate limiting in tests — replace global limiter with no-op
            services.Configure<RateLimiterOptions>(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    _ => RateLimitPartition.GetNoLimiter("testing"));
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Close();
            _connection.Dispose();
        }
        base.Dispose(disposing);
    }
}
