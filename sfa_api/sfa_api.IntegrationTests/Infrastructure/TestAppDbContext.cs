using Microsoft.EntityFrameworkCore;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.IntegrationTests.Infrastructure;

/// <summary>
/// Test-only AppDbContext subclass that suppresses the PostgreSQL sequence definition
/// from the EF Core model, so SQLite's EnsureCreated() can generate the schema
/// without throwing "SQLite does not support sequences".
///
/// Usage: call EnsureCreated() on this context instead of AppDbContext.
/// The application's runtime DI still uses the real AppDbContext — this is only
/// used for test schema setup.
/// </summary>
public class TestAppDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // After the real model is built, remove the sequence that SQLite cannot handle.
        // The TestSalesOrderRepository replaces GetNextOrderNumberAsync with an atomic counter.
        var seq = modelBuilder.Model.FindSequence("sales_order_number_seq");
        if (seq != null)
            modelBuilder.Model.RemoveSequence(seq.Name, seq.Schema);
    }
}
