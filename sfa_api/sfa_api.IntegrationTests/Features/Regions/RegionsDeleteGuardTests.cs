using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Regions;

/// <summary>
/// Covers DELETE /api/v1/regions/{id} and its child-integrity guard:
/// a region with active areas can neither be deleted nor deactivated.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class RegionsDeleteGuardTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public RegionsDeleteGuardTests(SfaWebApplicationFactory factory) => _client = factory.CreateClient();

    private void SetToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<int> CreateRegionAsync(string name)
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/regions", new { name });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task<int> CreateAreaAsync(string name, int regionId)
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/areas", new { name, regionId });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();
    }

    // ── Auth ───────────────────────────────────────────

    [Fact]
    public async Task DeleteRegion_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var resp = await _client.DeleteAsync("/api/v1/regions/1");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteRegion_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);
        var resp = await _client.DeleteAsync("/api/v1/regions/1");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteRegion_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);
        var resp = await _client.DeleteAsync("/api/v1/regions/1");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteRegion_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);
        var resp = await _client.DeleteAsync("/api/v1/regions/999999");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Clean delete (no children) ─────────────────────

    [Fact]
    public async Task DeleteRegion_NoChildren_Returns204_AndRemovedFromActive()
    {
        SetToken(AuthHelper.AdminToken);
        var id = await CreateRegionAsync("Deletable Childless Region");

        var deleteResp = await _client.DeleteAsync($"/api/v1/regions/{id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // A soft-deleted region must no longer surface in the active list.
        var activeResp = await _client.GetAsync("/api/v1/regions/active");
        var activeBody = await activeResp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        var ids = activeBody.GetProperty("data").EnumerateArray()
            .Select(i => i.GetProperty("id").GetInt32());
        ids.Should().NotContain(id);
    }

    // ── Guard: active children block delete AND deactivate ──

    [Fact]
    public async Task DeleteRegion_WithActiveArea_Returns422_WithGuardCode()
    {
        SetToken(AuthHelper.AdminToken);
        var regionId = await CreateRegionAsync("Region With Child Area (delete)");
        await CreateAreaAsync("Blocking Area D", regionId);

        var resp = await _client.DeleteAsync($"/api/v1/regions/{regionId}");

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("REGION_HAS_ACTIVE_AREAS");
    }

    [Fact]
    public async Task DeactivateRegion_WithActiveArea_Returns422_WithGuardCode()
    {
        SetToken(AuthHelper.AdminToken);
        var regionId = await CreateRegionAsync("Region With Child Area (deactivate)");
        await CreateAreaAsync("Blocking Area X", regionId);

        var resp = await _client.PostAsync($"/api/v1/regions/{regionId}/deactivate", null);

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("REGION_HAS_ACTIVE_AREAS");
    }

    [Fact]
    public async Task DeleteRegion_AfterChildDeactivated_Succeeds()
    {
        SetToken(AuthHelper.AdminToken);
        var regionId = await CreateRegionAsync("Region Freed After Child Off");
        var areaId = await CreateAreaAsync("Child To Deactivate", regionId);

        // Deactivate the only child, then the parent is free to delete.
        (await _client.PostAsync($"/api/v1/areas/{areaId}/deactivate", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var resp = await _client.DeleteAsync($"/api/v1/regions/{regionId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
