using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Infrastructure.Persistence;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Stock;

/// <summary>
/// Pins the "latest ledger row per (distributor, product, pool)" query after it was rewritten from
/// two round-trips (materialise MAX(Id) list, then IN (@p0..@pN)) into one composed
/// IN (SELECT MAX(Id) ... GROUP BY ...) subquery. GetLatestSnapshotsAsync has no decimal aggregate,
/// so unlike the rest of the reconciliation run it translates and executes on SQLite.
/// Ledger rows are seeded with FK enforcement briefly disabled — the query only reads the ledger.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class StockLatestLedgerRowQueryTests(SfaWebApplicationFactory factory)
{
    [Fact]
    public async Task GetLatestSnapshotsAsync_ReturnsQuantityAfterOfHighestIdPerGroup()
    {
        const int distA = 990_001, distB = 990_002, prod1 = 990_101, prod2 = 990_102;

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        StockTransaction Row(int dist, int prod, StockType pool, decimal after) => new()
        {
            DistributorId = dist, ProductId = prod, StockType = pool,
            TransactionType = StockTransactionType.GRNReceipt, Direction = StockTransactionDirection.In,
            Quantity = 1m, QuantityBefore = after - 1m, QuantityAfter = after,
            ReferenceType = "TEST", ReferenceId = 1, TransactedAt = DateTime.UtcNow, TransactedBy = 1,
        };

        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
        try
        {
            // Inserted one at a time so Id order == posting order.
            foreach (var r in new[]
                     {
                         Row(distA, prod1, StockType.Normal, 10m),
                         Row(distA, prod1, StockType.Normal, 7m),     // latest A/1/Normal
                         Row(distA, prod1, StockType.FreeIssue, 3m),  // latest A/1/FreeIssue
                         Row(distA, prod2, StockType.Normal, 50m),
                         Row(distA, prod2, StockType.Normal, 45m),
                         Row(distA, prod2, StockType.Normal, 44m),    // latest A/2/Normal
                         Row(distB, prod1, StockType.Normal, 99m),    // other distributor
                     })
            {
                db.StockTransactions.Add(r);
                await db.SaveChangesAsync();
            }
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
        }

        var repo = new StockReconciliationRepository(db);

        var forA = await repo.GetLatestSnapshotsAsync(distA, productId: null);
        forA.Select(s => (s.Key.ProductId, s.Key.StockType, s.LatestQuantityAfter))
            .Should().BeEquivalentTo(new[]
            {
                (prod1, StockType.Normal, 7m),
                (prod1, StockType.FreeIssue, 3m),
                (prod2, StockType.Normal, 44m),
            });

        var forProd1 = await repo.GetLatestSnapshotsAsync(distributorId: null, productId: prod1);
        forProd1.Where(s => s.Key.DistributorId is distA or distB)
            .Select(s => (s.Key.DistributorId, s.Key.StockType, s.LatestQuantityAfter))
            .Should().BeEquivalentTo(new[]
            {
                (distA, StockType.Normal, 7m),
                (distA, StockType.FreeIssue, 3m),
                (distB, StockType.Normal, 99m),
            });

        (await repo.GetLatestSnapshotsAsync(990_999, null)).Should().BeEmpty();

        // Test-fixture cleanup: these orphan rows (no real distributor/product) must not leak into other tests.
        await db.StockTransactions.Where(t => t.DistributorId == distA || t.DistributorId == distB).ExecuteDeleteAsync();
    }
}
