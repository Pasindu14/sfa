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
using sfa_api.Features.Billings.Repositories;
using sfa_api.Features.GRNs.Repositories;
using sfa_api.Features.PurchaseOrders.Repositories;
using sfa_api.Features.SalesInvoices.Repositories;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Infrastructure.Locking;
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

    // The app validates Jwt:SecretKey and the connection string at startup (fail-fast),
    // reading them from the environment-variable config source — which, unlike the factory's
    // ConfigureAppConfiguration, is visible during Program.cs's pre-Build() reads. Supply a
    // dedicated test-only signing key (matching AuthHelper) and a dummy connection string here.
    // The real DbContext is swapped to SQLite in ConfigureServices, so the value is only there
    // to satisfy the startup guard.
    static SfaWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__SecretKey", AuthHelper.SecretKey);
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "DataSource=:memory:");
    }

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

        // The DataSeeder only runs in Development/Staging (not "Testing").
        // Seed the admin user manually so FK-constrained entities (SalesInvoiceImportBatch.ImportedBy)
        // can reference a real user row. The seeded user gets ID=1 (SQLite auto-increment).
        if (!db.Users.Any())
        {
            db.Users.Add(new sfa_api.Features.Users.Entities.User
            {
                Name         = "System Admin",
                Username     = "admin",
                Email        = "admin@sfa.com",
                Phone        = "+94000000000",
                PasswordHash = "placeholder",
                Role         = sfa_api.Features.Users.Entities.UserRole.Admin,
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow,
                UpdatedAt    = DateTime.UtcNow,
            });
            db.SaveChanges();
        }
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
                ["RateLimit:TestWindowSeconds"]         = "3600",
                ["RateLimit:UserPermitLimit"]           = "100000",
                ["RateLimit:UserWindowSeconds"]         = "3600"
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

            // Set up EF Core internal services for AppDbContext with SQLite (registers DbContextOptions<AppDbContext>).
            services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(_connection));

            // Replace the AppDbContext service binding so the runtime resolves TestAppDbContext.
            // TestAppDbContext.OnModelCreating patches SQLite incompatibilities: removes sequences
            // and changes Area.RowVersion from a store-generated PostgreSQL xmin column to a
            // plain INTEGER column so inserts do not fail with "NOT NULL constraint failed: Areas.xmin".
            // TestAppDbContext accepts DbContextOptions<AppDbContext> (not DbContextOptions<TestAppDbContext>),
            // so AddDbContext<TContext, TImpl> cannot be used — we swap the registration manually.
            services.Remove(services.First(d => d.ServiceType == typeof(AppDbContext)));
            services.AddScoped<AppDbContext>(sp =>
                new TestAppDbContext(sp.GetRequiredService<DbContextOptions<AppDbContext>>()));

            // Remove background services that use PostgreSQL-specific features
            services.RemoveAll<IHostedService>();

            // Replace sequence-based repositories: all three call PostgreSQL nextval(),
            // which SQLite in-memory does not support. Each test wrapper uses an atomic counter.

            services.RemoveAll<IPurchaseOrderRepository>();
            services.AddScoped<IPurchaseOrderRepository>(sp =>
            {
                var db = sp.GetRequiredService<AppDbContext>();
                return new TestPurchaseOrderRepository(new PurchaseOrderRepository(db));
            });

            services.RemoveAll<ISalesInvoiceRepository>();
            services.AddScoped<ISalesInvoiceRepository>(sp =>
            {
                var db = sp.GetRequiredService<AppDbContext>();
                return new TestSalesInvoiceRepository(new SalesInvoiceRepository(db));
            });

            services.RemoveAll<IGrnRepository>();
            services.AddScoped<IGrnRepository>(sp =>
            {
                var db = sp.GetRequiredService<AppDbContext>();
                return new TestGrnRepository(new GrnRepository(db));
            });

            // Billing creation calls nextval('billing_number_seq') (PostgreSQL-only) — wrap
            // with an atomic counter, delegating all other calls to the real repository.
            services.RemoveAll<IBillingRepository>();
            services.AddScoped<IBillingRepository>(sp =>
            {
                var db = sp.GetRequiredService<AppDbContext>();
                return new TestBillingRepository(new BillingRepository(db));
            });

            // StockRepository.GetStockForUpdateAsync uses "SELECT … FOR UPDATE" (PostgreSQL-only).
            // Wrap with a no-op for that method; all balance mutations delegate to the real repo.
            services.RemoveAll<IStockRepository>();
            services.AddScoped<IStockRepository>(sp =>
            {
                var db = sp.GetRequiredService<AppDbContext>();
                return new TestStockRepository(new StockRepository(db));
            });

            // Replace PostgresAdvisoryLockService with a no-op implementation.
            // pg_try_advisory_lock() is PostgreSQL-only and cannot run against SQLite.
            services.RemoveAll<IDistributedLockService>();
            services.AddSingleton<IDistributedLockService, NoOpDistributedLockService>();

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
