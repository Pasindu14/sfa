using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Areas;

/// <summary>
/// Covers DELETE /api/v1/areas/{id} and its child-integrity guard:
/// an area with active territories can neither be deleted nor deactivated.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class AreasDeleteGuardTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public AreasDeleteGuardTests(SfaWebApplicationFactory factory) => _client = factory.CreateClient();

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

    // ── Auth ───────────────────────────────────────────

    [Fact]
    public async Task DeleteArea_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        (await _client.DeleteAsync("/api/v1/areas/1")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteArea_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);
        (await _client.DeleteAsync("/api/v1/areas/1")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteArea_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);
        (await _client.DeleteAsync("/api/v1/areas/999999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Clean delete (no children) ─────────────────────

    [Fact]
    public async Task DeleteArea_NoChildren_Returns204()
    {
        SetToken(AuthHelper.AdminToken);
        var regionId = await CreateRegionAsync("Region For Childless Area");
        var areaId = await CreateAreaAsync("Deletable Childless Area", regionId);

        (await _client.DeleteAsync($"/api/v1/areas/{areaId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Guard: active children block delete AND deactivate ──

    [Fact]
    public async Task DeleteArea_WithActiveTerritory_Returns422_WithGuardCode()
    {
        SetToken(AuthHelper.AdminToken);
        var regionId = await CreateRegionAsync("Region A For Area Guard");
        var areaId = await CreateAreaAsync("Area With Child Territory (delete)", regionId);
        await CreateTerritoryAsync("Blocking Territory D", areaId);

        var resp = await _client.DeleteAsync($"/api/v1/areas/{areaId}");

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("AREA_HAS_ACTIVE_TERRITORIES");
    }

    [Fact]
    public async Task DeactivateArea_WithActiveTerritory_Returns422_WithGuardCode()
    {
        SetToken(AuthHelper.AdminToken);
        var regionId = await CreateRegionAsync("Region B For Area Guard");
        var areaId = await CreateAreaAsync("Area With Child Territory (deactivate)", regionId);
        await CreateTerritoryAsync("Blocking Territory X", areaId);

        var resp = await _client.PostAsync($"/api/v1/areas/{areaId}/deactivate", null);

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("AREA_HAS_ACTIVE_TERRITORIES");
    }
}
