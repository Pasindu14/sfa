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
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Infrastructure.Persistence;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Stock;

/// <summary>
/// End-to-end tests for /api/v1/stock-transfers — moving a closed distributor's stock into an
/// active one. Exercises the real service, StockRepository Deduct/Credit and ledger writes against
/// SQLite (row locking is swapped for TestStockLocking, the advisory lock for a no-op).
/// </summary>
[Collection(SfaApiCollection.Name)]
public class StockTransfersApiTests
{
    private const string BaseUrl = "/api/v1/stock-transfers";

    private readonly SfaWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public StockTransfersApiTests(SfaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        // User 1 is the seeded admin — TransferredBy / TransactedBy are FKs to Users.
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AuthHelper.GenerateToken(1, "Admin", "admin@sfa.com", "System Admin"));
    }

    private record Envelope<T>(bool Success, T Data, PageMeta? Pagination);
    private record PageMeta(int Page, int PageSize, int Total, int TotalPages);
    private record ErrorEnvelope(bool Success, ErrorBody Error);
    private record ErrorBody(string Code, string Message);

    private record TransferResult(
        int Id, string TransferNumber,
        int SourceDistributorId, string SourceDistributorName,
        int TargetDistributorId, string TargetDistributorName,
        string? Notes, string TransferredByName, DateTime TransferredAt,
        int LineCount, decimal TotalQuantity, List<TransferLineResult>? Lines);
    private record TransferLineResult(
        int Id, int ProductId, string ProductCode, string ProductDescription,
        string StockType, decimal Quantity, int PiecesPerPack);
    private record StockRow(int ProductId, string StockType, decimal QuantityOnHand);

    private sealed record Seed(int SourceId, int TargetId, int ProductA, int ProductB);

    /// <summary>
    /// Source (inactive): A/Normal = 100, B/FreeIssue = 20. Target (active): A/Normal = 5, no B row.
    /// </summary>
    private async Task<Seed> SeedAsync(bool sourceActive = false)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var s = Guid.NewGuid().ToString("N")[..8];

        Distributor Dist(string role, bool active) => new()
        {
            Name = $"ST-{role}-{s}", Email = $"st-{role}-{s}@test.com", Phone = $"+94{role}{s}", IsActive = active,
        };
        var source = Dist("src", sourceActive);
        var target = Dist("tgt", true);
        var productA = new Product { Code = $"STA-{s}", ItemDescription = "Transfer Cracker", PiecesPerPack = 24, IsActive = true };
        var productB = new Product { Code = $"STB-{s}", ItemDescription = "Transfer Biscuit", PiecesPerPack = 12, IsActive = true };
        db.AddRange(source, target, productA, productB);
        await db.SaveChangesAsync();

        DistributorStock Stock(int distributorId, int productId, StockType type, decimal qty) => new()
        {
            DistributorId = distributorId, ProductId = productId, StockType = type,
            QuantityOnHand = qty, LastUpdatedAt = DateTime.UtcNow,
        };
        db.DistributorStocks.AddRange(
            Stock(source.Id, productA.Id, StockType.Normal, 100m),
            Stock(source.Id, productB.Id, StockType.FreeIssue, 20m),
            Stock(target.Id, productA.Id, StockType.Normal, 5m));
        await db.SaveChangesAsync();

        return new Seed(source.Id, target.Id, productA.Id, productB.Id);
    }

    private async Task<Dictionary<(int, StockType), decimal>> BalancesAsync(int distributorId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rows = await db.DistributorStocks.AsNoTracking().Where(x => x.DistributorId == distributorId).ToListAsync();
        return rows.ToDictionary(r => (r.ProductId, r.StockType), r => r.QuantityOnHand);
    }

    // Raw JSON so the stockType strings are exactly what the web client sends.
    private static StringContent Body(int source, int target, params (int ProductId, string StockType, decimal Quantity)[] lines)
    {
        var json = JsonSerializer.Serialize(new
        {
            sourceDistributorId = source,
            targetDistributorId = target,
            notes = "Area closed",
            lines = lines.Select(l => new { productId = l.ProductId, stockType = l.StockType, quantity = l.Quantity }),
        });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    // ── Happy path ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_MovesBalances_WritesLedger_AndIsReadable()
    {
        var seed = await SeedAsync();

        var response = await _client.PostAsync(BaseUrl,
            Body(seed.SourceId, seed.TargetId, (seed.ProductA, "Normal", 60m), (seed.ProductB, "FreeIssue", 20m)));

        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, raw);
        var created = JsonSerializer.Deserialize<Envelope<TransferResult>>(raw, _jsonOpts)!.Data;
        created.TransferNumber.Should().Be($"ST-{created.Id:D6}");
        created.SourceDistributorId.Should().Be(seed.SourceId);
        created.TargetDistributorId.Should().Be(seed.TargetId);
        created.TransferredByName.Should().Be("System Admin");
        created.LineCount.Should().Be(2);
        created.TotalQuantity.Should().Be(80m);
        created.Lines.Should().HaveCount(2);
        created.Lines!.Should().ContainSingle(l =>
            l.ProductId == seed.ProductB && l.StockType == "FreeIssue" && l.Quantity == 20m && l.PiecesPerPack == 12);

        // Balances moved: source down, target up (B row created on the target).
        var src = await BalancesAsync(seed.SourceId);
        src[(seed.ProductA, StockType.Normal)].Should().Be(40m);
        src[(seed.ProductB, StockType.FreeIssue)].Should().Be(0m);
        var tgt = await BalancesAsync(seed.TargetId);
        tgt[(seed.ProductA, StockType.Normal)].Should().Be(65m);
        tgt[(seed.ProductB, StockType.FreeIssue)].Should().Be(20m);

        // Ledger: one Out per line on the source, one In per line on the target, all referencing the transfer.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ledger = await db.StockTransactions.AsNoTracking()
                .Where(t => t.ReferenceType == "StockTransfer" && t.ReferenceId == created.Id)
                .ToListAsync();

            ledger.Should().HaveCount(4);
            ledger.Where(t => t.DistributorId == seed.SourceId).Should().OnlyContain(t =>
                t.TransactionType == StockTransactionType.TransferOut && t.Direction == StockTransactionDirection.Out);
            ledger.Where(t => t.DistributorId == seed.TargetId).Should().OnlyContain(t =>
                t.TransactionType == StockTransactionType.TransferIn && t.Direction == StockTransactionDirection.In);
            ledger.Should().ContainSingle(t => t.DistributorId == seed.TargetId && t.ProductId == seed.ProductA
                && t.QuantityBefore == 5m && t.QuantityAfter == 65m);
            ledger.Should().OnlyContain(t => t.TransactedBy == 1 && t.Notes!.Contains(created.TransferNumber));
        }

        // GET by id
        var byId = await _client.GetAsync($"{BaseUrl}/{created.Id}");
        byId.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = (await byId.Content.ReadFromJsonAsync<Envelope<TransferResult>>(_jsonOpts))!.Data;
        detail.Lines.Should().HaveCount(2);

        // GET list filtered by the target distributor (matches source OR target)
        var list = await _client.GetAsync($"{BaseUrl}?page=1&pageSize=20&distributorId={seed.TargetId}");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = (await list.Content.ReadFromJsonAsync<Envelope<List<TransferResult>>>(_jsonOpts))!;
        page.Pagination!.Total.Should().Be(1);
        var summary = page.Data.Single();
        summary.Id.Should().Be(created.Id);
        summary.LineCount.Should().Be(2);
        summary.TotalQuantity.Should().Be(80m);
        summary.SourceDistributorName.Should().StartWith("ST-src-");
    }

    [Fact]
    public async Task GetDistributorStock_ReturnsStockOfInactiveDistributor()
    {
        var seed = await SeedAsync();

        var response = await _client.GetAsync($"/api/v1/stock/distributors/{seed.SourceId}?page=1&pageSize=200");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = (await response.Content.ReadFromJsonAsync<Envelope<List<StockRow>>>(_jsonOpts))!.Data;
        rows.Should().HaveCount(2);
    }

    // ── Business rules ─────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ActiveSource_Returns422_SourceDistributorActive()
    {
        var seed = await SeedAsync(sourceActive: true);

        var response = await _client.PostAsync(BaseUrl, Body(seed.SourceId, seed.TargetId, (seed.ProductA, "Normal", 1m)));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var error = (await response.Content.ReadFromJsonAsync<ErrorEnvelope>(_jsonOpts))!.Error;
        error.Code.Should().Be("SOURCE_DISTRIBUTOR_ACTIVE");
    }

    [Fact]
    public async Task Create_QuantityAboveBalance_Returns422_AndLeavesBalancesUntouched()
    {
        var seed = await SeedAsync();

        var response = await _client.PostAsync(BaseUrl,
            Body(seed.SourceId, seed.TargetId, (seed.ProductA, "Normal", 10m), (seed.ProductB, "FreeIssue", 21m)));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var error = (await response.Content.ReadFromJsonAsync<ErrorEnvelope>(_jsonOpts))!.Error;
        error.Code.Should().Be("INSUFFICIENT_STOCK");

        (await BalancesAsync(seed.SourceId))[(seed.ProductA, StockType.Normal)].Should().Be(100m);
        (await BalancesAsync(seed.TargetId))[(seed.ProductA, StockType.Normal)].Should().Be(5m);
    }

    [Fact]
    public async Task Create_SameDistributor_Returns400()
    {
        var seed = await SeedAsync();

        var response = await _client.PostAsync(BaseUrl, Body(seed.SourceId, seed.SourceId, (seed.ProductA, "Normal", 1m)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_UnknownTarget_Returns404()
    {
        var seed = await SeedAsync();

        var response = await _client.PostAsync(BaseUrl, Body(seed.SourceId, 999_999, (seed.ProductA, "Normal", 1m)));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = (await response.Content.ReadFromJsonAsync<ErrorEnvelope>(_jsonOpts))!.Error;
        error.Code.Should().Be("DISTRIBUTOR_NOT_FOUND");
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

        var response = await client.PostAsync(BaseUrl, Body(1, 2, (1, "Normal", 1m)));

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

        var response = await client.PostAsync(BaseUrl, Body(1, 2, (1, "Normal", 1m)));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
