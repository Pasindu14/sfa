using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Divisions;

/// <summary>
/// Covers DELETE /api/v1/divisions/{id}. Division is a leaf in the geo hierarchy,
/// so there is no child guard — only auth and a clean soft-delete.
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
    public async Task DeleteDivision_LeafEntity_Returns204()
    {
        SetToken(AuthHelper.AdminToken);
        var divisionId = await CreateDivisionChainAsync("Deletable Leaf");

        (await _client.DeleteAsync($"/api/v1/divisions/{divisionId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
