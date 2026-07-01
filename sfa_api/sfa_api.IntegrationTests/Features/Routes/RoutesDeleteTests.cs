using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.Routes;

/// <summary>
/// Covers auth + not-found for DELETE /api/v1/routes/{id}. The child-integrity guard
/// (route with active outlets) is unit-tested at the service level — an Outlet requires
/// a large payload/graph that is impractical to build through the API here.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class RoutesDeleteTests
{
    private readonly HttpClient _client;

    public RoutesDeleteTests(SfaWebApplicationFactory factory) => _client = factory.CreateClient();

    private void SetToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task DeleteRoute_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        (await _client.DeleteAsync("/api/v1/routes/1")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteRoute_AsSalesRep_Returns403()
    {
        SetToken(AuthHelper.SalesRepToken);
        (await _client.DeleteAsync("/api/v1/routes/1")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteRoute_AsManager_Returns403()
    {
        SetToken(AuthHelper.ManagerToken);
        (await _client.DeleteAsync("/api/v1/routes/1")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteRoute_NonExistent_Returns404()
    {
        SetToken(AuthHelper.AdminToken);
        (await _client.DeleteAsync("/api/v1/routes/999999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
