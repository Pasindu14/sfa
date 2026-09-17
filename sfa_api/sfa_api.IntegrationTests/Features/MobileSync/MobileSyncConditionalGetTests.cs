using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using sfa_api.IntegrationTests.Infrastructure;

namespace sfa_api.IntegrationTests.Features.MobileSync;

/// <summary>
/// Conditional GET contract for the mobile catalog sync endpoints (consumed by sfa_mobile):
/// 200 carries an ETag; If-None-Match with that value → 304 with an empty body; any catalog change
/// → a new ETag and a full 200; no If-None-Match → a normal 200 exactly as before.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class MobileSyncConditionalGetTests(SfaWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    private async Task<HttpResponseMessage> GetAsync(string url, string token, string? ifNoneMatch = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (ifNoneMatch is not null) request.Headers.TryAddWithoutValidation("If-None-Match", ifNoneMatch);
        return await _client.SendAsync(request);
    }

    private static string ETagOf(HttpResponseMessage r)
    {
        r.Headers.ETag.Should().NotBeNull("a 200 catalog response must carry an ETag");
        return r.Headers.ETag!.ToString();
    }

    [Theory]
    [InlineData("/api/v1/mobile/products")]
    [InlineData("/api/v1/mobile/product-categories")]
    public async Task Get_WithoutIfNoneMatch_Returns200WithEnvelopeAndETag(string url)
    {
        var response = await GetAsync(url, AuthHelper.SalesRepToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ETagOf(response).Should().StartWith("W/\"");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts);
        body.GetProperty("success").GetBoolean().Should().BeTrue();
        body.GetProperty("data").GetProperty("totalCount").ValueKind.Should().Be(JsonValueKind.Number);
    }

    [Theory]
    [InlineData("/api/v1/mobile/products")]
    [InlineData("/api/v1/mobile/product-categories")]
    public async Task Get_WithMatchingIfNoneMatch_Returns304WithEmptyBody(string url)
    {
        var etag = ETagOf(await GetAsync(url, AuthHelper.SalesRepToken));

        var response = await GetAsync(url, AuthHelper.SalesRepToken, etag);

        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
        (await response.Content.ReadAsByteArrayAsync()).Should().BeEmpty();
        response.Headers.ETag!.ToString().Should().Be(etag);
    }

    [Fact]
    public async Task Get_WithStaleOrListedIfNoneMatch_BehavesPerRfc()
    {
        const string url = "/api/v1/mobile/products";
        var etag = ETagOf(await GetAsync(url, AuthHelper.SalesRepToken));

        (await GetAsync(url, AuthHelper.SalesRepToken, "W/\"deadbeef\"")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetAsync(url, AuthHelper.SalesRepToken, $"W/\"deadbeef\", {etag}")).StatusCode.Should().Be(HttpStatusCode.NotModified);
        // Weak comparison: the strong form of the same opaque tag also matches.
        (await GetAsync(url, AuthHelper.SalesRepToken, etag[2..])).StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task Products_AfterProductAddedAndDeactivated_ETagChangesAnd200Returned()
    {
        const string url = "/api/v1/mobile/products";
        var before = ETagOf(await GetAsync(url, AuthHelper.SalesRepToken));

        using var create = new HttpRequestMessage(HttpMethod.Post, "/api/v1/products")
        {
            Content = JsonContent.Create(new
            {
                code = $"ETAG-{Guid.NewGuid():N}"[..20], itemDescription = "ETag Probe Product",
                printDescription = (string?)null, piecesPerPack = 6, imageUrl = (string?)null,
                remarks = (string?)null, rowVersion = 1u
            })
        };
        create.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthHelper.AdminToken);
        var created = await _client.SendAsync(create);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var productId = (await created.Content.ReadFromJsonAsync<JsonElement>(_jsonOpts))
            .GetProperty("data").GetProperty("id").GetInt32();

        var afterCreate = await GetAsync(url, AuthHelper.SalesRepToken, before);
        afterCreate.StatusCode.Should().Be(HttpStatusCode.OK, "the old ETag no longer describes the catalog");
        var createdETag = ETagOf(afterCreate);
        createdETag.Should().NotBe(before);

        using var deactivate = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/products/{productId}/deactivate")
        {
            Content = JsonContent.Create(new { })
        };
        deactivate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthHelper.AdminToken);
        (await _client.SendAsync(deactivate)).IsSuccessStatusCode.Should().BeTrue();

        var afterDeactivate = await GetAsync(url, AuthHelper.SalesRepToken, createdETag);
        afterDeactivate.StatusCode.Should().Be(HttpStatusCode.OK);
        ETagOf(afterDeactivate).Should().NotBe(createdETag);
    }
}
