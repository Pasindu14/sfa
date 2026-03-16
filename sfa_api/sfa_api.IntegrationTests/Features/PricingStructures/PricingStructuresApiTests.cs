using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.PricingStructures;

[Collection(SfaApiCollection.Name)]
public class PricingStructuresApiTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public PricingStructuresApiTests(SfaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private void SetToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // ─────────────────────────────────────────────────
    // Seeding helpers
    // ─────────────────────────────────────────────────

    private async Task<int> SeedPricingStructureAsync(string name, bool isDefault = false)
    {
        SetToken(AuthHelper.AdminToken);
        var payload = new { name, description = "Test description", isDefault };
        var response = await _client.PostAsJsonAsync("/api/v1/pricing-structures", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        return body.GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task<int> SeedProductAsync(string code, string description)
    {
        SetToken(AuthHelper.AdminToken);
        var payload = new
        {
            code,
            itemDescription = description,
            printDescription = (string?)null,
            piecesPerPack = 10,
            imageUrl = (string?)null,
            remarks = (string?)null
        };
        var response = await _client.PostAsJsonAsync("/api/v1/products", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        return body.GetProperty("data").GetProperty("id").GetInt32();
    }

    // ─────────────────────────────────────────────────
    // Authentication — 401
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/v1/pricing-structures");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/v1/pricing-structures",
            new { name = "Test", isDefault = false });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────
    // Authorization — 403
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.GetAsync("/api/v1/pricing-structures");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);

        var response = await _client.GetAsync("/api/v1/pricing-structures");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);

        var response = await _client.PostAsJsonAsync("/api/v1/pricing-structures",
            new { name = "Test", isDefault = false });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────
    // GET all
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_AsAdmin_Returns200WithSuccessTrue()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/pricing-structures");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.TryGetProperty("data", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_WithSearchParam_Returns200()
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var name = $"SearchableStructure-{uniqueSuffix}";
        await SeedPricingStructureAsync(name);

        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync($"/api/v1/pricing-structures?search={uniqueSuffix}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    // ─────────────────────────────────────────────────
    // GET by id
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_Returns200WithCorrectNameAndItemsArray()
    {
        var uniqueName = $"ById-{Guid.NewGuid():N}";
        var id = await SeedPricingStructureAsync(uniqueName);

        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync($"/api/v1/pricing-structures/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").GetProperty("name").GetString().Should().Be(uniqueName);
        body.GetProperty("data").TryGetProperty("items", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404WithNotFoundCode()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/pricing-structures/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Contain("NOT_FOUND");
    }

    // ─────────────────────────────────────────────────
    // POST create
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidPayload_Returns201WithId()
    {
        SetToken(AuthHelper.AdminToken);
        var name = $"NewStructure-{Guid.NewGuid():N}";

        var response = await _client.PostAsJsonAsync("/api/v1/pricing-structures",
            new { name, description = "Test", isDefault = false });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").GetProperty("id").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_EmptyName_Returns400WithValidationFailedCode()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsJsonAsync("/api/v1/pricing-structures",
            new { name = "", description = "Test", isDefault = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        body.GetProperty("error").GetProperty("fields").TryGetProperty("Name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Create_DuplicateName_Returns409()
    {
        var name = $"DupStructure-{Guid.NewGuid():N}";
        await SeedPricingStructureAsync(name);

        SetToken(AuthHelper.AdminToken);
        var response = await _client.PostAsJsonAsync("/api/v1/pricing-structures",
            new { name, description = "Test", isDefault = false });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ─────────────────────────────────────────────────
    // IsDefault promotion — default swap
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_TwoDefaultStructures_FirstIsUnsetWhenSecondIsCreated()
    {
        var nameA = $"DefaultA-{Guid.NewGuid():N}";
        var nameB = $"DefaultB-{Guid.NewGuid():N}";

        var idA = await SeedPricingStructureAsync(nameA, isDefault: true);
        await SeedPricingStructureAsync(nameB, isDefault: true);

        SetToken(AuthHelper.AdminToken);
        var responseA = await _client.GetAsync($"/api/v1/pricing-structures/{idA}");
        responseA.StatusCode.Should().Be(HttpStatusCode.OK);
        var bodyA = await responseA.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        bodyA.GetProperty("data").GetProperty("isDefault").GetBoolean().Should().BeFalse();
    }

    // ─────────────────────────────────────────────────
    // PUT update
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Update_ValidPayload_Returns200WithUpdatedName()
    {
        var originalName = $"Original-{Guid.NewGuid():N}";
        var id = await SeedPricingStructureAsync(originalName);
        var updatedName = $"Updated-{Guid.NewGuid():N}";

        SetToken(AuthHelper.AdminToken);
        var response = await _client.PutAsJsonAsync($"/api/v1/pricing-structures/{id}",
            new { name = updatedName, description = "Updated desc", isDefault = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("data").GetProperty("name").GetString().Should().Be(updatedName);
    }

    [Fact]
    public async Task Update_NonExistentId_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PutAsJsonAsync("/api/v1/pricing-structures/99999",
            new { name = "Whatever", description = "x", isDefault = false });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_EmptyName_Returns400WithValidationFailedCode()
    {
        var id = await SeedPricingStructureAsync($"ToUpdate-{Guid.NewGuid():N}");

        SetToken(AuthHelper.AdminToken);
        var response = await _client.PutAsJsonAsync($"/api/v1/pricing-structures/{id}",
            new { name = "", description = "x", isDefault = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    // ─────────────────────────────────────────────────
    // DELETE (soft deactivate)
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingStructure_Returns204()
    {
        var id = await SeedPricingStructureAsync($"ToDelete-{Guid.NewGuid():N}");

        SetToken(AuthHelper.AdminToken);
        var response = await _client.DeleteAsync($"/api/v1/pricing-structures/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_NonExistentId_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.DeleteAsync("/api/v1/pricing-structures/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────
    // POST activate
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task Activate_AfterDelete_Returns204()
    {
        var id = await SeedPricingStructureAsync($"ToActivate-{Guid.NewGuid():N}");
        SetToken(AuthHelper.AdminToken);
        await _client.DeleteAsync($"/api/v1/pricing-structures/{id}");

        var response = await _client.PostAsync($"/api/v1/pricing-structures/{id}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Activate_NonExistentId_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.PostAsync("/api/v1/pricing-structures/99999/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────
    // GET items
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetItems_ExistingStructure_Returns200WithItemsArray()
    {
        var id = await SeedPricingStructureAsync($"ForItems-{Guid.NewGuid():N}");

        SetToken(AuthHelper.AdminToken);
        var response = await _client.GetAsync($"/api/v1/pricing-structures/{id}/items");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetItems_NonExistentStructure_Returns404()
    {
        SetToken(AuthHelper.AdminToken);

        var response = await _client.GetAsync("/api/v1/pricing-structures/99999/items");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────
    // PUT items — bulk replace
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task BulkUpdateItems_ValidProductId_Returns200WithItemsList()
    {
        var structureId = await SeedPricingStructureAsync($"BulkValid-{Guid.NewGuid():N}");
        var productId = await SeedProductAsync($"PROD-{Guid.NewGuid():N}"[..10], "Bulk Test Product");

        SetToken(AuthHelper.AdminToken);
        var payload = new
        {
            items = new[]
            {
                new { productId, unitPrice = 99.99m, packPrice = (decimal?)950.00m }
            }
        };

        var response = await _client.PutAsJsonAsync($"/api/v1/pricing-structures/{structureId}/items", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task BulkUpdateItems_EmptyItemsList_Returns400()
    {
        var structureId = await SeedPricingStructureAsync($"BulkEmpty-{Guid.NewGuid():N}");

        SetToken(AuthHelper.AdminToken);
        var payload = new { items = Array.Empty<object>() };

        var response = await _client.PutAsJsonAsync($"/api/v1/pricing-structures/{structureId}/items", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task BulkUpdateItems_NonExistentProductId_Returns400WithValidationFailed()
    {
        // The service throws ValidationException (not BusinessRuleException) for invalid product IDs,
        // which maps to HTTP 400.
        var structureId = await SeedPricingStructureAsync($"BulkBadProd-{Guid.NewGuid():N}");

        SetToken(AuthHelper.AdminToken);
        var payload = new
        {
            items = new[]
            {
                new { productId = 999999, unitPrice = 10.00m, packPrice = (decimal?)null }
            }
        };

        var response = await _client.PutAsJsonAsync($"/api/v1/pricing-structures/{structureId}/items", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    [Fact]
    public async Task BulkUpdateItems_NonExistentStructure_Returns404()
    {
        var productId = await SeedProductAsync($"PROD-{Guid.NewGuid():N}"[..10], "Orphan Product");

        SetToken(AuthHelper.AdminToken);
        var payload = new
        {
            items = new[]
            {
                new { productId, unitPrice = 10.00m, packPrice = (decimal?)null }
            }
        };

        var response = await _client.PutAsJsonAsync("/api/v1/pricing-structures/99999/items", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
