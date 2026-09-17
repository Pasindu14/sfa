using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.ProductCategoryPricings;
using sfa_api.Features.ProductCategoryPricings.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Caching;
using sfa_api.Infrastructure.Persistence;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.ProductCategoryPricings;

/// <summary>
/// GET /api/v1/product-category-pricings/portal — the distributor's single-tier price list, now a
/// projected LEFT JOIN cached per category. Covers: output identical to the original two-query
/// algorithm, and cache invalidation on pricing upsert and product deactivate/activate/update.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class ProductCategoryPricingPortalApiTests(SfaWebApplicationFactory factory)
{
    private const string PortalUrl = "/api/v1/product-category-pricings/portal";
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record PriceRow(int ProductId, string ProductCode, string ItemDescription, decimal UnitPrice);

    private sealed record Seed(string DistributorToken, string Category, int PricedId, int UnpricedId, int InactiveId, int DeletedId, string Suffix);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null)
    {
        using var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) req.Content = JsonContent.Create(body);
        return await _client.SendAsync(req);
    }

    private async Task<List<PriceRow>> GetPortalAsync(string token)
    {
        var response = await SendAsync(HttpMethod.Get, PortalUrl, token);
        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, raw);
        var data = JsonDocument.Parse(raw).RootElement.GetProperty("data");
        return data.EnumerateArray().Select(e => new PriceRow(
            e.GetProperty("productId").GetInt32(),
            e.GetProperty("productCode").GetString()!,
            e.GetProperty("itemDescription").GetString()!,
            e.GetProperty("unitPrice").GetDecimal())).ToList();
    }

    private async Task<Seed> SeedAsync(string category)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var distributor = new Distributor
        {
            Name = $"PriceDist-{suffix}", Address = "1 Price St", Phone = $"05{suffix}",
            Email = $"pricedist-{suffix}@sfa.com", Alias = Math.Abs(Guid.NewGuid().GetHashCode()),
            Category = category, IsActive = true,
        };
        db.Distributors.Add(distributor);

        Product P(string tag, bool active = true, bool deleted = false) => new()
        {
            Code = $"PX-{suffix}-{tag}", ItemDescription = $"Price test {tag} {suffix}", PiecesPerPack = 12,
            IsActive = active, IsDeleted = deleted,
        };
        var priced = P("1priced");
        var unpriced = P("2unpriced");
        var inactive = P("3inactive", active: false);
        var deleted = P("4deleted", active: false, deleted: true);
        db.Products.AddRange(priced, unpriced, inactive, deleted);
        await db.SaveChangesAsync();

        ProductCategoryPrice Price(int productId, string cat, decimal price) => new()
            { ProductId = productId, Category = cat, Price = price };
        db.ProductCategoryPrices.AddRange(
            Price(priced.Id, category, 123.45m),
            Price(priced.Id, category == "A" ? "B" : "A", 999m),   // another tier — must not leak in
            Price(unpriced.Id, category == "A" ? "B" : "A", 888m), // priced only in another tier → 0
            Price(inactive.Id, category, 77m),
            Price(deleted.Id, category, 66m));

        var user = new User
        {
            Name = $"Price Distributor {suffix}", Username = $"pricedist-{suffix}", Email = $"pricedist-user-{suffix}@sfa.com",
            Phone = $"076{suffix}", PasswordHash = "placeholder", Role = UserRole.Distributor,
            DistributorId = distributor.Id, IsActive = true,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Start each test from a cold price-list cache: the cache is shared across the test host.
        await scope.ServiceProvider.GetRequiredService<ICacheService>()
            .RemoveByPrefixAsync(ProductCategoryPricingCacheKeys.Prefix);

        return new Seed(AuthHelper.GenerateToken(user.Id, "Distributor"), category,
            priced.Id, unpriced.Id, inactive.Id, deleted.Id, suffix);
    }

    /// <summary>The pre-optimisation algorithm, verbatim: load active products, then their prices.</summary>
    private async Task<List<PriceRow>> LegacyExpectedAsync(string category)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var products = await db.Products.AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted).OrderBy(p => p.Code).ToListAsync();
        var productIds = products.Select(p => p.Id).ToList();
        var priceByProduct = (await db.ProductCategoryPrices.AsNoTracking()
                .Where(x => productIds.Contains(x.ProductId) && x.Category == category).ToListAsync())
            .ToDictionary(x => x.ProductId, x => x.Price);

        return products.Select(p => new PriceRow(p.Id, p.Code, p.ItemDescription,
            priceByProduct.GetValueOrDefault(p.Id, 0m))).ToList();
    }

    [Fact]
    public async Task Portal_ReturnsExactlyTheLegacyOutput_ForCallersCategory()
    {
        var seed = await SeedAsync("C");

        var actual = await GetPortalAsync(seed.DistributorToken);
        var expected = await LegacyExpectedAsync("C");

        actual.Should().Equal(expected);   // same rows, same order, same values

        actual.Single(r => r.ProductId == seed.PricedId).UnitPrice.Should().Be(123.45m);
        actual.Single(r => r.ProductId == seed.UnpricedId).UnitPrice.Should().Be(0m);
        actual.Should().NotContain(r => r.ProductId == seed.InactiveId || r.ProductId == seed.DeletedId);

        // Second call is served from cache and must be byte-for-byte the same list.
        (await GetPortalAsync(seed.DistributorToken)).Should().Equal(expected);
    }

    [Fact]
    public async Task Portal_IsCached_AndPricingUpsertInvalidatesIt()
    {
        var seed = await SeedAsync("B");
        (await GetPortalAsync(seed.DistributorToken)).Single(r => r.ProductId == seed.PricedId)
            .UnitPrice.Should().Be(123.45m);

        // A write that bypasses the service is NOT seen — proves the list is cached.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = await db.ProductCategoryPrices.SingleAsync(x => x.ProductId == seed.PricedId && x.Category == "B");
            row.Price = 1m;
            await db.SaveChangesAsync();
        }
        (await GetPortalAsync(seed.DistributorToken)).Single(r => r.ProductId == seed.PricedId)
            .UnitPrice.Should().Be(123.45m);

        // The real write path evicts it.
        var put = await SendAsync(HttpMethod.Put, "/api/v1/product-category-pricings", AuthHelper.AdminToken, new
        {
            items = new[] { new { productId = seed.UnpricedId, priceA = 1m, priceB = 42.5m, priceC = 3m, priceD = 4m } }
        });
        put.StatusCode.Should().Be(HttpStatusCode.OK, await put.Content.ReadAsStringAsync());

        var after = await GetPortalAsync(seed.DistributorToken);
        after.Single(r => r.ProductId == seed.UnpricedId).UnitPrice.Should().Be(42.5m);
        after.Single(r => r.ProductId == seed.PricedId).UnitPrice.Should().Be(1m);
        after.Should().Equal(await LegacyExpectedAsync("B"));
    }

    [Fact]
    public async Task Portal_ProductDeactivateActivateAndUpdate_InvalidateIt()
    {
        var seed = await SeedAsync("D");
        (await GetPortalAsync(seed.DistributorToken)).Should().Contain(r => r.ProductId == seed.PricedId);

        var deactivate = await SendAsync(HttpMethod.Post, $"/api/v1/products/{seed.PricedId}/deactivate", AuthHelper.AdminToken);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetPortalAsync(seed.DistributorToken)).Should().NotContain(r => r.ProductId == seed.PricedId);

        var activate = await SendAsync(HttpMethod.Post, $"/api/v1/products/{seed.PricedId}/activate", AuthHelper.AdminToken);
        activate.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
        (await GetPortalAsync(seed.DistributorToken)).Should().Contain(r => r.ProductId == seed.PricedId && r.UnitPrice == 123.45m);

        // Update (description) through the API → the cached description is replaced.
        var current = await SendAsync(HttpMethod.Get, $"/api/v1/products/{seed.PricedId}", AuthHelper.AdminToken);
        var product = JsonDocument.Parse(await current.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        var newDescription = $"Renamed {seed.Suffix}";
        var update = await SendAsync(HttpMethod.Put, $"/api/v1/products/{seed.PricedId}", AuthHelper.AdminToken, new
        {
            code = product.GetProperty("code").GetString(),
            itemDescription = newDescription,
            piecesPerPack = product.GetProperty("piecesPerPack").GetInt32(),
            rowVersion = product.GetProperty("rowVersion").GetUInt32(),
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK, await update.Content.ReadAsStringAsync());

        var after = await GetPortalAsync(seed.DistributorToken);
        after.Single(r => r.ProductId == seed.PricedId).ItemDescription.Should().Be(newDescription);
        after.Should().Equal(await LegacyExpectedAsync("D"));
    }
}
