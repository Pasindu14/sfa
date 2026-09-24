using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Stock.Entities;
using sfa_api.Features.Stock.Enums;
using sfa_api.Infrastructure.Persistence;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Stock;

/// <summary>
/// GET /api/v1/stock/distributors/{id}/balances — the unpaged admin list behind the Stock,
/// Stock Adjustment and Stock Transfer pages (the paged endpoint silently capped them at 50 rows).
/// </summary>
[Collection(SfaApiCollection.Name)]
public class StockBalancesApiTests(SfaWebApplicationFactory factory)
{
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    private record Envelope<T>(bool Success, T Data);
    private record Balance(int Id, int ProductId, string ProductCode, string StockType, decimal QuantityOnHand, DateTime? LastUpdatedAt);

    private HttpClient Client(string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AuthHelper.GenerateToken(1, role, "admin@sfa.com", "System Admin"));
        return client;
    }

    /// <summary>A distributor holding 60 products (more than one default page) plus one active product it never held.</summary>
    private async Task<(int DistributorId, int UnheldProductId)> SeedAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var s = Guid.NewGuid().ToString("N")[..8];

        var distributor = new Distributor { Name = $"BAL-{s}", Email = $"bal-{s}@test.com", Phone = $"+94bl{s}", IsActive = true };
        var held = Enumerable.Range(1, 60)
            .Select(i => new Product { Code = $"BAL{i:D2}-{s}", ItemDescription = $"Held {i}", PiecesPerPack = 12, IsActive = true })
            .ToList();
        var unheld = new Product { Code = $"BALZ-{s}", ItemDescription = "Never held", PiecesPerPack = 12, IsActive = true };
        db.Add(distributor);
        db.AddRange(held);
        db.Add(unheld);
        await db.SaveChangesAsync();

        db.DistributorStocks.AddRange(held.Select((p, i) => new DistributorStock
        {
            DistributorId = distributor.Id, ProductId = p.Id, StockType = StockType.Normal,
            QuantityOnHand = i % 2 == 0 ? 0m : 10m,   // half of them at zero balance
            LastUpdatedAt = DateTime.UtcNow,
        }));
        await db.SaveChangesAsync();
        return (distributor.Id, unheld.Id);
    }

    private async Task<List<Balance>> GetAsync(HttpClient client, int distributorId, bool includeZero)
    {
        var res = await client.GetAsync($"/api/v1/stock/distributors/{distributorId}/balances?includeZeroStock={includeZero}");
        res.StatusCode.Should().Be(HttpStatusCode.OK, await res.Content.ReadAsStringAsync());
        return JsonSerializer.Deserialize<Envelope<List<Balance>>>(await res.Content.ReadAsStringAsync(), _jsonOpts)!.Data;
    }

    [Fact]
    public async Task Balances_ReturnsEveryRow_IncludingZeroBalances_NotJustOnePage()
    {
        var (distributorId, unheldId) = await SeedAsync();

        var rows = await GetAsync(Client("Admin"), distributorId, includeZero: false);

        rows.Should().HaveCount(60);
        rows.Count(r => r.QuantityOnHand == 0).Should().Be(30);
        rows.Should().NotContain(r => r.ProductId == unheldId);
    }

    [Fact]
    public async Task Balances_WithIncludeZeroStock_AddsNeverHeldProductsAsPlaceholders()
    {
        var (distributorId, unheldId) = await SeedAsync();

        var rows = await GetAsync(Client("Admin"), distributorId, includeZero: true);

        rows.Should().HaveCountGreaterThanOrEqualTo(61);
        var placeholder = rows.Should().ContainSingle(r => r.ProductId == unheldId).Subject;
        placeholder.Id.Should().Be(0);
        placeholder.QuantityOnHand.Should().Be(0);
        placeholder.StockType.Should().Be("Normal");
        placeholder.LastUpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task Balances_ForbiddenForSalesRep()
    {
        var (distributorId, _) = await SeedAsync();

        var res = await Client("SalesRep").GetAsync($"/api/v1/stock/distributors/{distributorId}/balances");

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
