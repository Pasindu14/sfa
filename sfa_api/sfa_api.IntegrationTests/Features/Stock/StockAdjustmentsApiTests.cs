using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Features.Stock.Requests;
using sfa_api.Features.Stock.Services;
using sfa_api.Infrastructure.Persistence;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Stock;

/// <summary>
/// End-to-end tests for /api/v1/stock-adjustments — admin correction of a distributor's balances.
/// Exercises the real service, StockRepository Credit/Deduct and ledger writes against SQLite
/// (row locking is swapped for TestStockLocking, the advisory lock for a no-op).
/// </summary>
[Collection(SfaApiCollection.Name)]
public class StockAdjustmentsApiTests
{
    private const string BaseUrl = "/api/v1/stock-adjustments";

    private readonly SfaWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public StockAdjustmentsApiTests(SfaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        // User 1 is the seeded admin — AdjustedBy / TransactedBy are FKs to Users.
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AuthHelper.GenerateToken(1, "Admin", "admin@sfa.com", "System Admin"));
    }

    private record Envelope<T>(bool Success, T Data, PageMeta? Pagination);
    private record PageMeta(int Page, int PageSize, int Total, int TotalPages);
    private record ErrorEnvelope(bool Success, ErrorBody Error);
    private record ErrorBody(string Code, string Message, Dictionary<string, string[]>? Fields);

    private record AdjustmentResult(
        int Id, string AdjustmentNumber, int DistributorId, string DistributorName,
        string Reason, string? Notes, string AdjustedByName, DateTime AdjustedAt,
        int LineCount, decimal TotalIncrease, decimal TotalDecrease, List<AdjustmentLineResult>? Lines);
    private record AdjustmentLineResult(
        int Id, int ProductId, string ProductCode, string ProductDescription, string StockType,
        decimal QuantityBefore, decimal NewQuantity, decimal Difference, int PiecesPerPack);

    private sealed record Seed(int DistributorId, int ProductA, int ProductB, int ProductC);

    /// <summary>A/Normal = 100, B/FreeIssue = 20; C has no stock row.</summary>
    private async Task<Seed> SeedAsync(bool distributorActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var s = Guid.NewGuid().ToString("N")[..8];

        var distributor = new Distributor
        {
            Name = $"SA-{s}", Email = $"sa-{s}@test.com", Phone = $"+94sa{s}", IsActive = distributorActive,
        };
        Product P(string tag, int ppp) => new()
        {
            Code = $"SA{tag}-{s}", ItemDescription = $"Adjust {tag}", PiecesPerPack = ppp, IsActive = true,
        };
        var a = P("A", 24);
        var b = P("B", 12);
        var c = P("C", 6);
        db.AddRange(distributor, a, b, c);
        await db.SaveChangesAsync();

        db.DistributorStocks.AddRange(
            new DistributorStock { DistributorId = distributor.Id, ProductId = a.Id, StockType = StockType.Normal, QuantityOnHand = 100m, LastUpdatedAt = DateTime.UtcNow },
            new DistributorStock { DistributorId = distributor.Id, ProductId = b.Id, StockType = StockType.FreeIssue, QuantityOnHand = 20m, LastUpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        return new Seed(distributor.Id, a.Id, b.Id, c.Id);
    }

    private async Task<Dictionary<(int, StockType), decimal>> BalancesAsync(int distributorId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rows = await db.DistributorStocks.AsNoTracking().Where(x => x.DistributorId == distributorId).ToListAsync();
        return rows.ToDictionary(r => (r.ProductId, r.StockType), r => r.QuantityOnHand);
    }

    // Raw JSON so the enum strings are exactly what the web client sends.
    private static StringContent Body(int distributorId, string reason, string? notes,
        params (int ProductId, string StockType, decimal Expected, decimal New)[] lines)
    {
        var json = JsonSerializer.Serialize(new
        {
            distributorId,
            reason,
            notes,
            lines = lines.Select(l => new
            {
                productId = l.ProductId, stockType = l.StockType, expectedQuantity = l.Expected, newQuantity = l.New,
            }),
        });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    // ── Happy path ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_UpdatesBalances_WritesCorrectionLedger_AndIsReadable()
    {
        var seed = await SeedAsync(distributorActive: false);   // closed distributors may be adjusted too

        var response = await _client.PostAsync(BaseUrl, Body(seed.DistributorId, "CountCorrection", "Monthly count",
            (seed.ProductA, "Normal", 100m, 130m),      // +30
            (seed.ProductB, "FreeIssue", 20m, 5m),      // −15
            (seed.ProductC, "Normal", 0m, 12m),         // +12, new row
            (seed.ProductA, "FreeIssue", 0m, 0m)));     // unchanged — dropped

        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, raw);
        var created = JsonSerializer.Deserialize<Envelope<AdjustmentResult>>(raw, _jsonOpts)!.Data;
        created.AdjustmentNumber.Should().Be($"SA-{created.Id:D6}");
        created.Reason.Should().Be("CountCorrection");
        created.AdjustedByName.Should().Be("System Admin");
        created.LineCount.Should().Be(3);
        created.TotalIncrease.Should().Be(42m);
        created.TotalDecrease.Should().Be(15m);
        created.Lines!.Should().ContainSingle(l => l.ProductId == seed.ProductB && l.StockType == "FreeIssue"
            && l.QuantityBefore == 20m && l.NewQuantity == 5m && l.Difference == -15m && l.PiecesPerPack == 12);

        var balances = await BalancesAsync(seed.DistributorId);
        balances[(seed.ProductA, StockType.Normal)].Should().Be(130m);
        balances[(seed.ProductB, StockType.FreeIssue)].Should().Be(5m);
        balances[(seed.ProductC, StockType.Normal)].Should().Be(12m);
        balances.Should().NotContainKey((seed.ProductA, StockType.FreeIssue));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ledger = await db.StockTransactions.AsNoTracking()
                .Where(t => t.ReferenceType == "StockAdjustment" && t.ReferenceId == created.Id)
                .ToListAsync();

            ledger.Should().HaveCount(3);
            ledger.Should().OnlyContain(t => t.TransactionType == StockTransactionType.Correction
                && t.DistributorId == seed.DistributorId && t.TransactedBy == 1
                && t.Notes!.Contains(created.AdjustmentNumber));
            ledger.Should().ContainSingle(t => t.ProductId == seed.ProductA && t.Direction == StockTransactionDirection.In
                && t.Quantity == 30m && t.QuantityBefore == 100m && t.QuantityAfter == 130m);
            ledger.Should().ContainSingle(t => t.ProductId == seed.ProductB && t.Direction == StockTransactionDirection.Out
                && t.Quantity == 15m && t.QuantityAfter == 5m);
            ledger.Should().ContainSingle(t => t.ProductId == seed.ProductC && t.Direction == StockTransactionDirection.In
                && t.Quantity == 12m && t.QuantityBefore == 0m);
        }

        var byId = await _client.GetAsync($"{BaseUrl}/{created.Id}");
        byId.StatusCode.Should().Be(HttpStatusCode.OK);
        (await byId.Content.ReadFromJsonAsync<Envelope<AdjustmentResult>>(_jsonOpts))!.Data.Lines.Should().HaveCount(3);

        var list = await _client.GetAsync($"{BaseUrl}?page=1&pageSize=20&distributorId={seed.DistributorId}");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = (await list.Content.ReadFromJsonAsync<Envelope<List<AdjustmentResult>>>(_jsonOpts))!;
        page.Pagination!.Total.Should().Be(1);
        var summary = page.Data.Single();
        summary.Id.Should().Be(created.Id);
        summary.LineCount.Should().Be(3);
        summary.TotalIncrease.Should().Be(42m);
        summary.TotalDecrease.Should().Be(15m);
    }

    /// <summary>
    /// The bin card's own repository aggregates SUM(decimal) in SQL, which SQLite cannot translate
    /// (see BinCardApiTests). So the ledger rows written by the real endpoint are aggregated in memory
    /// by <see cref="LedgerBinCardRepository"/> and fed through the real <see cref="BinCardService"/>,
    /// proving Correction rows land in the Stock Adjustment column and End Stock matches the balance.
    /// </summary>
    [Fact]
    public async Task Create_ShowsInBinCardStockAdjustmentColumn()
    {
        var seed = await SeedAsync();
        var slToday = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5.5));   // Sri Lanka business date

        // SeedAsync writes balances directly; give A the ledger history a real balance always has,
        // dated before the bin-card window, so it becomes Open Stock = 100.
        using (var seedScope = _factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            seedDb.StockTransactions.Add(new StockTransaction
            {
                DistributorId = seed.DistributorId, ProductId = seed.ProductA, StockType = StockType.Normal,
                TransactionType = StockTransactionType.GRNReceipt, Direction = StockTransactionDirection.In,
                Quantity = 100m, QuantityBefore = 0m, QuantityAfter = 100m,
                ReferenceType = "GRN", ReferenceId = 0, TransactedAt = DateTime.UtcNow.AddDays(-5), TransactedBy = 1,
            });
            await seedDb.SaveChangesAsync();
        }

        var response = await _client.PostAsync(BaseUrl, Body(seed.DistributorId, "Damage", null,
            (seed.ProductA, "Normal", 100m, 70m),
            (seed.ProductC, "Normal", 0m, 12m)));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var binCard = await new BinCardService(new LedgerBinCardRepository(db))
            .GetBinCardAsync(new BinCardQuery(seed.DistributorId, slToday.AddDays(-1), slToday.AddDays(1)));

        var balances = await BalancesAsync(seed.DistributorId);
        var rowA = binCard.Rows.Single(r => r.ItemCode.StartsWith("SAA-"));
        rowA.OpenStock.Should().Be(100m);
        rowA.StockAdjustment.Should().Be(-30m);
        rowA.EndStock.Should().Be(balances[(seed.ProductA, StockType.Normal)]);   // 70
        var rowC = binCard.Rows.Single(r => r.ItemCode.StartsWith("SAC-"));
        rowC.StockAdjustment.Should().Be(12m);
        rowC.EndStock.Should().Be(12m);
    }

    /// <summary>In-memory stand-in for BinCardRepository's SQL aggregates, computed from the real ledger rows.</summary>
    private sealed class LedgerBinCardRepository(AppDbContext db) : IBinCardRepository
    {
        private Task<List<StockTransaction>> LedgerAsync(int distributorId, CancellationToken ct)
            => db.StockTransactions.AsNoTracking().Where(t => t.DistributorId == distributorId).ToListAsync(ct);

        public async Task<List<BinCardMovementAgg>> GetBinCardMovementsAsync(
            int distributorId, DateTime fromUtc, DateTime toExclusiveUtc, CancellationToken ct = default)
            => (await LedgerAsync(distributorId, ct))
                .Where(t => t.TransactedAt >= fromUtc && t.TransactedAt < toExclusiveUtc)
                .GroupBy(t => new { t.ProductId, t.TransactionType, t.StockType, t.Direction })
                .Select(g => new BinCardMovementAgg(g.Key.ProductId, g.Key.TransactionType, g.Key.StockType,
                    g.Key.Direction, g.Sum(t => t.Quantity)))
                .ToList();

        public async Task<List<BinCardOpeningAgg>> GetBinCardOpeningAsync(
            int distributorId, DateTime fromUtc, CancellationToken ct = default)
            => (await LedgerAsync(distributorId, ct))
                .Where(t => t.TransactedAt < fromUtc)
                .GroupBy(t => new { t.ProductId, t.StockType })
                .Select(g => (g.Key.ProductId, g.MaxBy(t => t.Id)!.QuantityAfter))
                .GroupBy(x => x.ProductId)
                .Select(g => new BinCardOpeningAgg(g.Key, g.Sum(x => x.QuantityAfter)))
                .ToList();

        public Task<List<BinCardRepReturnAgg>> GetBinCardRepReturnsAsync(
            int distributorId, DateOnly from, DateOnly to, CancellationToken ct = default)
            => Task.FromResult(new List<BinCardRepReturnAgg>());

        public Task<List<BinCardCountAgg>> GetBinCardLatestCountsAsync(
            int distributorId, DateTime asOfExclusiveUtc, CancellationToken ct = default)
            => Task.FromResult(new List<BinCardCountAgg>());

        public async Task<List<BinCardProductInfo>> GetBinCardProductsAsync(
            IReadOnlyCollection<int> productIds, CancellationToken ct = default)
            => (await db.Products.AsNoTracking().Where(p => productIds.Contains(p.Id)).ToListAsync(ct))
                .Select(p => new BinCardProductInfo(p.Id, p.Code, p.ItemDescription, 0m))
                .ToList();

        public Task<string?> GetDistributorNameAsync(int distributorId, CancellationToken ct = default)
            => db.Distributors.AsNoTracking().Where(d => d.Id == distributorId).Select(d => (string?)d.Name).FirstOrDefaultAsync(ct);
    }

    // ── Rejections ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_StaleExpectedQuantity_Returns409_StockChanged_WithFields()
    {
        var seed = await SeedAsync();

        var response = await _client.PostAsync(BaseUrl, Body(seed.DistributorId, "Damage", null,
            (seed.ProductA, "Normal", 90m, 80m)));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = (await response.Content.ReadFromJsonAsync<ErrorEnvelope>(_jsonOpts))!.Error;
        error.Code.Should().Be("STOCK_CHANGED");
        error.Fields.Should().ContainKey($"product:{seed.ProductA}");
        (await BalancesAsync(seed.DistributorId))[(seed.ProductA, StockType.Normal)].Should().Be(100m);
    }

    [Fact]
    public async Task Create_NoDifferences_Returns422_NoChanges()
    {
        var seed = await SeedAsync();

        var response = await _client.PostAsync(BaseUrl, Body(seed.DistributorId, "Expiry", null,
            (seed.ProductA, "Normal", 100m, 100m)));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadFromJsonAsync<ErrorEnvelope>(_jsonOpts))!.Error.Code.Should().Be("NO_CHANGES");
    }

    [Fact]
    public async Task Create_ReasonOtherWithoutNotes_Returns400()
    {
        var seed = await SeedAsync();

        var response = await _client.PostAsync(BaseUrl, Body(seed.DistributorId, "Other", null,
            (seed.ProductA, "Normal", 100m, 90m)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ErrorEnvelope>(_jsonOpts))!.Error.Code.Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task Create_UnknownDistributor_Returns404()
    {
        var seed = await SeedAsync();

        var response = await _client.PostAsync(BaseUrl, Body(999_999, "Damage", null, (seed.ProductA, "Normal", 0m, 1m)));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<ErrorEnvelope>(_jsonOpts))!.Error.Code.Should().Be("DISTRIBUTOR_NOT_FOUND");
    }

    [Fact]
    public async Task Create_UnknownProduct_Returns404()
    {
        var seed = await SeedAsync();

        var response = await _client.PostAsync(BaseUrl, Body(seed.DistributorId, "Damage", null, (999_999, "Normal", 0m, 1m)));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<ErrorEnvelope>(_jsonOpts))!.Error.Code.Should().Be("PRODUCT_NOT_FOUND");
    }

    [Fact]
    public async Task GetById_Unknown_Returns404()
    {
        var response = await _client.GetAsync($"{BaseUrl}/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Auth ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_NonAdmin_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthHelper.SalesRepToken);

        var response = await client.PostAsync(BaseUrl, Body(1, "Damage", null, (1, "Normal", 0m, 1m)));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetList_NonAdmin_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthHelper.SalesRepToken);

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_NoToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync(BaseUrl, Body(1, "Damage", null, (1, "Normal", 0m, 1m)));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
