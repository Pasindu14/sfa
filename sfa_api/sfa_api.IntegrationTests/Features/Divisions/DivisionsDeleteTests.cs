using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Divisions;

/// <summary>
/// Covers DELETE /api/v1/divisions/{id} and its child-integrity guard:
/// a division with active routes can neither be deleted nor deactivated
/// (mirrors the Area → active-territory guard).
/// </summary>
[Collection(SfaApiCollection.Name)]
public class DivisionsDeleteTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public DivisionsDeleteTests(SfaWebApplicationFactory factory) => _client = factory.CreateClient();

    private void SetToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<int> PostIdAsync(string url, object payload)
    {
        var resp = await _client.PostAsJsonAsync(url, payload);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task<int> CreateDivisionChainAsync(string prefix)
    {
        var regionId = await PostIdAsync("/api/v1/regions", new { name = $"{prefix} Region" });
        var areaId = await PostIdAsync("/api/v1/areas", new { name = $"{prefix} Area", regionId });
        var territoryId = await PostIdAsync("/api/v1/territories", new { name = $"{prefix} Territory", areaId });
        return await PostIdAsync("/api/v1/divisions", new { name = $"{prefix} Division", territoryId });
    }

    private Task<int> CreateRouteAsync(string name, int divisionId) =>
        PostIdAsync("/api/v1/routes", new { name, divisionId, pinColor = "#FF5733", description = (string?)null });

    [Fact]
    public async Task DeleteDivision_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        (await _client.DeleteAsync("/api/v1/divisions/1")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteDivision_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);
        (await _client.DeleteAsync("/api/v1/divisions/1")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteDivision_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);
        (await _client.DeleteAsync("/api/v1/divisions/999999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteDivision_NoRoutes_Returns204()
    {
        SetToken(AuthHelper.AdminToken);
        var divisionId = await CreateDivisionChainAsync("Deletable Leaf");

        (await _client.DeleteAsync($"/api/v1/divisions/{divisionId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Guard: active routes block delete AND deactivate ──

    [Fact]
    public async Task DeleteDivision_WithActiveRoute_Returns422_WithGuardCode()
    {
        SetToken(AuthHelper.AdminToken);
        var divisionId = await CreateDivisionChainAsync("Division With Route (delete)");
        await CreateRouteAsync("Blocking Route D", divisionId);

        var resp = await _client.DeleteAsync($"/api/v1/divisions/{divisionId}");

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("DIVISION_HAS_ACTIVE_ROUTES");
    }

    [Fact]
    public async Task DeactivateDivision_WithActiveRoute_Returns422_WithGuardCode()
    {
        SetToken(AuthHelper.AdminToken);
        var divisionId = await CreateDivisionChainAsync("Division With Route (deactivate)");
        await CreateRouteAsync("Blocking Route X", divisionId);

        var resp = await _client.PostAsync($"/api/v1/divisions/{divisionId}/deactivate", null);

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("DIVISION_HAS_ACTIVE_ROUTES");
    }
}
