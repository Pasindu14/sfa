using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Features.PricingStructures.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Infrastructure.Persistence;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.PricingStructures;

/// <summary>
/// End-to-end tests for /api/v1/pricing-structures and the mobile price-list sync.
/// The collection shares one SQLite database, so each test works on structures it created and
/// only relies on "some default exists", never on which one.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class PricingStructuresApiTests(SfaWebApplicationFactory factory)
{
    private const string BaseUrl = "/api/v1/pricing-structures";
    private readonly HttpClient _client = factory.CreateClient();
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    private record StructureDto(int Id, string Name, string? Description, bool IsDefault, bool IsActive, int PricedCount, uint RowVersion);
    private record ItemRowDto(int ProductId, string ProductCode, decimal? DealerPackPrice, decimal? DealerCasePrice, decimal? Mrp);
    private record Envelope<T>(bool Success, T Data);

    private void SetToken(string token)
        => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private static string UniqueName(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..30];

    private async Task<T> ReadDataAsync<T>(HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<Envelope<T>>(raw, _jsonOpts);
        envelope.Should().NotBeNull(raw);
        return envelope!.Data;
    }

    private async Task<StructureDto> CreateAsync(string? name = null)
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync(BaseUrl, new { name = name ?? UniqueName("PS"), description = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return await ReadDataAsync<StructureDto>(response);
    }

    private async Task<StructureDto> GetAsync(int id)
    {
        SetToken(AuthHelper.AdminToken);
        return await ReadDataAsync<StructureDto>(await _client.GetAsync($"{BaseUrl}/{id}"));
    }

    private async Task<int> SeedProductAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var p = new Product { Code = UniqueName("PSP"), ItemDescription = "Pricing test product", PiecesPerPack = 12, IsActive = true };
        db.Products.Add(p);
        await db.SaveChangesAsync();
        return p.Id;
    }

    /// Makes sure SOME default exists, so creates in this class never become the first-ever default.
    private async Task<int> EnsureDefaultAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = db.PricingStructures.FirstOrDefault(s => s.IsDefault && !s.IsDeleted);
        if (existing is not null) return existing.Id;
        var s = new PricingStructure { Name = UniqueName("Default"), IsActive = true, IsDefault = true };
        db.PricingStructures.Add(s);
        await db.SaveChangesAsync();
        return s.Id;
    }

    private async Task PutItemsAsync(int structureId, params object[] items)
    {
        SetToken(AuthHelper.AdminToken);
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{structureId}/items", new { items });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private async Task ActivateAsync(int id)
    {
        SetToken(AuthHelper.AdminToken);
        (await _client.PostAsync($"{BaseUrl}/{id}/activate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── CRUD ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WhenDefaultExists_StartsInactiveAndNotDefault()
    {
        await EnsureDefaultAsync();

        var created = await CreateAsync();

        created.IsActive.Should().BeFalse("reps must never sync a half-priced list");
        created.IsDefault.Should().BeFalse();
        created.PricedCount.Should().Be(0);
    }

    [Fact]
    public async Task Create_DuplicateName_Returns409()
    {
        await EnsureDefaultAsync();
        var name = UniqueName("Dup");
        await CreateAsync(name);

        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync(BaseUrl, new { name = name.ToUpperInvariant() });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Endpoints_RequireAdmin()
    {
        SetToken(AuthHelper.SalesRepToken);
        (await _client.GetAsync(BaseUrl)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await _client.GetAsync($"{BaseUrl}/default/prices")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_ChangesName()
    {
        await EnsureDefaultAsync();
        var created = await CreateAsync();

        SetToken(AuthHelper.AdminToken);
        var newName = UniqueName("Renamed");
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created.Id}",
            new { name = newName, description = "d", rowVersion = created.RowVersion });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await GetAsync(created.Id)).Name.Should().Be(newName);
    }

    // ── Items ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Items_UpsertThenRead_ShowsPricedAndUnpricedProducts()
    {
        await EnsureDefaultAsync();
        var s = await CreateAsync();
        var priced = await SeedProductAsync();
        var unpriced = await SeedProductAsync();

        await PutItemsAsync(s.Id, new { productId = priced, dealerPackPrice = 50m, dealerCasePrice = 580m, mrp = 60m });

        SetToken(AuthHelper.AdminToken);
        var rows = await ReadDataAsync<List<ItemRowDto>>(await _client.GetAsync($"{BaseUrl}/{s.Id}/items"));
        rows.Single(r => r.ProductId == priced).Should().BeEquivalentTo(
            new { DealerPackPrice = 50m, DealerCasePrice = 580m, Mrp = 60m }, o => o.ExcludingMissingMembers());
        rows.Single(r => r.ProductId == unpriced).DealerPackPrice.Should().BeNull("unpriced products still appear so they can be priced");
        (await GetAsync(s.Id)).PricedCount.Should().Be(1);

        // Clearing every price leaves the product unpriced.
        await PutItemsAsync(s.Id, new { productId = priced, dealerPackPrice = (decimal?)null, dealerCasePrice = (decimal?)null, mrp = (decimal?)null });
        (await GetAsync(s.Id)).PricedCount.Should().Be(0);
    }

    [Fact]
    public async Task Items_CasePriceWithoutPackPrice_Returns400()
    {
        await EnsureDefaultAsync();
        var s = await CreateAsync();
        var product = await SeedProductAsync();

        SetToken(AuthHelper.AdminToken);
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{s.Id}/items",
            new { items = new[] { new { productId = product, dealerPackPrice = (decimal?)null, dealerCasePrice = (decimal?)500m, mrp = (decimal?)null } } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Duplicate ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Duplicate_CopiesAllPrices_IntoInactiveStructure()
    {
        await EnsureDefaultAsync();
        var source = await CreateAsync();
        var p1 = await SeedProductAsync();
        var p2 = await SeedProductAsync();
        await PutItemsAsync(source.Id,
            new { productId = p1, dealerPackPrice = 10m, dealerCasePrice = 115m, mrp = 12m },
            new { productId = p2, dealerPackPrice = 20m, dealerCasePrice = (decimal?)null, mrp = (decimal?)null });

        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/{source.Id}/duplicate", new { name = UniqueName("Copy") });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var copy = await ReadDataAsync<StructureDto>(response);

        copy.IsActive.Should().BeFalse();
        copy.IsDefault.Should().BeFalse();
        copy.PricedCount.Should().Be(2);
        var rows = await ReadDataAsync<List<ItemRowDto>>(await _client.GetAsync($"{BaseUrl}/{copy.Id}/items"));
        rows.Single(r => r.ProductId == p1).DealerCasePrice.Should().Be(115m);

        // Editing the copy never touches the source.
        await PutItemsAsync(copy.Id, new { productId = p1, dealerPackPrice = 11m, dealerCasePrice = 126m, mrp = 12m });
        var sourceRows = await ReadDataAsync<List<ItemRowDto>>(await _client.GetAsync($"{BaseUrl}/{source.Id}/items"));
        sourceRows.Single(r => r.ProductId == p1).DealerPackPrice.Should().Be(10m);
    }

    // ── Default ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SetDefault_MovesTheSingleDefault_AndDefaultCannotBeDeactivatedOrDeleted()
    {
        var oldDefaultId = await EnsureDefaultAsync();
        var s = await CreateAsync();

        // Inactive structures can't become the default.
        SetToken(AuthHelper.AdminToken);
        var refused = await _client.PostAsJsonAsync($"{BaseUrl}/{s.Id}/set-default", new { rowVersion = s.RowVersion });
        refused.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await refused.Content.ReadAsStringAsync()).Should().Contain("PRICING_STRUCTURE_INACTIVE");

        await ActivateAsync(s.Id);
        var fresh = await GetAsync(s.Id);
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/{s.Id}/set-default", new { rowVersion = fresh.RowVersion });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PricingStructures.Where(x => x.IsDefault && !x.IsDeleted).Select(x => x.Id).ToList()
              .Should().Equal([s.Id], "exactly one structure is the default");
        }

        var deactivate = await _client.PostAsync($"{BaseUrl}/{s.Id}/deactivate", null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await deactivate.Content.ReadAsStringAsync()).Should().Contain("PRICING_STRUCTURE_IS_DEFAULT");
        (await _client.DeleteAsync($"{BaseUrl}/{s.Id}")).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        // The former default is now an ordinary structure and can be deactivated.
        (await _client.PostAsync($"{BaseUrl}/{oldDefaultId}/deactivate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await ActivateAsync(oldDefaultId);
    }

    [Fact]
    public async Task DefaultPrices_OpenToSupervisor_ReturnsDefaultsPricedItems()
    {
        var defaultId = await EnsureDefaultAsync();
        var product = await SeedProductAsync();
        await PutItemsAsync(defaultId, new { productId = product, dealerPackPrice = 33m, dealerCasePrice = 390m, mrp = (decimal?)null });

        SetToken(AuthHelper.SupervisorToken);
        var response = await _client.GetAsync($"{BaseUrl}/default/prices");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var data = body.GetProperty("data");
        data.GetProperty("pricingStructureId").GetInt32().Should().Be(defaultId);
        data.GetProperty("items").EnumerateArray()
            .Single(i => i.GetProperty("productId").GetInt32() == product)
            .GetProperty("dealerCasePrice").GetDecimal().Should().Be(390m);
    }

    // ── Delete ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_SoftDeletes_AndNameBecomesReusable()
    {
        await EnsureDefaultAsync();
        var name = UniqueName("Del");
        var s = await CreateAsync(name);

        SetToken(AuthHelper.AdminToken);
        (await _client.DeleteAsync($"{BaseUrl}/{s.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"{BaseUrl}/{s.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PricingStructures.Single(x => x.Id == s.Id).IsDeleted.Should().BeTrue("soft delete — the row stays for bill history");
        }

        await CreateAsync(name);   // the filtered unique index frees the name
    }

    // ── Mobile sync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task MobileSync_ReturnsOnlyActiveStructures_WithPricedItems()
    {
        await EnsureDefaultAsync();
        var active = await CreateAsync();
        var inactive = await CreateAsync();
        var priced = await SeedProductAsync();
        var unpriced = await SeedProductAsync();
        await PutItemsAsync(active.Id, new { productId = priced, dealerPackPrice = 5m, dealerCasePrice = (decimal?)null, mrp = (decimal?)null });
        await PutItemsAsync(inactive.Id, new { productId = priced, dealerPackPrice = 6m, dealerCasePrice = (decimal?)null, mrp = (decimal?)null });
        await ActivateAsync(active.Id);

        SetToken(AuthHelper.SalesRepToken);
        var response = await _client.GetAsync("/api/v1/mobile/pricing-structures");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var structures = (await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("pricingStructures").EnumerateArray().ToList();
        structures.Should().NotContain(s => s.GetProperty("id").GetInt32() == inactive.Id);
        structures.Should().ContainSingle(s => s.GetProperty("isDefault").GetBoolean());
        var mine = structures.Single(s => s.GetProperty("id").GetInt32() == active.Id);
        var items = mine.GetProperty("items").EnumerateArray().ToList();
        items.Should().ContainSingle(i => i.GetProperty("productId").GetInt32() == priced);
        items.Should().NotContain(i => i.GetProperty("productId").GetInt32() == unpriced);
    }

    [Fact]
    public async Task MobileProducts_LegacyPriceFields_ComeFromDefaultStructure()
    {
        var defaultId = await EnsureDefaultAsync();
        var product = await SeedProductAsync();
        await PutItemsAsync(defaultId, new { productId = product, dealerPackPrice = 44m, dealerCasePrice = 520m, mrp = 50m });

        SetToken(AuthHelper.SalesRepToken);
        var body = await (await _client.GetAsync("/api/v1/mobile/products")).Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);

        var row = body.GetProperty("data").GetProperty("products").EnumerateArray()
            .Single(p => p.GetProperty("id").GetInt32() == product);
        row.GetProperty("dealerPackPrice").GetDecimal().Should().Be(44m, "older app builds still bill from these fields");
        row.GetProperty("dealerCasePrice").GetDecimal().Should().Be(520m);
        row.GetProperty("mrp").GetDecimal().Should().Be(50m);
    }
}
