using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Territories;

/// <summary>
/// Covers DELETE /api/v1/territories/{id} and its child-integrity guard:
/// a territory with active divisions can neither be deleted nor deactivated.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class TerritoriesDeleteGuardTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public TerritoriesDeleteGuardTests(SfaWebApplicationFactory factory) => _client = factory.CreateClient();

    private void SetToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<int> PostIdAsync(string url, object payload)
    {
        var resp = await _client.PostAsJsonAsync(url, payload);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();
    }

    private Task<int> CreateRegionAsync(string name) => PostIdAsync("/api/v1/regions", new { name });
    private Task<int> CreateAreaAsync(string name, int regionId) => PostIdAsync("/api/v1/areas", new { name, regionId });
    private Task<int> CreateTerritoryAsync(string name, int areaId) => PostIdAsync("/api/v1/territories", new { name, areaId });
    private Task<int> CreateDivisionAsync(string name, int territoryId) => PostIdAsync("/api/v1/divisions", new { name, territoryId });

    private async Task<int> CreateTerritoryChainAsync(string prefix)
    {
        var regionId = await CreateRegionAsync($"{prefix} Region");
        var areaId = await CreateAreaAsync($"{prefix} Area", regionId);
        return await CreateTerritoryAsync($"{prefix} Territory", areaId);
    }

    // ── Auth ───────────────────────────────────────────

    [Fact]
    public async Task DeleteTerritory_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        (await _client.DeleteAsync("/api/v1/territories/1")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteTerritory_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);
        (await _client.DeleteAsync("/api/v1/territories/1")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteTerritory_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);
        (await _client.DeleteAsync("/api/v1/territories/999999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Clean delete (no children) ─────────────────────

    [Fact]
    public async Task DeleteTerritory_NoChildren_Returns204()
    {
        SetToken(AuthHelper.AdminToken);
        var territoryId = await CreateTerritoryChainAsync("Childless");

        (await _client.DeleteAsync($"/api/v1/territories/{territoryId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Guard: active children block delete AND deactivate ──

    [Fact]
    public async Task DeleteTerritory_WithActiveDivision_Returns422_WithGuardCode()
    {
        SetToken(AuthHelper.AdminToken);
        var territoryId = await CreateTerritoryChainAsync("GuardDelete");
        await CreateDivisionAsync("Blocking Division D", territoryId);

        var resp = await _client.DeleteAsync($"/api/v1/territories/{territoryId}");

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("TERRITORY_HAS_ACTIVE_DIVISIONS");
    }

    [Fact]
    public async Task DeactivateTerritory_WithActiveDivision_Returns422_WithGuardCode()
    {
        SetToken(AuthHelper.AdminToken);
        var territoryId = await CreateTerritoryChainAsync("GuardDeactivate");
        await CreateDivisionAsync("Blocking Division X", territoryId);

        var resp = await _client.PostAsync($"/api/v1/territories/{territoryId}/deactivate", null);

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("TERRITORY_HAS_ACTIVE_DIVISIONS");
    }
}
